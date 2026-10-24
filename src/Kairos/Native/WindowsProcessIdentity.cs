using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Kairos.Native;

/// <summary>Read executable identity without module enumeration or process memory access.</summary>
internal sealed class WindowsProcessIdentity : IDisposable
{
    const uint QueryLimitedInformation=0x1000, Synchronize=0x100000, WaitTimeout=258;
    readonly SafeProcessHandle _handle;
    public string ExecutablePath { get; }
    WindowsProcessIdentity(SafeProcessHandle handle,string path){_handle=handle;ExecutablePath=path;}
    public bool IsAlive=>!_handle.IsClosed && !_handle.IsInvalid && WaitForSingleObject(_handle,0)==WaitTimeout;

    public static WindowsProcessIdentity Open(int pid)
    {
        if(pid<=0)throw new InvalidOperationException("Application identity unavailable");
        var handle=OpenProcess(QueryLimitedInformation|Synchronize,false,(uint)pid);
        try
        {
            string? path=Read(handle);
            if(path==null || WaitForSingleObject(handle,0)!=WaitTimeout)throw new InvalidOperationException("Application identity unavailable");
            return new(handle,path);
        }
        catch{handle.Dispose();throw;}
    }
    public static string? ReadImagePath(int pid)
    {
        if(pid<=0)return null;
        using var handle=OpenProcess(QueryLimitedInformation,false,(uint)pid);
        return Read(handle);
    }
    static string? Read(SafeProcessHandle handle)
    {
        if(handle.IsInvalid || handle.IsClosed)return null;
        var text=new StringBuilder(1024);uint size=(uint)text.Capacity;
        if(QueryFullProcessImageName(handle,0,text,ref size))return text.ToString();
        if(Marshal.GetLastWin32Error()!=122)return null;
        text=new StringBuilder(32768);size=(uint)text.Capacity;
        return QueryFullProcessImageName(handle,0,text,ref size)?text.ToString():null;
    }
    public void Dispose()=>_handle.Dispose();
    [DllImport("kernel32.dll",SetLastError=true)]static extern SafeProcessHandle OpenProcess(uint access,[MarshalAs(UnmanagedType.Bool)]bool inherit,uint pid);
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]
    [return:MarshalAs(UnmanagedType.Bool)]static extern bool QueryFullProcessImageName(SafeProcessHandle process,uint flags,StringBuilder path,ref uint size);
    [DllImport("kernel32.dll",SetLastError=true)]static extern uint WaitForSingleObject(SafeProcessHandle process,uint milliseconds);
}
