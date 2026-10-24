// Replay Local: modified 2026-10-23; independent fork of BasilForge/Kairos. See MODIFICATIONS.md. GPL-2.0-or-later.
using Kairos.App;
using Kairos.Engine.Share;
using Kairos.Settings;

namespace Kairos.Tests;

public static class ShareSettingsTests
{
    [Test]
    public static void Remembering_share_choices_keeps_the_replay_buffer()
    {
        var before = new AppSettings();
        var after = SettingsStore.Deserialize(SettingsStore.Serialize(before));
        after.Share.Tags = "#fps";
        Assert.Equal(false, SettingsDiff.NeedsRebuild(before, after), "no pipeline rebuild");
    }

    [Test]
    public static void The_tray_hands_a_clip_to_the_settings_window_once()
    {
        string file = Path.Combine(Path.GetTempPath(), "kairos-tests", Guid.NewGuid().ToString("N"), "share-request.txt");
        var now = new DateTime(2026, 10, 13, 12, 0, 0, DateTimeKind.Utc);
        ShareRequest.Write(file, @"D:\Clips\لعبة\clip.mp4", now);
        Assert.Equal(@"D:\Clips\لعبة\clip.mp4", ShareRequest.Take(file, now.AddSeconds(2)), "taken");
        Assert.Equal(null, ShareRequest.Take(file, now.AddSeconds(3)), "only once");

        ShareRequest.Write(file, @"D:\Clips\old.mp4", now);
        Assert.Equal(null, ShareRequest.Take(file, now.AddMinutes(2)), "a stale request is ignored");
        Assert.True(!File.Exists(file), "stale request removed");
    }
}
