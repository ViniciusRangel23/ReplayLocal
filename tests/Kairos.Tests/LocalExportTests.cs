using Kairos.Engine.Share;
using Kairos.Ui;
namespace Kairos.Tests;
public static class LocalExportTests
{
    [Test] public static void Unicode_destination_and_collision_preserve_source()
    {
        string dir = Path.Combine(Path.GetTempPath(), "replay-local-tests", Guid.NewGuid().ToString("N"), "edição local");
        Directory.CreateDirectory(dir);
        try
        {
            string source = Path.Combine(dir, "origem.mp4"), target = Path.Combine(dir, "cópia.mp4"), temp = Path.Combine(dir, "novo.part");
            File.WriteAllText(source, "original"); File.WriteAllText(target, "previous"); File.WriteAllText(temp, "export");
            using (var held = File.Open(target, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                string placed = ClipTranscoder.Place(temp, target);
                Assert.True(placed != target, "in-use copy uses another name");
                Assert.Equal("export", File.ReadAllText(placed), "export preserved");
            }
            Assert.Equal("original", File.ReadAllText(source), "source unchanged");
            Assert.Equal("previous", File.ReadAllText(target), "in-use destination unchanged");
        }
        finally { Directory.Delete(dir, true); }
    }
    [Test] public static void Copy_has_no_launch_or_publish_authority()
    {
        Assert.Equal("Copy", string.Join(",", Enum.GetNames<SharePlatform>()), "local destinations only");
        Assert.True(!typeof(IShareHost).GetMethods().Any(m => m.Name is "Launch" or "Publish" or "RevealSite"), "local host has no network authority");
    }
}
