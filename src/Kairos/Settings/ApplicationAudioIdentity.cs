// Replay Local: added 2026-10-24. GPL-2.0-or-later. See MODIFICATIONS.md.
namespace Kairos.Settings;

/// <summary>Reconnect only inside the selected installation, never by executable basename alone.</summary>
public static class ApplicationAudioIdentity
{
    public static bool Matches(ApplicationAudioTarget selected,ApplicationAudioTarget current)=>
        string.Equals(Key(selected),Key(current),StringComparison.OrdinalIgnoreCase);

    public static string Key(ApplicationAudioTarget target)
    {
        string path=Path.GetFullPath(target.ExecutablePath);
        string name=Path.GetFileName(path);
        var directory=Directory.GetParent(path);
        if(directory==null || !string.Equals(name,target.ExecutableName,StringComparison.OrdinalIgnoreCase))return path;
        var installation=directory.Parent;
        if(installation!=null && name.Equals("Discord.exe",StringComparison.OrdinalIgnoreCase) &&
            installation.Name.Equals("Discord",StringComparison.OrdinalIgnoreCase) &&
            directory.Name.StartsWith("app-",StringComparison.OrdinalIgnoreCase) && VersionParts(directory.Name[4..],3))
            return "discord-install:"+installation.FullName;
        if(installation!=null && name.Equals("Spotify.exe",StringComparison.OrdinalIgnoreCase) &&
            installation.Name.Equals("WindowsApps",StringComparison.OrdinalIgnoreCase))
        {
            string[] parts=directory.Name.Split('_');
            if(parts.Length==5 && parts[0].Equals("SpotifyAB.SpotifyMusic",StringComparison.OrdinalIgnoreCase) &&
                VersionParts(parts[1],4) && parts[2].ToLowerInvariant() is "x64" or "x86" or "arm64" && parts[3].Length==0 &&
                parts[4].Equals("zpdnekdrzrea0",StringComparison.OrdinalIgnoreCase))
                return "spotify-store:"+installation.FullName+"|"+parts[2]+"|"+parts[4];
        }
        return path;
    }
    static bool VersionParts(string value,int count)=>value.Split('.').Length==count &&
        value.Split('.').All(p=>p.Length>0 && p.All(char.IsAsciiDigit)) && Version.TryParse(value,out _);
}
