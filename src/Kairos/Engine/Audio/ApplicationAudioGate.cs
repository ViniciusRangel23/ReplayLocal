namespace Kairos.Engine.Audio;

/// <summary>Fail closed for every audio block, independently of owner-thread refresh/reconnection.</summary>
public static class ApplicationAudioGate
{
    public static bool Allows(AudioTargetCandidate expected,string hookedToken,IReadOnlyList<AudioTargetCandidate> windows,bool originalProcessAlive)
    {
        if(!originalProcessAlive || expected.ProcessId<=0 || string.IsNullOrEmpty(hookedToken)) return false;
        var matching=windows.Where(w=>w.WindowToken==hookedToken).ToArray();
        return matching.Length>0 && matching.All(w=>w.ProcessId==expected.ProcessId &&
            string.Equals(w.Target.ExecutablePath,expected.Target.ExecutablePath,StringComparison.OrdinalIgnoreCase));
    }
}
