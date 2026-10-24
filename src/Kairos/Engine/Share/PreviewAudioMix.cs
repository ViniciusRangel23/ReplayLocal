using System.Runtime.InteropServices;
using Kairos.Engine.Audio;
using Kairos.Engine.Edit;

namespace Kairos.Engine.Share;

public sealed record PreviewAudioPlan(AudioLayout Layout,IReadOnlyDictionary<string,double> Gains);

/// <summary>Small synchronous blocks on the preview worker; no output device and no capture sources.</summary>
public sealed class PreviewAudioMix : IDisposable
{
    readonly List<Source> _sources=[];
    readonly long _totalFrames;
    long _position;
    bool _disposed;
    readonly float[] _scratch=new float[1920];
    public int BufferedFrames=>_sources.Count==0?0:_sources.Max(s=>s.BufferedFrames);

    public PreviewAudioMix(string path,double from,double to,PreviewAudioPlan plan,double rate=1)
    {
        if(!double.IsFinite(rate)||rate<0.25||rate>4||from<0||to<from)throw new ArgumentException("Invalid preview range");
        _totalFrames=(long)Math.Round((to-from)*48000/rate);
        var probe=AudioContainer.Probe(path);
        try
        {
            foreach(var track in plan.Layout.Tracks)
            {
                if(!plan.Gains.TryGetValue(track.Id,out double gain)||gain<=0)continue;
                if(!double.IsFinite(gain))throw new ArgumentException("Invalid preview gain");
                int ordinal=probe.Streams.ToList().FindIndex(t=>t.StreamIndex==track.StreamIndex);
                if(ordinal<0)throw new ArgumentException("Preview source does not exist");
                _sources.Add(new(path,ordinal,from,to,rate,Math.Clamp(gain,0,4)));
            }
        }
        catch{Dispose();throw;}
    }
    public int Read(Span<float> interleavedStereo,CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested(); ObjectDisposedException.ThrowIf(_disposed,this);
        int frames=(int)Math.Min(Math.Min(interleavedStereo.Length/2,960),_totalFrames-_position);
        if(frames<=0)return 0;
        var output=interleavedStereo[..(frames*2)];output.Clear();
        foreach(var source in _sources)
        {
            ct.ThrowIfCancellationRequested();
            source.Read(_scratch.AsSpan(0,frames*2),ct);
            for(int i=0;i<output.Length;i++)output[i]+=_scratch[i];
        }
        for(int i=0;i<output.Length;i++)output[i]=AudioEdit.Saturate(output[i]);
        _position+=frames;return output.Length;
    }
    public void Dispose(){if(_disposed)return;_disposed=true;foreach(var source in _sources)source.Dispose();_sources.Clear();}

    sealed class Source : IDisposable
    {
        readonly ClipPlayback _reader;
        readonly double _from,_gain;
        readonly long _originalFrames;
        readonly SoundTempo? _tempo;
        long _originalPosition;
        byte[] _pending=[]; long _pendingStart;
        byte[] _processed=[];int _processedAt;
        bool _eof,_flushed;
        readonly byte[] _chunk=new byte[1920*4];
        public int BufferedFrames=>(_pending.Length+_processed.Length+_chunk.Length)/8;
        public Source(string path,int ordinal,double from,double to,double rate,double gain)
        {
            _from=from;_gain=gain;_originalFrames=(long)Math.Round((to-from)*48000);
            _reader=new(path,2,2,ordinal,audioOnly:true,floatAudio:true);
            try{_reader.Seek(from);if(rate!=1)_tempo=new(rate,floatAudio:true);}
            catch{_reader.Dispose();throw;}
        }
        int Original(Span<byte> output,CancellationToken ct)
        {
            int frames=(int)Math.Min(output.Length/8,_originalFrames-_originalPosition);
            if(frames<=0)return 0;
            output[..(frames*8)].Clear();
            int filled=0;
            while(filled<frames)
            {
                ct.ThrowIfCancellationRequested();
                long at=_originalPosition+filled;
                if(!_eof&&(_pending.Length==0||at>=_pendingStart+_pending.Length/8))
                {
                    if(!_reader.NextSound()){_eof=true;_pending=[];}
                    else{_pending=_reader.Sound.ToArray();_pendingStart=(long)Math.Round((_reader.SoundTime-_from)*48000);continue;}
                }
                if(_pending.Length==0){filled=frames;break;}
                if(at<_pendingStart){filled+=(int)Math.Min(frames-filled,_pendingStart-at);continue;}
                int offset=(int)(at-_pendingStart),copy=Math.Min(frames-filled,_pending.Length/8-offset);
                if(copy<=0){_pending=[];continue;}
                _pending.AsSpan(offset*8,copy*8).CopyTo(output[(filled*8)..]);filled+=copy;
            }
            _originalPosition+=frames;return frames*8;
        }
        public void Read(Span<float> output,CancellationToken ct)
        {
            output.Clear();int written=0;
            while(written<output.Length)
            {
                ct.ThrowIfCancellationRequested();
                if(_processedAt>=_processed.Length)
                {
                    int bytes=Original(_chunk,ct);_processedAt=0;
                    if(bytes>0)_processed=_tempo?.Push(_chunk.AsSpan(0,bytes))??_chunk.AsSpan(0,bytes).ToArray();
                    else if(!_flushed){_flushed=true;_processed=_tempo?.Flush()??[];}
                    else break;
                    if(_processed.Length==0)continue;
                }
                var samples=MemoryMarshal.Cast<byte,float>(_processed.AsSpan(_processedAt));
                int count=Math.Min(samples.Length,output.Length-written);
                for(int i=0;i<count;i++)output[written+i]=(float)(samples[i]*_gain);
                written+=count;_processedAt+=count*4;
            }
        }
        public void Dispose(){_tempo?.Dispose();_reader.Dispose();}
    }
}
