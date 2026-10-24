using System.Text;
using System.Text.Json;

namespace Kairos.Engine.Audio;

public static class AudioLayoutMetadata
{
    public const string Prefix = "ReplayLocalAudio/1:";
    public static AudioLayout? TryRead(string json, IReadOnlyList<int> actualAudioIndexes)
    {
        if (json.Length > 16384 || Encoding.UTF8.GetByteCount(json) > 16384) return null;
        try
        {
            var layout = JsonSerializer.Deserialize<AudioLayout>(json);
            if (layout is not { Version: 1, Tracks.Count: > 0 and <= 6 }) return null;
            if (layout.Tracks.Count(t => t?.Role == AudioRole.Mix) > 1) return null;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var indexes = new HashSet<int>();
            foreach (var t in layout.Tracks)
                if (t == null || !Enum.IsDefined(t.Role) || string.IsNullOrWhiteSpace(t.Id) || t.Id.Length > 80
                    || t.Id.Any(char.IsControl) || string.IsNullOrWhiteSpace(t.Name) || t.Name.Length > 80 || t.Name.Any(char.IsControl)
                    || !ids.Add(t.Id) || !indexes.Add(t.StreamIndex) || !actualAudioIndexes.Contains(t.StreamIndex)) return null;
            if (layout.Tracks.Count != actualAudioIndexes.Count) return null;
            return layout;
        }
        catch (JsonException) { return null; }
    }

    public static string Write(AudioLayout layout)
    {
        string json = JsonSerializer.Serialize(layout);
        if (TryRead(json, layout.Tracks.Select(t => t.StreamIndex).ToArray()) == null)
            throw new ArgumentException("Invalid audio layout", nameof(layout));
        return Prefix + json;
    }
}
