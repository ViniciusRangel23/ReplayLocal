using Kairos.Settings;

namespace Kairos.Engine.Audio;

public sealed record AudioRoute(string SourceId, int MixerIndex, uint MixerMask);

public static class AudioRoutingPlan
{
    public static IReadOnlyList<AudioRoute> Build(AppSettings s)
    {
        var a = s.Audio;
        var routes = new List<AudioRoute>();
        if (a.Mode == AudioCaptureMode.Applications)
        {
            if (ApplicationAudioSettings.Validate(a) is { } error) throw new ArgumentException(error);
            for (int i = 0; i < a.Applications.Count; i++)
                routes.Add(new(a.Applications[i].Id, i + 1, a.Applications[i].Enabled ? 1u | (1u << (i + 1)) : 0u));
        }
        else if (a.System || a.SourceAudioOnly) routes.Add(new("system", 1, a.SeparateTracks ? 3u : 1u));
        if (a.Mic && a.MicConfirmed && !string.IsNullOrWhiteSpace(a.MicDevice) && a.MicDevice != "default")
        {
            int mixer = a.Mode == AudioCaptureMode.Applications ? 5 : 2;
            routes.Add(new("microphone", mixer, a.Mode == AudioCaptureMode.Applications || a.SeparateTracks ? 1u | (1u << mixer) : 1u));
        }
        return routes;
    }
}
