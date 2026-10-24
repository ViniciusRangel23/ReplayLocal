// Replay Local: added 2026-10-24. GPL-2.0-or-later. See MODIFICATIONS.md.
using Kairos.Engine.Audio;
using Kairos.Settings;
namespace Kairos.Tests;
public static class ApplicationAudioReconnectTests
{
    static ApplicationAudioTarget Discord(string version,string root=@"C:\Users\User\AppData\Local\Discord")=>new($@"{root}\app-{version}\Discord.exe","Discord.exe");
    static ApplicationAudioTarget Spotify(string version,string publisher="zpdnekdrzrea0",string arch="x64")=>new($@"C:\Program Files\WindowsApps\SpotifyAB.SpotifyMusic_{version}_{arch}__{publisher}\Spotify.exe","Spotify.exe");
    [Test]
    public static void Discord_update_reconnects_in_the_same_installation()
    {
        var saved=Discord("1.0.9264");var running=new AudioTargetCandidate(Discord("1.0.9265"),"discord-new",101);
        Assert.Equal("discord-new",ApplicationAudioTargets.Resolve(saved,[running]).WindowToken);
    }
    [Test]
    public static void Spotify_store_update_reconnects_in_the_same_package_family()
    {
        var saved=Spotify("1.305.3058.0");var running=new AudioTargetCandidate(Spotify("1.306.3060.0"),"spotify-new",102);
        Assert.Equal("spotify-new",ApplicationAudioTargets.Resolve(saved,[running]).WindowToken);
    }
    [Test]
    public static void Same_name_elsewhere_or_different_publisher_is_never_selected()
    {
        var saved=Discord("1.0.9264");
        foreach(var other in new[]{Discord("1.0.9265",@"C:\Other\Discord"),Discord("evil"),new ApplicationAudioTarget(@"C:\Other\Discord.exe","Discord.exe")})
            Assert.Equal("unavailable",ApplicationAudioTargets.Resolve(saved,[new(other,"other",101)]).State);
        Assert.Equal("unavailable",ApplicationAudioTargets.Resolve(Spotify("1.305.3058.0"),[new(Spotify("1.306.3060.0","different"),"other",102)]).State);
        Assert.Equal("unavailable",ApplicationAudioTargets.Resolve(Spotify("1.305.3058.0"),[new(Spotify("1.306.3060.0",arch:"arm64"),"other",102)]).State);
        Assert.Equal("unavailable",ApplicationAudioTargets.Resolve(new(@"C:\A\game.exe","game.exe"),[new(new(@"C:\B\game.exe","game.exe"),"other",103)]).State);
    }
    [Test]
    public static void Multiple_version_processes_remain_ambiguous()
    {
        var saved=Discord("1.0.9264");
        Assert.Equal("ambiguous",ApplicationAudioTargets.Resolve(saved,[new(saved,"old",100),new(Discord("1.0.9265"),"new",101)]).State);
    }
    sealed class Resource(Action release):IDisposable{public void Dispose()=>release();}
    sealed class Backend:IApplicationAudioBackend
    {
        public List<AudioTargetCandidate> Created=[];public int Released;
        public IDisposable Create(string id,string token,uint mask,double volume)=>throw new InvalidOperationException("Candidate identity required");
        public IDisposable Create(string id,AudioTargetCandidate candidate,uint mask,double volume){Created.Add(candidate);return new Resource(()=>Released++);}
    }
    [Test]
    public static void Session_passes_current_identity_to_guard_and_preserves_track_id()
    {
        var saved=Discord("1.0.9264");var backend=new Backend();
        using var session=new ApplicationAudioSession(new AudioSettings{Applications=[new(){Id="discord-track",Name="Discord",Target=saved}]},backend);
        session.Refresh([new(saved,"old",100)]);
        session.Refresh([]);Assert.Equal(1,backend.Released);
        var current=new AudioTargetCandidate(Discord("1.0.9265"),"new",101);
        session.Refresh([current]);Assert.Equal(2,backend.Created.Count);
        Assert.Equal(current,backend.Created[1]);Assert.Equal("discord-track",session.Status[0].Id);
        Assert.Equal("available",session.Status[0].State);
        session.Refresh([current]);Assert.Equal(2,backend.Created.Count,"same version does not recreate source");
    }
    [Test]
    public static void Case_changes_and_hidden_windows_do_not_break_family_identity()
    {
        var target=Spotify("1.305.3058.0");var current=Spotify("1.306.3060.0");
        current=current with {ExecutablePath=current.ExecutablePath.ToUpperInvariant(),ExecutableName="SPOTIFY.EXE"};
        Assert.Equal("hidden",ApplicationAudioTargets.Resolve(target,[new(current,"hidden",102,IsVisible:false)]).WindowToken);
        Assert.Equal("unavailable",ApplicationAudioTargets.Resolve(Discord("1.0.9264"),[new(Discord("1.0.9265"),"",101)]).State);
    }
    [Test]
    public static void Two_versions_of_same_installation_cannot_be_added_as_two_sources()
    {
        var audio=new AudioSettings{Applications=[new(){Name="Discord",Target=Discord("1.0.9264")},new(){Name="Discord again",Target=Discord("1.0.9265")}]};
        Assert.True(ApplicationAudioSettings.Validate(audio)!=null,"same installation would duplicate the recording mix");
        var vm=new Kairos.Ui.SettingsViewModel();
        var old=new AudioTargetCandidate(Discord("1.0.9264"),"old",100);var current=new AudioTargetCandidate(Discord("1.0.9265"),"new",101);
        vm.Seed(null,new([],[],[],[],[],"test",[old,current],true));vm.UseApplicationAudio=true;
        vm.SelectedAudioCandidate=old;vm.AddAudioApplicationCommand.Execute(null);
        vm.SelectedAudioCandidate=current;vm.AddAudioApplicationCommand.Execute(null);
        Assert.Equal(1,vm.AppAudioRows.Count);Assert.True(!vm.CaptureEnabled,"configuration does not grant capture");
    }
    [Test]
    public static void Explicit_updated_application_is_excluded_from_automatic_game_track()
    {
        var backend=new Backend();var current=new AudioTargetCandidate(Discord("1.0.9265"),"new",101);
        using var session=new ApplicationAudioSession(new AudioSettings{Applications=[new(){Id="discord",Name="Discord",Target=Discord("1.0.9264")},new(){Id="game",Name="Game",AutomaticGame=true}]},backend);
        session.Refresh([current],current);
        Assert.Equal(1,backend.Created.Count);Assert.Equal("waitingGame",session.Status.Single(s=>s.Id=="game").State);
    }
    [Test]
    public static void Conflicting_process_path_is_not_reused_even_if_pid_is_equal()
    {
        var saved=Discord("1.0.9264");var newer=Discord("1.0.9265");
        Assert.Equal("ambiguous",ApplicationAudioTargets.Resolve(saved,[new(saved,"same",100),new(newer,"same",100)]).State);
    }
}
