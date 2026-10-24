using Kairos.App;
using Kairos.Engine;
using Kairos.Engine.Audio;
using Kairos.Settings;
using Kairos.Ui;
namespace Kairos.Tests;
public static class AudioFeedbackTests
{
    static SettingsViewModel Model()
    {
        var vm=new SettingsViewModel();
        vm.Seed(null,new([],[],[new("Microfone de teste","desk")],[],[],"test",[new(new(@"C:\Apps\Discord.exe","Discord.exe"),"discord",10)],true));
        return vm;
    }
    [Test]
    public static void Game_field_is_separate_keeps_identity_and_never_grants_capture()
    {
        var vm=Model();vm.UseApplicationAudio=true;vm.GameAudioEnabled=true;
        Assert.True(vm.GameAudioRow!=null && vm.AppAudioRows.Count==0,"separate game field");
        string id=vm.GameAudioRow!.Id;
        vm.GameAudioEnabled=false;vm.GameAudioEnabled=true;
        Assert.Equal(id,vm.GameAudioRow!.Id);
        vm.SelectedAudioCandidate=vm.AudioCandidates[0];vm.AddAudioApplicationCommand.Execute(null);
        Assert.Equal(1,vm.AppAudioRows.Count);Assert.Equal(2,vm.ApplicationAudioRows.Count);
        Assert.True(!vm.CaptureEnabled&&!vm.Mic,"no consent granted by feedback controls");
    }
    [Test]
    public static void Microphone_device_pending_off_and_online_states_are_distinct()
    {
        var vm=Model();Assert.Equal("selectDevice",vm.MicFeedbackState);
        vm.MicDevice="desk";Assert.True(vm.Mic,"specific device selection grants only microphone setting");
        Assert.Equal("pending",vm.MicFeedbackState);Assert.True(!vm.MicMeterActive,"pending source is not shown live");
        vm.SeedReplay(vm.ReplayEnabled);vm.SeedStatus(new(null,[],null,null,false,false,"test",CaptureEnabled:false));
        Assert.Equal("captureOff",vm.MicFeedbackState);
        vm.SeedStatus(new(null,[],null,null,false,false,"test",CaptureEnabled:true));
        Assert.True(vm.MicMeterActive,"applied mic can show real level stream");
        vm.Mic=false;Assert.Equal("pending",vm.MicFeedbackState);Assert.True(!vm.MicMeterActive,"unapplied disable is clearly pending");
    }
    [Test]
    public static void Source_meter_uses_its_own_level_and_clears_unapplied_changes()
    {
        var vm=Model();vm.UseApplicationAudio=true;vm.GameAudioEnabled=true;
        var row=vm.GameAudioRow!;vm.SeedReplay(vm.ReplayEnabled);
        vm.SeedStatus(new(null,[],null,null,false,false,"test",CaptureEnabled:true,AudioSources:[new(row.Id,"available",-12,"game.exe")]));
        Assert.True(vm.GameMeterActive,"valid applied game");Assert.Equal(-12d,vm.GamePeakDb);
        vm.GameAudioEnabled=false;
        Assert.Equal("pending",vm.GameFeedbackState);Assert.True(!vm.GameMeterActive,"pending game does not show a live level");Assert.Equal(LevelMeter.Silent,vm.GamePeakDb);
    }
    [Test]
    public static void Removing_game_releases_its_application_slot()
    {
        var vm=Model();vm.UseApplicationAudio=true;vm.GameAudioEnabled=true;
        var candidates=Enumerable.Range(0,4).Select(i=>new AudioTargetCandidate(new($@"C:\Apps\App{i}.exe",$"App{i}.exe"),$"app{i}",10+i)).ToArray();
        vm.Seed(null,new([],[],[],[],[],"test",candidates,true));
        foreach(var candidate in candidates.Take(3)){vm.SelectedAudioCandidate=candidate;vm.AddAudioApplicationCommand.Execute(null);}
        var row=vm.GameAudioRow!;vm.GameAudioEnabled=false;
        Assert.True(vm.HasGameAudio,"disabled source preserves identity until explicitly removed");
        vm.SelectedAudioCandidate=candidates[3];Assert.True(!vm.CanAddAudioApplication,"four slots occupied");
        vm.RemoveAudioApplicationCommand.Execute(row);
        Assert.True(!vm.HasGameAudio && vm.CanAddAudioApplication,"remove releases slot");
        vm.AddAudioApplicationCommand.Execute(null);
        Assert.Equal(4,vm.AppAudioRows.Count);
    }
    static void OnSta(Action action)
    {
        Exception? failure=null;var thread=new Thread(()=>{try{action();}catch(Exception e){failure=e;}});
        thread.SetApartmentState(ApartmentState.STA);thread.Start();thread.Join();Assert.True(failure==null,failure?.ToString()??"");
    }
    [Test]
    public static void Bound_meter_animates_and_never_opens_a_capture_or_level_connection()=>OnSta(()=>
    {
        int connections=0;LevelBar.StreamFactory=()=>{connections++;return new LevelStream(connect:false);};
        try
        {
            var meter=new LevelBar{ExternalDb=-12,IsMonitoringActive=true};meter.Attach();meter.Frame(TimeSpan.FromSeconds(1));
            Assert.Equal(0,connections);Assert.Near(-12,meter.BarDb,0.001);Assert.Equal("receiving",meter.SignalState);
            meter.ExternalDb=LevelMeter.Silent;meter.Frame(TimeSpan.FromSeconds(2));Assert.Equal("silent",meter.SignalState);
            meter.IsMonitoringActive=false;Assert.Equal("inactive",meter.SignalState);Assert.Equal(LevelMeter.Silent,meter.BarDb);
            meter.Detach();Assert.True(!meter.Rendering,"no animation retained after unload");
        }
        finally{LevelBar.StreamFactory=()=>new LevelStream();}
    });
}
