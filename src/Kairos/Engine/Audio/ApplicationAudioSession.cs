// Replay Local: modified 2026-10-24; independent fork of BasilForge/Kairos. See MODIFICATIONS.md. GPL-2.0-or-later.
using Kairos.Settings;

namespace Kairos.Engine.Audio;

public sealed record AudioSourceStatus(string Id, string State, double PeakDb, string? ApplicationName=null);
public interface IApplicationAudioBackend
{
    IDisposable Create(string id, string windowToken, uint mask, double volume);
    IDisposable Create(string id, AudioTargetCandidate candidate, uint mask, double volume) => Create(id,candidate.WindowToken,mask,volume);
    double PeakDb(string id) => LevelMeter.Silent;
}

/// <summary>Called on its owner thread. A synchronous/reentrant Disable also revokes resources still being created.</summary>
public sealed class ApplicationAudioSession : IDisposable
{
    readonly ApplicationAudioSettings[] _apps;
    readonly IApplicationAudioBackend _backend;
    readonly Dictionary<string, (string Token, int ProcessId, string Path, IDisposable Resource)> _active = [];
    readonly Dictionary<string, string> _states = [];
    readonly Dictionary<string,string> _names=[];
    readonly AutomaticGameAudio _game=new();
    bool _disposed;

    public ApplicationAudioSession(AudioSettings settings, IApplicationAudioBackend backend)
    {
        _apps = settings.Applications.Select(a => new ApplicationAudioSettings { Id = a.Id, AutomaticGame=a.AutomaticGame, Target = a.Target, Name = a.Name, Enabled = a.Enabled, Volume = a.Volume }).ToArray();
        _backend = backend;
    }
    public IReadOnlyList<AudioSourceStatus> Status => _apps.Select(a => new AudioSourceStatus(a.Id,
        _states.GetValueOrDefault(a.Id, a.Enabled ? "unavailable" : "disabled"), _active.ContainsKey(a.Id) ? _backend.PeakDb(a.Id) : LevelMeter.Silent,_names.GetValueOrDefault(a.Id))).ToArray();

    public void Refresh(IReadOnlyList<AudioTargetCandidate> candidates,AudioTargetCandidate? fullscreen=null)
    {
        if (_disposed) return;
        var explicitApps=_apps.Where(a=>!a.AutomaticGame && a.Enabled).ToArray();
        var explicitPaths=explicitApps.Select(a=>a.Target.ExecutablePath).Concat(candidates.Where(c=>explicitApps.Any(a=>
            ApplicationAudioIdentity.Matches(a.Target,c.Target))).Select(c=>c.Target.ExecutablePath)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var game=_game.Update(candidates,fullscreen,explicitPaths);
        for (int i = 0; i < _apps.Length && !_disposed; i++)
        {
            var a = _apps[i];
            AudioTargetCandidate? candidate=null;
            AudioTargetResolution r;
            if(!a.Enabled)r=new("disabled",null);
            else if(a.AutomaticGame){candidate=game;r=new(game==null?"waitingGame":"available",game?.WindowToken);}
            else
            {
                r=ApplicationAudioTargets.Resolve(a.Target,candidates);
                candidate=r.Candidate;
            }
            if(candidate!=null)_names[a.Id]=candidate.Target.ExecutableName;else _names.Remove(a.Id);
            _states[a.Id] = r.State;
            if (_active.TryGetValue(a.Id, out var old))
            {
                if (old.Token == r.WindowToken && old.ProcessId == (candidate?.ProcessId ?? 0) && string.Equals(old.Path,candidate?.Target.ExecutablePath,StringComparison.OrdinalIgnoreCase)) continue;
                _active.Remove(a.Id);
                old.Resource.Dispose();
            }
            if (r.WindowToken == null) continue;
            try
            {
                var resource = _backend.Create(a.Id, candidate!, 1u | (1u << (i + 1)), a.Volume);
                if (_disposed) resource.Dispose();
                else _active.Add(a.Id, (r.WindowToken, candidate!.ProcessId, candidate.Target.ExecutablePath, resource));
            }
            catch (Exception e) when (e is InvalidOperationException or ArgumentException)
            { _states[a.Id] = "incompatible"; ObsLog.Warn("Application audio source could not start."); }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var r in _active.Values) r.Resource.Dispose();
        _active.Clear();
    }
}
