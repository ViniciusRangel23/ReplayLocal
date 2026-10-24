using System.Globalization;
using System.Text;
using Kairos.Engine.Audio;

namespace Kairos.Engine.Edit;

static class MultitrackAudioGraph
{
    static string N(double x)=>x.ToString("0.########",CultureInfo.InvariantCulture);
    public static (string Text,int[] Inputs,string[] Outputs,AudioLayout Layout) Build(EditProject p,AudioExportMode mode)
    {
        var state=p.Audio!;
        var tracks=state.Levels.Select(l=>state.Layout.Tracks.Single(t=>t.Id==l.SourceId)).ToArray();
        if(tracks.Length==0) return ("",[],[],new(1,[]));
        var g=new StringBuilder();
        var pieces=p.Pieces();
        for(int t=0;t<tracks.Length;t++)
        {
            var input=tracks[t];
            // Resample with a zero origin inserts missing initial samples. Padding ensures short sources survive every cut.
            g.Append($"[a{input.StreamIndex}]aresample=48000:async=1:first_pts=0,aformat=sample_fmts=fltp:channel_layouts=stereo,apad,atrim=duration={N(p.Duration)}");
            if(pieces.Count>1) g.Append($",asplit={pieces.Count}");
            for(int k=0;k<pieces.Count;k++) g.Append($"[src{t}_{k}]");
            g.Append(';');
            for(int k=0;k<pieces.Count;k++)
            {
                var (span,speed)=pieces[k];
                double gain=AudioEdit.ResolveGains(p,span).GetValueOrDefault(input.Id);
                g.Append($"[src{t}_{k}]atrim=start={N(span.Start)}:end={N(span.End)},asetpts=PTS-STARTPTS,volume={N(gain)}");
                if(speed!=1) g.Append(',').Append(EditGraph.TempoChain(speed));
                g.Append($"[piece{t}_{k}];");
            }
            for(int k=0;k<pieces.Count;k++) g.Append($"[piece{t}_{k}]");
            g.Append(pieces.Count>1?$"concat=n={pieces.Count}:v=0:a=1":"anull");
            g.Append(mode==AudioExportMode.Separate?$",asplit=2[mix{t}][own{t}];":$"[mix{t}];");
        }
        for(int t=0;t<tracks.Length;t++)g.Append($"[mix{t}]");
        g.Append(tracks.Length>1?$"amix=inputs={tracks.Length}:normalize=0:duration=longest":"anull");
        g.Append(",aeval=clip(val(0)\\,-1\\,1)|clip(val(1)\\,-1\\,1)[aout]");
        var outputTracks=new List<AudioTrackInfo>{new("mix",1,"Mistura",AudioRole.Mix)};
        var outputs=new List<string>{"aout"};
        if(mode==AudioExportMode.Separate)
            for(int t=0;t<tracks.Length;t++)
            {
                g.Append($";[own{t}]aeval=clip(val(0)\\,-1\\,1)|clip(val(1)\\,-1\\,1)[out{t}]");
                outputs.Add("out"+t);
                outputTracks.Add(tracks[t] with {Id=tracks[t].Id=="mix"?"source-mix":tracks[t].Id,StreamIndex=t+2,Role=tracks[t].Role==AudioRole.Mix?AudioRole.Generic:tracks[t].Role});
            }
        return (g.ToString(),tracks.Select(t=>t.StreamIndex).ToArray(),outputs.ToArray(),new(1,outputTracks));
    }
}
