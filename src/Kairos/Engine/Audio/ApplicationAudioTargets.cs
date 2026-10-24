// Replay Local: modified 2026-10-24; independent fork of BasilForge/Kairos. See MODIFICATIONS.md. GPL-2.0-or-later.
using Kairos.Settings;

namespace Kairos.Engine.Audio;

public sealed record AudioTargetCandidate(ApplicationAudioTarget Target, string WindowToken, int ProcessId = 0, bool IsVisible = true);
public sealed record AudioTargetResolution(string State, string? WindowToken, AudioTargetCandidate? Candidate=null);

public static class ApplicationAudioTargets
{
    public static AudioTargetResolution Resolve(ApplicationAudioTarget target, IReadOnlyList<AudioTargetCandidate> candidates)
    {
        if (string.IsNullOrWhiteSpace(target.ExecutablePath)) return new("unavailable", null);
        var matching = candidates.Where(c => string.Equals(c.Target.ExecutablePath, target.ExecutablePath, StringComparison.OrdinalIgnoreCase) ||
            (c.ProcessId>0 && ApplicationAudioIdentity.Matches(target,c.Target))).Where(c=>!string.IsNullOrWhiteSpace(c.WindowToken)).ToArray();
        if(matching.Select(c=>c.Target.ExecutablePath).Distinct(StringComparer.OrdinalIgnoreCase).Count()>1 &&
            matching.Select(c=>c.ProcessId).Distinct().Count()==1)return new("ambiguous",null);
        if(matching.Select(c=>c.ProcessId).Where(pid=>pid>0).Distinct().Count()>1)return new("ambiguous",null);
        if(matching.Length>1 && matching.All(c=>c.ProcessId>0) && matching.Select(c=>c.ProcessId).Distinct().Count()==1)
        {
            var selected=matching.OrderByDescending(c=>c.IsVisible).First();
            return new("available",selected.WindowToken,selected);
        }
        var matches = matching
            .Select(c => c.WindowToken).Where(t => !string.IsNullOrWhiteSpace(t)).Distinct(StringComparer.Ordinal).ToArray();
        return matches.Length switch { 0 => new("unavailable", null), 1 => new("available", matches[0],matching.First(c=>c.WindowToken==matches[0])), _ => new("ambiguous", null) };
    }
}
