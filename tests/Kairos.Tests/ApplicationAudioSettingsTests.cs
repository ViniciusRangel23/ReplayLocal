using Kairos.Settings;
using Kairos.Engine.Audio;

namespace Kairos.Tests;

public static class ApplicationAudioSettingsTests
{
    public static AppSettings Config(int count = 3)
    {
        var s = new AppSettings();
        s.Audio.Mode = AudioCaptureMode.Applications;
        s.Audio.Applications = Enumerable.Range(0, count).Select(i => new ApplicationAudioSettings {
            Id = "app-" + i, Name = "App " + i, Enabled = true, Volume = 1,
            Target = new(@"C:\Apps\app" + i + ".exe", "app" + i + ".exe") }).ToList();
        return s;
    }

    [Test]
    public static void Old_settings_keep_legacy_mode_and_microphone_off()
    {
        var s = SettingsStore.Deserialize("{}");
        Assert.Equal(AudioCaptureMode.Legacy, s.Audio.Mode);
        Assert.True(!s.Audio.Mic && s.Audio.Applications.Count == 0, "no new capture implied");
    }

    [Test]
    public static void Rejects_fifth_duplicate_or_nonfinite_source()
    {
        Assert.True(ApplicationAudioSettings.Validate(Config(5).Audio) != null, "reject fifth source");
        var s = Config();
        s.Audio.Applications[1].Target = s.Audio.Applications[0].Target;
        Assert.True(ApplicationAudioSettings.Validate(s.Audio) != null, "reject duplicate target");
        s = Config(); s.Audio.Applications[0].Volume = double.NaN;
        Assert.True(ApplicationAudioSettings.Validate(s.Audio) != null, "reject NaN");
        s = Config(); s.Audio.Applications[0].Volume = 5;
        s.Normalize();
        Assert.Equal(2d, s.Audio.Applications[0].Volume);
    }
}
