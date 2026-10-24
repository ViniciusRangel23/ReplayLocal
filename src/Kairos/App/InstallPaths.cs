// Replay Local: modified 2026-10-23; independent fork of BasilForge/Kairos. See MODIFICATIONS.md. GPL-2.0-or-later.
namespace Kairos.App;

/// <summary>Portable application paths; no installer, updater or uninstall hooks.</summary>
public static class InstallPaths
{
    public static string Root { get; } = AppContext.BaseDirectory;
    public static string LaunchExe => Environment.ProcessPath!;
    public static bool IsInside(string root, string path)
    {
        string r = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        string p = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        return p.Equals(r, StringComparison.OrdinalIgnoreCase) || p.StartsWith(r + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The ids of Kairos processes running from <paramref name="root"/>, this one left out.</summary>
    public static IEnumerable<int> OwnProcesses(string root, IEnumerable<(int Id, string? Path)> processes, int self) =>
        processes.Where(p => p.Id != self && p.Path != null && IsInside(root, p.Path)).Select(p => p.Id);


}
