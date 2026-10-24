using Kairos.Engine.Audio;
using Kairos.App;
using Kairos.Settings;
using Kairos.Ui;
namespace Kairos.Tests;
public static class AutomaticGameAudioTests
{
    static readonly AudioTargetCandidate Game=new(new(@"C:\Games\game.exe","game.exe"),"game:window:game.exe",10);
    static readonly AudioTargetCandidate Other=new(new(@"C:\Games\other.exe","other.exe"),"other:window:other.exe",20);
    [Test]
    public static void Tracker_retains_game_on_alt_tab_switches_fullscreen_and_clears_on_exit()
    {
        var tracker=new AutomaticGameAudio();
        Assert.True(tracker.Update([Game,Other],null,[])==null,"no capture before fullscreen detection");
        Assert.Equal(Game,tracker.Update([Game,Other],Game,[]));
        Assert.Equal(Game,tracker.Update([Game,Other],null,[]));
        Assert.Equal(Other,tracker.Update([Game,Other],Other,[]));
        Assert.True(tracker.Update([Game],null,[])==null,"closing selected process clears authorization");
        Assert.True(tracker.Update([Other with {ProcessId=21}],null,[])==null,"restarted process needs fresh detection");
    }
    [Test]
    public static void Geometry_accepts_borderless_and_excludes_maximized_shell_and_partial_windows()
    {
        var monitor=new ScreenRect(-1920,0,0,1080);
        Assert.True(AutomaticGameAudio.IsFullscreen(monitor,monitor,"UnrealWindow"),"fullscreen borderless");
        Assert.True(!AutomaticGameAudio.IsFullscreen(new(-1920,32,0,1040),monitor,"Chrome_WidgetWin_1"),"ordinary maximized window is not fullscreen");
        Assert.True(!AutomaticGameAudio.IsFullscreen(monitor,monitor,"Chrome_WidgetWin_1",hasCaption:true),"custom caption maximized with hidden taskbar is not fullscreen");
        Assert.True(!AutomaticGameAudio.IsFullscreen(monitor,monitor,"WorkerW"),"desktop is not a game");
        Assert.True(!AutomaticGameAudio.IsFullscreen(new(-1800,100,-200,900),monitor,"UnrealWindow"),"partial window rejected");
    }
    [Test]
    public static void Manual_tracks_cannot_be_captured_twice_or_selected_without_valid_identity()
    {
        var tracker=new AutomaticGameAudio();
        Assert.True(tracker.Update([Game],Game,[Game.Target.ExecutablePath])==null,"explicit track wins over automatic source");
        Assert.True(tracker.Update([Game],Game with {ProcessId=0},[])==null,"unverified process rejected");
        Assert.True(tracker.Update([],Game,[])==null,"stale foreground candidate rejected");
    }
    [Test]
    public static void Automatic_setting_is_unique_round_trips_and_keeps_microphone_off()
    {
        var settings=ApplicationAudioSettingsTests.Config(0);
        settings.Audio.Applications.Add(new(){AutomaticGame=true,Name="Som de jogo"});
        Assert.True(ApplicationAudioSettings.Validate(settings.Audio)==null,"automatic target does not require fixed path");
        var saved=SettingsStore.Deserialize(SettingsStore.Serialize(settings));
        Assert.True(saved.Audio.Applications[0].AutomaticGame&&!saved.Audio.Mic,"round trip does not grant microphone");
        saved.Audio.Applications.Add(new(){AutomaticGame=true,Name="Duplicate"});
        Assert.True(ApplicationAudioSettings.Validate(saved.Audio)!=null,"single automatic game track");
    }
    [Test]
    public static void UI_adds_one_game_source_without_starting_capture()
    {
        var vm=new SettingsViewModel();vm.Seed(null,new([],[],[],[],[],"test",[],true));vm.UseApplicationAudio=true;
        vm.AddGameAudioCommand.Execute(null);
        Assert.Equal(1,vm.ApplicationAudioRows.Count);
        Assert.True(!vm.CanAddGameAudio&&!vm.CaptureEnabled&&!vm.Mic,"one auto source and no implicit recording");
        vm.AddGameAudioCommand.Execute(null);Assert.Equal(1,vm.ApplicationAudioRows.Count);
    }
    sealed class Backend:IApplicationAudioBackend
    {
        public List<(string Id,string Token,uint Mask)> Created=[];
        public int Released;
        public IDisposable Create(string id,string token,uint mask,double volume){Created.Add((id,token,mask));return new Resource(()=>Released++);}
        sealed class Resource(Action dispose):IDisposable{public void Dispose()=>dispose();}
    }
    [Test]
    public static void Session_switches_only_game_source_and_preserves_track_slot_and_manual_sources()
    {
        var config=ApplicationAudioSettingsTests.Config(0);
        config.Audio.Applications.Add(new(){Id="game-track",AutomaticGame=true,Name="Som de jogo"});
        var music=new AudioTargetCandidate(new(@"C:\Apps\music.exe","music.exe"),"music",30);
        config.Audio.Applications.Add(new(){Id="music-track",Target=music.Target,Name="Music"});
        var backend=new Backend();using var session=new ApplicationAudioSession(config.Audio,backend);
        session.Refresh([Game,Other,music],Game);
        Assert.Equal(2,backend.Created.Count);Assert.Equal(3u,backend.Created[0].Mask);
        session.Refresh([Game,Other,music]);Assert.Equal(2,backend.Created.Count);
        session.Refresh([Game,Other,music],Other);
        Assert.Equal(3,backend.Created.Count);Assert.Equal(1,backend.Released);
        Assert.Equal(("game-track",Other.WindowToken,3u),backend.Created[2]);
        Assert.Equal("other.exe",session.Status[0].ApplicationName);
        session.Refresh([Game,music]);Assert.Equal("waitingGame",session.Status[0].State);
        Assert.Equal("available",session.Status[1].State);Assert.Equal(2,backend.Released);
    }
    [Test]
    public static void Unsaved_game_source_says_save_instead_of_unavailable()
    {
        var vm=new SettingsViewModel();vm.Seed(null,new([],[],[],[],[],"test",[],true));vm.UseApplicationAudio=true;
        vm.AddGameAudioCommand.Execute(null);
        vm.SeedStatus(new(null,[],null,null,false,false,"test",AudioSources:[]));
        Assert.Equal("pending",vm.ApplicationAudioRows[0].State);
    }
}
