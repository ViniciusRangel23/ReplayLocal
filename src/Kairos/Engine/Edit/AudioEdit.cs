using Kairos.Engine.Audio;

namespace Kairos.Engine.Edit;

public sealed record AudioLevel(string SourceId, double Gain = 1, bool Muted = false, bool Included = true);
public enum AudioExportMode { Mixed, Separate }
public sealed record AudioEditState(AudioLayout Layout, IReadOnlyList<AudioLevel> Levels);

public static class AudioEdit
{
    public static AudioEditState Create(AudioLayout layout)
    {
        bool known = layout.Tracks.Any(t=>t.Role==AudioRole.Mix);
        var sources=layout.Tracks.Where(t=>t.Role!=AudioRole.Mix).ToArray();
        if(sources.Length==0) sources=layout.Tracks.ToArray();
        return new(layout,sources.Select((t,i)=>new AudioLevel(t.Id,Included:known||i==0)).ToArray());
    }
    public static IReadOnlyDictionary<string,double> ResolveGains(EditProject project, ClipSpan span, IReadOnlySet<string>? soloIds=null)
    {
        var result=new Dictionary<string,double>(StringComparer.Ordinal);
        if(project.Audio==null) return result;
        foreach(var level in project.Audio.Levels)
        {
            double part=span.AudioGains?.GetValueOrDefault(level.SourceId,1)??1;
            double value=level.Muted||!level.Included||(soloIds?.Count>0&&!soloIds.Contains(level.SourceId))?0:level.Gain*part;
            result[level.SourceId]=double.IsFinite(value)?Math.Clamp(value,0,4):0;
        }
        return result;
    }
    public static float Saturate(double sample)=> (float)Math.Clamp(sample,-1,1);
}
