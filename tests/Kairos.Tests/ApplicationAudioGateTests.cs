using Kairos.Engine.Audio;
namespace Kairos.Tests;
public static class ApplicationAudioGateTests
{
    [Test]
    public static void Foreign_reconnect_and_exited_process_never_pass_audio()
    {
        var target=new AudioTargetCandidate(new(@"C:\A\game.exe","game.exe"),"title:class:game.exe",10);
        Assert.True(ApplicationAudioGate.Allows(target,"title:class:game.exe",[target],true),"selected live identity");
        Assert.True(!ApplicationAudioGate.Allows(target,"title:class:game.exe",[target with {ProcessId=11,Target=new(@"C:\B\game.exe","game.exe")}],true),"basename replacement rejected");
        Assert.True(!ApplicationAudioGate.Allows(target,"title:class:game.exe",[target],false),"dead original handle rejects even reused PID");
        Assert.True(!ApplicationAudioGate.Allows(target,"title:class:game.exe",[target,target with {ProcessId=11}],true),"indistinguishable hook rejected");
        Assert.True(!ApplicationAudioGate.Allows(target,"other:class:game.exe",[target],true),"wrong hooked window rejected");
    }
}
