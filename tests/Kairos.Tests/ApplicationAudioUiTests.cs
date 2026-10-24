using Kairos.Ui;
using Kairos.App;
using Kairos.Engine.Audio;
using Kairos.Settings;

namespace Kairos.Tests;

public static class ApplicationAudioUiTests
{
    [Test]
    public static void Adding_apps_respects_limit_duplicates_and_consent()
    {
        var vm=new SettingsViewModel();
        var candidates=Enumerable.Range(0,5).Select(i=>new AudioTargetCandidate(new(@"C:\Apps\app"+i+".exe","app"+i+".exe"),"token"+i)).ToArray();
        vm.Seed(null,new([],[],[],[],[],"test",candidates,true));
        vm.UseApplicationAudio=true;
        foreach(var c in candidates){vm.SelectedAudioCandidate=c;vm.AddAudioApplicationCommand.Execute(null);}
        Assert.Equal(4,vm.ApplicationAudioRows.Count);
        Assert.True(!vm.CanAddAudioApplication&&!vm.Mic&&!vm.CaptureEnabled,"adding rows does not start capture");
        vm.SelectedAudioCandidate=candidates[0];vm.AddAudioApplicationCommand.Execute(null);
        Assert.Equal(4,vm.ApplicationAudioRows.Count);
        vm.ApplicationAudioRows[0].Name="Jogo";vm.ApplicationAudioRows[0].VolumePercent=50;
        Assert.Equal(50d,vm.ApplicationAudioRows[0].VolumePercent);
        Assert.True(vm.IsDirty,"row editing is tracked");
    }
    [Test]
    public static void Unsupported_mode_preserves_legacy()
    {
        var vm=new SettingsViewModel();vm.Seed(null,new([],[],[],[],[],"test"));
        vm.UseApplicationAudio=true;
        Assert.True(!vm.UseApplicationAudio,"unsupported mode not selected");
    }
}
