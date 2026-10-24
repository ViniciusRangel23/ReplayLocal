using Kairos.App;
namespace Kairos.Engine.Audio;

/// <summary>One stable game track. Alt+Tab preserves its validated process; a new fullscreen target replaces it.</summary>
public sealed class AutomaticGameAudio
{
    AudioTargetCandidate? _selected;
    public AudioTargetCandidate? Update(IReadOnlyList<AudioTargetCandidate> candidates,AudioTargetCandidate? foreground,IReadOnlyCollection<string> explicitPaths)
    {
        bool Excluded(AudioTargetCandidate c)=>explicitPaths.Contains(c.Target.ExecutablePath,StringComparer.OrdinalIgnoreCase);
        AudioTargetCandidate? Current(AudioTargetCandidate selected)=>candidates.FirstOrDefault(c=>c.ProcessId==selected.ProcessId &&
            string.Equals(c.Target.ExecutablePath,selected.Target.ExecutablePath,StringComparison.OrdinalIgnoreCase) && c.WindowToken==selected.WindowToken)
            ??candidates.FirstOrDefault(c=>c.ProcessId==selected.ProcessId && string.Equals(c.Target.ExecutablePath,selected.Target.ExecutablePath,StringComparison.OrdinalIgnoreCase));
        if(_selected!=null)_selected=Current(_selected);
        if(_selected!=null && Excluded(_selected))_selected=null;
        if(foreground is {ProcessId:>0} && !Excluded(foreground))
        {
            var valid=candidates.FirstOrDefault(c=>c.ProcessId==foreground.ProcessId && c.WindowToken==foreground.WindowToken &&
                string.Equals(c.Target.ExecutablePath,foreground.Target.ExecutablePath,StringComparison.OrdinalIgnoreCase));
            if(valid!=null)_selected=valid;
        }
        return _selected;
    }
    public static bool IsFullscreen(ScreenRect client,ScreenRect monitor,string className,bool hasCaption=false)=>
        !hasCaption &&
        className is not ("Progman" or "WorkerW" or "Shell_TrayWnd" or "Shell_SecondaryTrayWnd") &&
        monitor.Right>monitor.Left && monitor.Bottom>monitor.Top &&
        Math.Abs((long)client.Left-monitor.Left)<=2 && Math.Abs((long)client.Top-monitor.Top)<=2 &&
        Math.Abs((long)client.Right-monitor.Right)<=2 && Math.Abs((long)client.Bottom-monitor.Bottom)<=2;
}
