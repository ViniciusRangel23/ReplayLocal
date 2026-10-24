// Replay Local: modified 2026-10-23; independent fork of BasilForge/Kairos. See MODIFICATIONS.md. GPL-2.0-or-later.
using System.Reflection;
using System.Text.RegularExpressions;

namespace Kairos;

/// <summary>Every name the app registers with Windows, in one place so a rename is a single edit.</summary>
public static class AppInfo
{
    public const string Name = "Replay Local";
    public const string StorageName = "ReplayLocal";
    public const string EngineMutex = @"Local\ReplayLocal.Engine";
    public const string SettingsMutex = @"Local\ReplayLocal.Settings";
    public const string SettingsShowEvent = @"Local\ReplayLocal.Settings.Show";
    public const string PipeName = "ReplayLocal";
    public const string RunValueName = "ReplayLocal";

    /// <summary>The full version, a prerelease part included ("1.0.0-beta.1"), without the build details after "+".</summary>
    public static string Version { get; } =
        (typeof(AppInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0").Split('+')[0];

    /// <summary>A version as people read it: "1.0.0-beta.3" is "1.0.0 beta 3" in their language; any other stays as it is.</summary>
    public static string Display(string version) => Regex.Match(version, @"^(\d+\.\d+\.\d+)-beta\.(\d+)$") is { Success: true } m
        ? App.Text.Format("version.beta", m.Groups[1].Value, m.Groups[2].Value)
        : version;
}
