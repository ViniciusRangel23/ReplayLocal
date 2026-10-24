using Kairos.Native;
using System.Runtime.InteropServices;
using System.Text;
using Kairos.Settings;

namespace Kairos.Engine.Audio;

/// <summary>Window identity only; does not create an audio or video capture source.</summary>
public static partial class ApplicationAudioWindows
{
    delegate bool EnumWindowProc(IntPtr hwnd, IntPtr data);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowProc callback, IntPtr data);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int length);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr hwnd, StringBuilder text, int length);

    static readonly HashSet<string> InfrastructureClasses=new(StringComparer.OrdinalIgnoreCase)
    {"IME","MSCTFIME UI","OleDdeWndClass","GDI+ Hook Window Class"};
    internal static bool IsCaptureWindow(string className,long style,long extendedStyle)=>
        !string.IsNullOrWhiteSpace(className) && !InfrastructureClasses.Contains(className) &&
        (style&0x40000000)==0 /* WS_CHILD */ && (extendedStyle&0x80)==0 /* WS_EX_TOOLWINDOW */;

    public static IReadOnlyList<AudioTargetCandidate> List(bool includeHidden = false)
    {
        var result = new List<AudioTargetCandidate>();
        var paths = new Dictionary<uint, string?>();
        static string Escape(string s) => s.Replace("#", "#22").Replace(":", "#3A");
        EnumWindows((hwnd, _) =>
        {
            bool visible=IsWindowVisible(hwnd);
            if (!includeHidden && !visible) return true;
            var klass = new StringBuilder(512); GetClassName(hwnd, klass, klass.Capacity);
            if(!IsCaptureWindow(klass.ToString(),GetWindowStyle(hwnd,-16).ToInt64(),GetWindowStyle(hwnd,-20).ToInt64()))return true;
            var title = new StringBuilder(1024); GetWindowText(hwnd, title, title.Capacity);
            if (title.Length == 0) return true;
            GetWindowThreadProcessId(hwnd, out uint pid);
            if (!paths.TryGetValue(pid, out var path))
            {
                path=WindowsProcessIdentity.ReadImagePath((int)pid);
                paths[pid] = path;
            }
            if (string.IsNullOrWhiteSpace(path)) return true;
            string exe = Path.GetFileName(path);
            result.Add(new(new(path, exe), $"{Escape(title.ToString())}:{Escape(klass.ToString())}:{Escape(exe)}",(int)pid,visible));
            return true;
        }, IntPtr.Zero);
        // OBS matches an executable name, not its full path. Suppress indistinguishable identities before creating any source.
        var unsafeNames = result.GroupBy(c => c.Target.ExecutableName, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Select(x => x.Target.ExecutablePath).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
            .Select(g => g.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return result.Where(c => !unsafeNames.Contains(c.Target.ExecutableName)).ToArray();
    }
    internal static string Token(string title,string klass,string exe)
    {
        static string Escape(string s)=>s.Replace("#","#22").Replace(":","#3A");
        return $"{Escape(title)}:{Escape(klass)}:{Escape(exe)}";
    }
    // OBS reports the actual HWND's title/class/exe through get_hooked. Reject ANY competing
    // HWND with that title/class (even another executable) rather than guess its owner.
    internal static IReadOnlyList<AudioTargetCandidate> HookOwners(string title,string klass,string exe,AudioTargetCandidate expected)
    {
        var found=new List<AudioTargetCandidate>();
        var t=new StringBuilder(1024);var c=new StringBuilder(512);
        EnumWindows((hwnd,_)=>
        {
            GetWindowThreadProcessId(hwnd,out uint pid);
            // GetWindowText on our own HWND could synchronously wait for the UI thread during teardown.
            if(pid==Environment.ProcessId)return true;
            t.Clear();GetWindowText(hwnd,t,t.Capacity);
            if(t.ToString()!=title)return true;
            c.Clear();GetClassName(hwnd,c,c.Capacity);
            if(c.ToString()!=klass)return true;
            found.Add(new(expected.Target,Token(title,klass,exe),(int)pid));return true;
        },IntPtr.Zero);
        return found;
    }
}
