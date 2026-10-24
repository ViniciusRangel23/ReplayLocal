using System.Runtime.InteropServices;
using Kairos.Native;

namespace Kairos.Engine.Audio;

/// <summary>Filter runs before libobs buffers/mixes a block. Never depends on a UI timer.
/// A retained OS process handle prevents a recycled PID from inheriting authorization.</summary>
sealed class ObsProcessAudioGate : IDisposable
{
    const string FilterId="replay_local_process_identity_gate";
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate IntPtr NameCallback(IntPtr data);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate IntPtr CreateCallback(IntPtr settings,IntPtr source);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate void DestroyCallback(IntPtr data);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate IntPtr AudioCallback(IntPtr data,IntPtr audio);
    static readonly IntPtr IdText=Marshal.StringToCoTaskMemUTF8(FilterId),NameText=Marshal.StringToCoTaskMemUTF8("Selected process identity");
    static readonly NameCallback Name=_=>NameText;
    static readonly CreateCallback Create=(settings,_)=>new IntPtr(Obs.obs_data_get_int(settings,"gate"));
    static readonly DestroyCallback Destroy=data=>{if(data!=IntPtr.Zero)GCHandle.FromIntPtr(data).Free();};
    static readonly AudioCallback Audio=(data,audio)=>
    {
        try{return ((ObsProcessAudioGate)GCHandle.FromIntPtr(data).Target!).Allow()?audio:IntPtr.Zero;}
        catch{return IntPtr.Zero;} // Native callback boundary always fails closed.
    };
    readonly AudioTargetCandidate _expected;
    readonly WindowsProcessIdentity _process;
    readonly IntPtr _source;
    IntPtr _filter;
    bool _closed;
    long _accepted,_rejected;
    public long Accepted=>Interlocked.Read(ref _accepted);
    public long Rejected=>Interlocked.Read(ref _rejected);

    public ObsProcessAudioGate(IntPtr source,AudioTargetCandidate candidate)
    {
        _source=source;_expected=candidate;
        if(candidate.ProcessId<=0 || candidate.ProcessId==Environment.ProcessId ||
            string.Equals(candidate.Target.ExecutableName,Path.GetFileName(Environment.ProcessPath),StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Application identity unavailable");
        _process=WindowsProcessIdentity.Open(candidate.ProcessId);
        try
        {
            // Retain this limited-query handle; never reopen the PID on reconnect.
            if(!_process.IsAlive || !string.Equals(_process.ExecutablePath,candidate.Target.ExecutablePath,StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Application identity changed");
            if(Obs.obs_source_get_display_name(FilterId)==IntPtr.Zero)
            {
                var info=new ObsAudioFilterInfo{Id=IdText,Type=1,Flags=2,
                    GetName=Marshal.GetFunctionPointerForDelegate(Name),Create=Marshal.GetFunctionPointerForDelegate(Create),
                    Destroy=Marshal.GetFunctionPointerForDelegate(Destroy),FilterAudio=Marshal.GetFunctionPointerForDelegate(Audio)};
                Obs.obs_register_source_s(ref info,(UIntPtr)Marshal.SizeOf<ObsAudioFilterInfo>());
            }
            var handle=GCHandle.Alloc(this);
            using var settings=new ObsData().Set("gate",GCHandle.ToIntPtr(handle).ToInt64());
            _filter=Obs.obs_source_create(FilterId,"Process identity gate",settings.Handle,IntPtr.Zero);
            if(_filter==IntPtr.Zero){handle.Free();throw new InvalidOperationException("Application audio guard unavailable");}
            Obs.obs_source_filter_add(source,_filter);
        }
        catch{_process.Dispose();throw;}
    }
    bool Allow()
    {
        bool allowed=false;
        if(!Volatile.Read(ref _closed) && _process.IsAlive)
        {
            CallData cd=default;
            try
            {
                if(Obs.proc_handler_call(Obs.obs_source_get_proc_handler(_source),"get_hooked",ref cd))
                {
                    string Read(string key)=>Obs.calldata_get_string(ref cd,key,out var p)?Obs.Utf8(p)??"":"";
                    string title=Read("title"),klass=Read("class"),exe=Read("executable");
                    if(title.Length>0 && string.Equals(exe,_expected.Target.ExecutableName,StringComparison.OrdinalIgnoreCase))
                    {
                        string token=ApplicationAudioWindows.Token(title,klass,exe);
                        allowed=ApplicationAudioGate.Allows(_expected,token,ApplicationAudioWindows.HookOwners(title,klass,exe,_expected),_process.IsAlive);
                    }
                }
            }
            finally{Obs.FreeCallData(ref cd);}
        }
        if(allowed)Interlocked.Increment(ref _accepted);else Interlocked.Increment(ref _rejected);
        return allowed;
    }
    public void Dispose()
    {
        if(Volatile.Read(ref _closed))return;
        Volatile.Write(ref _closed,true);
        if(_filter!=IntPtr.Zero)
        {
            // Removal takes libobs's filter mutex, waiting for any in-flight audio callback.
            Obs.obs_source_filter_remove(_source,_filter);Obs.obs_source_release(_filter);_filter=IntPtr.Zero;
        }
        _process.Dispose();
    }
}
