using Kairos.Engine.Audio;
using Kairos.Engine;
using Kairos.Engine.Edit;
using Kairos.Engine.Share;
using Kairos.Settings;

namespace Kairos.Tests;

public static class MultitrackExportTests
{
    [Test]
    public static void Late_and_short_sources_stay_aligned_through_cuts_and_speed()
    {
        TestFfmpeg.Load();
        string source=AudioFixtures.Create(lateSeconds:0.25,shortSeconds:0.6);
        var info=ClipTranscoder.Probe(source);
        var p=EditProject.For(source,2) with { Audio=AudioEdit.Create(AudioContainer.Probe(source).Layout!) };
        var target=Path.Combine(Path.GetDirectoryName(source)!,"aligned.mp4");
        var plan=EncoderPresets.Plan(VideoCodec.H264,QualityPreset.Light,info.Width,info.Height,(int)Math.Round(info.Fps),22,40,false);
        EditExporter.Export(source,target,EditGraph.Build(p,info.Width,info.Height,info.Fps,4,new Dictionary<Guid,Overlay>(),AudioExportMode.Separate),plan,null,default);
        var discord=TrackAudio.Read(target,2,null,default).Samples;
        Assert.True(discord.Take(2400).Max(Math.Abs)<0.003,"late discord has initial silence");
        var spotify=TrackAudio.Read(target,3,null,default).Samples;
        Assert.True(spotify.Skip(16000).Max(Math.Abs)<0.003,"short spotify has trailing silence");
        foreach(int t in Enumerable.Range(0,4)) Assert.Near(2,TrackAudio.Read(target,t,null,default).Samples.Length/16000d,0.08,"same duration");
        p=p.SplitAt(1).SetSpeed(0,1,2);
        target=Path.Combine(Path.GetDirectoryName(source)!,"speed.mp4");
        EditExporter.Export(source,target,EditGraph.Build(p,info.Width,info.Height,info.Fps,4,new Dictionary<Guid,Overlay>(),AudioExportMode.Separate),plan,null,default);
        foreach(int t in Enumerable.Range(0,4)) Assert.Near(1.5,TrackAudio.Read(target,t,null,default).Samples.Length/16000d,0.09,"speed matches all tracks");
    }
    [Test]
    public static void Both_export_modes_preserve_identity_and_isolation()
    {
        TestFfmpeg.Load();
        string source=AudioFixtures.Create();
        var info=ClipTranscoder.Probe(source);
        var layout=AudioContainer.Probe(source).Layout!;
        var project=EditProject.For(source,info.Duration) with { Audio=AudioEdit.Create(layout) };
        project=project with { Audio=project.Audio! with {Levels=project.Audio.Levels.Select(l=>l.SourceId=="spotify"?l with {Muted=true}:l).ToArray()} };
        foreach(var mode in new[]{AudioExportMode.Mixed,AudioExportMode.Separate})
        {
            var graph=EditGraph.Build(project,info.Width,info.Height,info.Fps,info.AudioStreams,new Dictionary<Guid,Overlay>(),mode);
            var plan=EncoderPresets.Plan(VideoCodec.H264,QualityPreset.Light,info.Width,info.Height,(int)Math.Round(info.Fps),22,40,false);
            string target=Path.Combine(Path.GetDirectoryName(source)!,mode+".mp4");
            EditExporter.Export(source,target,graph,plan,null,default);
            var read=AudioContainer.Probe(target);
            Assert.Equal(mode==AudioExportMode.Mixed?1:4,read.Streams.Count);
            Assert.True(read.Layout!=null,"output has layout");
            var samples=TrackAudio.Read(target,0,null,default).Samples;
            Assert.True(AudioFixtures.Magnitude(samples,440)>0.05 && AudioFixtures.Magnitude(samples,660)>0.05,"game and discord in mix");
            Assert.True(AudioFixtures.Magnitude(samples,880)<0.003,"spotify removed from mix");
            if(mode==AudioExportMode.Separate)
            {
                Assert.Equal("spotify",read.Layout!.Tracks[3].Id);
                var spotify=TrackAudio.Read(target,3,null,default).Samples;
                Assert.True(spotify.Max(Math.Abs)<0.003,"muted individual stream retained silent");
                var game=TrackAudio.Read(target,1,null,default).Samples;
                Assert.True(AudioFixtures.Magnitude(game,440)>0.05 && AudioFixtures.Magnitude(game,660)<0.003,"isolated game");
            }
        }
    }
}
