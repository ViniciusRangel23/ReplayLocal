// Replay Local: modified 2026-10-23; independent fork of BasilForge/Kairos. See MODIFICATIONS.md. GPL-2.0-or-later.
using Kairos.App;
using Kairos.Engine;

namespace Kairos.Tests;

/// <summary>The installed Kairos (Velopack's folder): what is inside it, which processes are its own, where clips may go.</summary>
public static class InstallPathsTests
{
    [Test]
    public static void A_folder_inside_the_install_is_inside()
    {
        const string root = @"C:\U\AppData\Local\Kairos";
        Assert.True(InstallPaths.IsInside(root, @"C:\U\AppData\Local\Kairos\clips"), "a folder in it");
        Assert.True(InstallPaths.IsInside(root, @"c:\u\appdata\local\KAIROS\"), "the folder itself, any case, trailing slash");
        Assert.True(!InstallPaths.IsInside(root, @"C:\U\AppData\Local\KairosOther\clips"), "a folder that only starts the same");
        Assert.True(!InstallPaths.IsInside(root, @"D:\Clips"), "elsewhere");
    }

[Test]
    public static void Uninstall_stops_only_this_installs_processes()
    {
        var running = new (int Id, string? Path)[]
        {
            (1, @"C:\L\Kairos\current\Kairos.exe"), (2, @"D:\Kairos\bin\64bit\Kairos.exe"), (3, null), (9, @"C:\L\Kairos\current\Kairos.exe"),
        };
        Assert.Equal("1", string.Join(",", InstallPaths.OwnProcesses(@"C:\L\Kairos", running, self: 9)), "this install's, not itself, not another copy");
    }

    /// <summary>An uninstall removes the start-with-Windows entry only when it starts this install (not another copy's).</summary>
    [Test]
    public static void Uninstall_removes_only_its_own_autostart()
    {
        Assert.True(Autostart.PointsInto("\"C:\\L\\Kairos\\Kairos.exe\" --background", @"C:\L\Kairos"), "this install's stub");
        Assert.True(!Autostart.PointsInto("\"D:\\Kairos\\bin\\64bit\\Kairos.exe\" --background", @"C:\L\Kairos"), "another copy");
        Assert.True(!Autostart.PointsInto(null, @"C:\L\Kairos"), "none");
    }

/// <summary>Uninstalling removes the whole install folder, so clips must never be kept inside it.</summary>
    [Test]
    public static void Clips_are_never_kept_inside_the_install()
    {
        string root = Path.Combine(Path.GetTempPath(), "kairos-tests", "install-" + Guid.NewGuid().ToString("N"));
        string elsewhere = Path.Combine(Path.GetTempPath(), "kairos-tests", "clips-" + Guid.NewGuid().ToString("N"));
        try
        {
            Assert.True(!ClipOrganizer.Usable(Path.Combine(root, "clips"), root), "inside the install: refused");
            Assert.True(ClipOrganizer.Usable(elsewhere, root), "elsewhere: kept");
            Assert.True(ClipOrganizer.Usable(elsewhere, installRoot: null), "a development build has no install");
        }
        finally
        {
            foreach (string dir in new[] { root, elsewhere })
                if (Directory.Exists(dir))
                    Directory.Delete(dir, recursive: true);
        }
    }
}

