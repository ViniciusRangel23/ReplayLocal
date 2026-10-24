using System.Runtime.InteropServices;
using System.Text;
using Kairos.App;
namespace Kairos.Engine.Audio;

public static partial class ApplicationAudioWindows
{
    [StructLayout(LayoutKind.Sequential)]struct Point {public int X,Y;}
    [StructLayout(LayoutKind.Sequential)]struct Rect {public int Left,Top,Right,Bottom;}
    [StructLayout(LayoutKind.Sequential)]struct MonitorInfo {public int Size;public Rect Monitor,Work;public uint Flags;}
    [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")]static extern IntPtr GetWindowStyle(IntPtr window,int index);
    [DllImport("user32.dll")]static extern bool IsIconic(IntPtr window);
    [DllImport("user32.dll")]static extern bool GetClientRect(IntPtr window,out Rect rect);
    [DllImport("user32.dll")]static extern bool ClientToScreen(IntPtr window,ref Point point);
    [DllImport("user32.dll")]static extern IntPtr MonitorFromWindow(IntPtr window,uint flags);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern bool GetMonitorInfo(IntPtr monitor,ref MonitorInfo info);

    /// <summary>Read geometry/identity only. Does not activate, focus, resize, hook or capture a window.</summary>
    public static AudioTargetCandidate? ForegroundFullscreen(IReadOnlyList<AudioTargetCandidate> candidates)
    {
        IntPtr window=GetForegroundWindow();
        if(window==IntPtr.Zero || IsIconic(window) || !GetClientRect(window,out var rect))return null;
        GetWindowThreadProcessId(window,out uint pid);
        if(pid==0 || pid==Environment.ProcessId)return null;
        var origin=new Point{X=rect.Left,Y=rect.Top};var end=new Point{X=rect.Right,Y=rect.Bottom};
        if(!ClientToScreen(window,ref origin) || !ClientToScreen(window,ref end))return null;
        var info=new MonitorInfo{Size=Marshal.SizeOf<MonitorInfo>()};
        if(!GetMonitorInfo(MonitorFromWindow(window,2),ref info))return null;
        long style=GetWindowStyle(window,-16 /* GWL_STYLE */).ToInt64();
        if(style==0)return null;
        bool hasCaption=(style&0x00C00000)==0x00C00000;
        var klass=new StringBuilder(512);GetClassName(window,klass,klass.Capacity);
        if(!AutomaticGameAudio.IsFullscreen(new(origin.X,origin.Y,end.X,end.Y),new(info.Monitor.Left,info.Monitor.Top,info.Monitor.Right,info.Monitor.Bottom),klass.ToString(),hasCaption))return null;
        var title=new StringBuilder(1024);GetWindowText(window,title,title.Capacity);
        return candidates.FirstOrDefault(c=>c.ProcessId==(int)pid && c.WindowToken==Token(title.ToString(),klass.ToString(),c.Target.ExecutableName));
    }
}
