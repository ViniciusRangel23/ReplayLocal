// Replay Local: modified 2026-10-24; independent fork of BasilForge/Kairos. See MODIFICATIONS.md. GPL-2.0-or-later.
namespace Kairos.Settings;

public enum AudioCaptureMode { Legacy, Applications }
public sealed record ApplicationAudioTarget(string ExecutablePath, string ExecutableName);

public sealed class ApplicationAudioSettings
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public ApplicationAudioTarget Target { get; set; } = new("", "");
    public string Name { get; set; } = "";
    public bool AutomaticGame { get; set; }
    public bool Enabled { get; set; } = true;
    public double Volume { get; set; } = 1;

    public static string? Validate(AudioSettings audio)
    {
        if (!Enum.IsDefined(audio.Mode)) return "Invalid audio mode.";
        if (audio.Applications == null || audio.Applications.Count > 4) return "Select at most four applications.";
        int automaticCount=0;
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in audio.Applications)
        {
            if (a == null || string.IsNullOrWhiteSpace(a.Id) || a.Id.Length > 80 || !ids.Add(a.Id) || a.Id == "microphone" || a.Id == "mix") return "Invalid or duplicate source ID.";
            if(a.AutomaticGame)
            {
                if(++automaticCount>1)return "Select only one automatic game source.";
            }
            else if (a.Target == null || string.IsNullOrWhiteSpace(a.Target.ExecutablePath) || !Path.IsPathFullyQualified(a.Target.ExecutablePath)
                || !targets.Add(ApplicationAudioIdentity.Key(a.Target))) return "Invalid or duplicate application.";
            if (string.IsNullOrWhiteSpace(a.Name) || a.Name.Length > 80 || a.Name.Any(char.IsControl)) return "Source names must contain 1 to 80 printable characters.";
            if (!double.IsFinite(a.Volume)) return "Invalid source volume.";
        }
        return null;
    }
}
