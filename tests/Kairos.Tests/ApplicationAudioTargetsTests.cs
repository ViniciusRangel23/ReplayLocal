using Kairos.Engine.Audio;
using Kairos.Settings;

namespace Kairos.Tests;

public static class ApplicationAudioTargetsTests
{
    [Test]
    public static void Same_name_other_path_is_not_selected()
    {
        var target = new ApplicationAudioTarget(@"C:\A\game.exe", "game.exe");
        Assert.Equal("unavailable", ApplicationAudioTargets.Resolve(target, [new(new(@"C:\B\game.exe", "game.exe"), "token")]).State);
    }
    [Test]
    public static void New_pid_same_target_reconnects()
    {
        var target = new ApplicationAudioTarget(@"C:\A\game.exe", "game.exe");
        Assert.Equal("new-token", ApplicationAudioTargets.Resolve(target, [new(target, "new-token")]).WindowToken);
    }
    [Test]
    public static void Ambiguous_target_stays_disconnected()
    {
        var target = new ApplicationAudioTarget(@"C:\A\game.exe", "game.exe");
        var result = ApplicationAudioTargets.Resolve(target, [new(target, "one"), new(target, "two")]);
        Assert.True(result.State == "ambiguous" && result.WindowToken == null, "never choose arbitrary instance");
    }
    [Test]
    public static void Identical_window_tokens_in_distinct_processes_are_ambiguous()
    {
        var target=new ApplicationAudioTarget(@"C:\A\game.exe","game.exe");
        Assert.Equal("ambiguous",ApplicationAudioTargets.Resolve(target,[new(target,"same",10),new(target,"same",11)]).State);
    }
    [Test]
    public static void Input_method_and_tool_windows_are_not_application_instances()
    {
        Assert.True(!ApplicationAudioWindows.IsCaptureWindow("IME",0x88000000,0),"renderer IME is not a session");
        Assert.True(!ApplicationAudioWindows.IsCaptureWindow("MSCTFIME UI",0x88000000,0),"text service helper");
        Assert.True(!ApplicationAudioWindows.IsCaptureWindow("OleDdeWndClass",0x80000000,0),"OLE helper");
        Assert.True(!ApplicationAudioWindows.IsCaptureWindow("GDI+ Hook Window Class",0x80000000,0),"drawing helper");
        Assert.True(!ApplicationAudioWindows.IsCaptureWindow("DiscordDesktopOverlayInputTrap",0x80000000,0x80),"overlay tool window");
        Assert.True(!ApplicationAudioWindows.IsCaptureWindow("Chrome_RenderWidgetHostHWND",0x40000000,0),"child renderer");
        Assert.True(ApplicationAudioWindows.IsCaptureWindow("Chrome_WidgetWin_1",0x16CF0000,0),"real Discord/Spotify window");
        Assert.True(ApplicationAudioWindows.IsCaptureWindow("UnrealWindow",0x80000000,0),"borderless game window");
    }
    [Test]
    public static void Prefer_visible_window_within_one_process_without_hiding_real_ambiguity()
    {
        var target=new ApplicationAudioTarget(@"C:\Apps\app.exe","app.exe");
        var hidden=new AudioTargetCandidate(target,"hidden",10,IsVisible:false);
        var visible=new AudioTargetCandidate(target,"main",10);
        Assert.Equal("main",ApplicationAudioTargets.Resolve(target,[hidden,visible]).WindowToken);
        Assert.Equal("hidden",ApplicationAudioTargets.Resolve(target,[hidden]).WindowToken);
        Assert.Equal("ambiguous",ApplicationAudioTargets.Resolve(target,[hidden,visible with {ProcessId=11}]).State);
    }
}
