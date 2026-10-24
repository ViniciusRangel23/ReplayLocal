using Kairos.Settings;
using Kairos.App;
namespace Kairos.Tests;
public static class CaptureLifecycleTests
{
    sealed class Session(List<string> calls) : ICaptureSession
    {
        public bool Recording => true;
        public void StopRecording() => calls.Add("stop");
        public void SetReplayActive(bool active) => calls.Add(active ? "resume" : "pause");
        public void Dispose() => calls.Add("dispose");
    }
    [Test] public static void Pause_keeps_session_but_disable_disposes_in_order()
    {
        List<string> calls = []; int creates = 0;
        var c = new CaptureController(() => { creates++; return new Session(calls); }, () => calls.Add("voice-off"));
        Assert.Equal(null, c.Session, "construction does not start capture");
        c.Enable(); c.Enable(); c.SetReplayPaused(true);
        Assert.Equal(1, creates, "enable is idempotent");
        Assert.Equal("pause", string.Join(",", calls), "pause keeps sources");
        c.Disable();
        Assert.Equal("pause,voice-off,stop,dispose", string.Join(",", calls), "finish recording before releasing sources");
        Assert.Equal(null, c.Session, "disabled has no sources");
    }
    [Test] public static void Fresh_and_legacy_settings_require_confirmation()
    {
        foreach (string json in new[] { "{}", "{\"capture\":{},\"audio\":{\"mic\":true},\"general\":{\"startWithWindows\":true}}" })
        {
            var s = SettingsStore.Deserialize(json);
            Assert.True(!s.Audio.Mic && !s.General.StartWithWindows, "unconfirmed settings cannot capture mic or autostart");
        }
    }
    [Test] public static void Mic_requires_explicit_device_selection()
    {
        var s = new AppSettings(); s.Audio.Mic = true; s.Normalize();
        Assert.True(!s.Audio.Mic, "default device is not microphone consent");
    }
}
