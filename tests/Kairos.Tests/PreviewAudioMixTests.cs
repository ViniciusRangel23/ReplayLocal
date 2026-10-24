using Kairos.Engine.Share;
using Kairos.Engine.Audio;

namespace Kairos.Tests;

public static class PreviewAudioMixTests
{
    [Test]
    public static void Mix_respects_gains_late_start_and_short_source()
    {
        TestFfmpeg.Load();string source=AudioFixtures.Create(lateSeconds:0.25,shortSeconds:0.6);
        var layout=AudioContainer.Probe(source).Layout!;
        var gains=new Dictionary<string,double>{{"game",0.5},{"discord",1},{"spotify",0}};
        using var mix=new PreviewAudioMix(source,0,2,new(layout,gains));
        var samples=new List<float>();float[] buffer=new float[1920];int count;
        while((count=mix.Read(buffer,default))>0)samples.AddRange(buffer.Take(count).Where((_,i)=>i%2==0));
        Assert.Near(2,samples.Count/48000d,0.01);
        Assert.True(AudioFixtures.Magnitude(samples.ToArray(),440,48000)>0.05,"game half gain");
        Assert.True(AudioFixtures.Magnitude(samples.ToArray(),880,48000)<0.003,"spotify muted");
        Assert.True(AudioFixtures.Magnitude(samples.Take(4800).ToArray(),660,48000)<0.003,"discord not moved to time zero");
    }
    [Test]
    public static void Seek_speed_and_cancel_are_bounded()
    {
        TestFfmpeg.Load();string source=AudioFixtures.Create();var layout=AudioContainer.Probe(source).Layout!;
        using var mix=new PreviewAudioMix(source,1,2,new(layout,new Dictionary<string,double>{{"game",1}}),2);
        float[] data=new float[1920];int total=0,n;
        while((n=mix.Read(data,default))>0)total+=n;
        Assert.Near(0.5,total/96000d,0.06);
        bool cancelled=false;try{mix.Read(data,new CancellationToken(true));}catch(OperationCanceledException){cancelled=true;}
        Assert.True(cancelled,"cancellation checked even at EOF");
        Assert.True(mix.BufferedFrames<=24000,"less than 500ms per source");
    }
    [Test]
    public static void Float_sources_keep_headroom_before_gain_and_mix()
    {
        TestFfmpeg.Load();string path=AudioFixtures.Create(sourceCount:1,amplitude:1.2);
        using var mix=new PreviewAudioMix(path,0,2,new(AudioContainer.Probe(path).Layout!,new Dictionary<string,double>{{"game",0.5}}));
        float[] buffer=new float[1920];var samples=new List<float>();int n;
        while((n=mix.Read(buffer,default))>0)samples.AddRange(buffer.Take(n).Where((_,i)=>i%2==0));
        Assert.Near(0.6,AudioFixtures.Magnitude(samples.ToArray(),440,48000),0.015);
    }
    [Test]
    public static void Middle_timestamp_gap_remains_silent()
    {
        TestFfmpeg.Load();string path=AudioFixtures.Create(seconds:2.5,sourceCount:1,middleGapSeconds:0.25);
        using var mix=new PreviewAudioMix(path,0,2.5,new(AudioContainer.Probe(path).Layout!,new Dictionary<string,double>{{"game",1}}));
        float[] buffer=new float[1920];var samples=new List<float>();int n;
        while((n=mix.Read(buffer,default))>0)samples.AddRange(buffer.Take(n).Where((_,i)=>i%2==0));
        Assert.True(samples.Skip(53000).Take(5000).Max(Math.Abs)<0.002,"middle gap must not move later audio earlier");
        Assert.True(AudioFixtures.Magnitude(samples.Skip(63000).ToArray(),440,48000)>0.1,"sound resumes after gap");
    }
}
