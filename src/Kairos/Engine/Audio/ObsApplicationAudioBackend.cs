using Kairos.Native;

namespace Kairos.Engine.Audio;

sealed class ObsApplicationAudioBackend(IReadOnlyList<string> sourceIds) : IApplicationAudioBackend
{
    readonly Dictionary<string, Source> _active = [];
    bool _muted;
    internal long MeterUpdates=>_active.Values.Sum(s=>s.MeterUpdates);
    internal long MeterUpdatesFor(string id)=>_active.TryGetValue(id,out var source)?source.MeterUpdates:0;
    internal long RejectedBlocksFor(string id)=>_active.TryGetValue(id,out var source)?source.RejectedBlocks:0;
    public IDisposable Create(string id, string windowToken, uint mask, double volume) => throw new InvalidOperationException("Validated process identity required");
    public IDisposable Create(string id, AudioTargetCandidate candidate, uint mask, double volume)
    {
        int slot = sourceIds.ToList().IndexOf(id);
        if (slot < 0 || slot > 3) throw new ArgumentException("Unknown application audio slot");
        var source = new Source(id, candidate, mask, volume, (uint)(4 + slot), () => _active.Remove(id));
        _active[id] = source;
        source.SetMuted(_muted);
        return source;
    }
    public double PeakDb(string id) => _active.TryGetValue(id, out var source) ? source.Level.Take() : LevelMeter.Silent;
    public void SetMuted(bool muted) { _muted = muted; foreach (var s in _active.Values) s.SetMuted(muted); }

    sealed class Source : IDisposable
    {
        IntPtr _source, _meter;
        readonly ObsVolmeterCallback _callback;
        ObsProcessAudioGate? _gate;
        readonly uint _channel;
        readonly Action _released;
        public LevelMeter Level { get; } = new();
        long _updates;
        public long MeterUpdates=>Interlocked.Read(ref _updates);
        public long RejectedBlocks=>_gate?.Rejected??0;
        public unsafe Source(string id, AudioTargetCandidate candidate, uint mask, double volume, uint channel, Action released)
        {
            _channel = channel; _released = released;
            _callback = (_, _, peak, _) => { var p = (float*)peak; Level.Add(Math.Max(p[0], p[1]));Interlocked.Increment(ref _updates); };
            try
            {
                // An impossible initial window prevents any packets before the guard is installed.
                using var d = new ObsData().Set("window", "ReplayLocal-"+Guid.NewGuid().ToString("N")+":none:none.exe").Set("priority", 0L);
                _source = Obs.obs_source_create("wasapi_process_output_capture", "Replay audio " + id, d.Handle, IntPtr.Zero);
                if (_source == IntPtr.Zero) throw new InvalidOperationException("Application audio unsupported");
                Obs.obs_source_set_audio_mixers(_source, 0);
                _gate=new(_source,candidate);
                using var selected=new ObsData().Set("window",candidate.WindowToken).Set("priority",2L);
                Obs.obs_source_update(_source,selected.Handle);
                Obs.obs_source_set_audio_mixers(_source, mask);
                Obs.obs_source_set_volume(_source, (float)volume);
                _meter = Obs.obs_volmeter_create(1);
                if (_meter != IntPtr.Zero)
                {
                    Obs.obs_volmeter_add_callback(_meter, _callback, IntPtr.Zero);
                    Obs.obs_volmeter_attach_source(_meter, _source);
                }
                Obs.obs_set_output_source(channel, _source);
            }
            catch (System.ComponentModel.Win32Exception e) { Dispose(); throw new InvalidOperationException("Application identity unavailable",e); }
            catch { Dispose(); throw; }
        }
        public void SetMuted(bool muted) { if (_source != IntPtr.Zero) Obs.obs_source_set_muted(_source, muted); }
        public void Dispose()
        {
            if (_meter != IntPtr.Zero)
            {
                Obs.obs_volmeter_remove_callback(_meter, _callback, IntPtr.Zero);
                Obs.obs_volmeter_detach_source(_meter); Obs.obs_volmeter_destroy(_meter); _meter = IntPtr.Zero;
            }
            if (_source == IntPtr.Zero) return;
            Obs.obs_source_set_audio_mixers(_source,0);
            Obs.obs_set_output_source(_channel, IntPtr.Zero);
            _gate?.Dispose(); _gate=null;
            Obs.obs_source_remove(_source); Obs.obs_source_release(_source); _source = IntPtr.Zero;
            _released();
        }
    }
}
