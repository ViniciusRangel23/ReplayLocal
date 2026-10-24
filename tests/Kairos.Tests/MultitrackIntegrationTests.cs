using System.Diagnostics;
using System.Text.Json;
using Kairos.Engine;
using Kairos.Engine.Audio;
using Kairos.Engine.Edit;
using Kairos.Engine.Share;
using Kairos.Settings;
using Kairos.Ui.Edit;

namespace Kairos.Tests;

public static class MultitrackIntegrationTests
{
    [Test]
    public static void Four_synthetic_sources_reopen_in_editor_and_export_both_modes()
    {
        TestFfmpeg.Load();string source=AudioFixtures.Create(sourceCount:4);
        var probe=AudioContainer.Probe(source);Assert.Equal(5,probe.Streams.Count);
        var info=ClipTranscoder.Probe(source);
        var project=EditProject.For(source,2) with {Audio=AudioEdit.Create(probe.Layout!)};
        var vm=new EditorViewModel(project,false,EditorMode.Library,info.Width/(double)info.Height,5);
        Assert.Equal(4,vm.AudioTracks.Count);
        vm.AudioTracks.Single(t=>t.SourceId=="spotify").Muted=true;
        vm.AudioTracks.Single(t=>t.SourceId=="microphone").GainPercent=50;
        foreach(var mode in new[]{AudioExportMode.Mixed,AudioExportMode.Separate})
        {
            string target=Path.Combine(Path.GetDirectoryName(source)!,"integration-"+mode+".mp4");
            var graph=EditGraph.Build(vm.Project,info.Width,info.Height,info.Fps,5,new Dictionary<Guid,Overlay>(),mode);
            var plan=EncoderPresets.Plan(VideoCodec.H264,QualityPreset.Light,info.Width,info.Height,(int)Math.Round(info.Fps),22,40,false);
            EditExporter.Export(source,target,graph,plan,null,default);
            var result=AudioContainer.Probe(target);
            Assert.Equal(mode==AudioExportMode.Mixed?1:5,result.Streams.Count);
            var reopened=new EditorViewModel(EditProject.For(target,2) with {Audio=AudioEdit.Create(result.Layout!)},false,EditorMode.Library,1.7,result.Streams.Count);
            Assert.Equal(mode==AudioExportMode.Mixed?1:4,reopened.AudioTracks.Count);
            var samples=TrackAudio.Read(target,0,null,default).Samples;
            Assert.True(AudioFixtures.Magnitude(samples,880)<0.003,"Spotify removed");
            // TrackAudio's FFmpeg mono downmix sums identical stereo channels at sqrt(2).
            Assert.Near(0.075*Math.Sqrt(2),AudioFixtures.Magnitude(samples,1100),0.015,"microphone half volume");
        }
        string? evidence=Environment.GetEnvironmentVariable("REPLAYLOCAL_TEST_EVIDENCE");
        if(evidence!=null){Directory.CreateDirectory(evidence);File.Copy(source,Path.Combine(evidence,"four-sources.mp4"),true);}
    }
    [Test]
    public static void Measure_one_and_four_source_export_and_preview_cost()
    {
        TestFfmpeg.Load();var measurements=new List<object>();
        foreach(int sources in new[]{1,4})
        {
            string source=AudioFixtures.Create(sourceCount:sources);var info=ClipTranscoder.Probe(source);
            var project=EditProject.For(source,2) with {Audio=AudioEdit.Create(AudioContainer.Probe(source).Layout!)};
            for(int run=0;run<3;run++)
            {
                using var self=Process.GetCurrentProcess();var cpu=self.TotalProcessorTime;var watch=Stopwatch.StartNew();
                string target=Path.Combine(Path.GetDirectoryName(source)!,"measure-"+run+".mp4");
                var plan=EncoderPresets.Plan(VideoCodec.H264,QualityPreset.Light,info.Width,info.Height,(int)Math.Round(info.Fps),22,40,false);
                EditExporter.Export(source,target,EditGraph.Build(project,info.Width,info.Height,info.Fps,sources+1,new Dictionary<Guid,Overlay>(),AudioExportMode.Separate),plan,null,default);
                watch.Stop();self.Refresh();
                double exportMs=watch.Elapsed.TotalMilliseconds;
                watch.Restart();using var mix=new PreviewAudioMix(source,0,2,new(project.Audio!.Layout,AudioEdit.ResolveGains(project,project.Kept[0])));
                float[] data=new float[1920];while(mix.Read(data,default)>0){}watch.Stop();
                measurements.Add(new{sources,run,exportMs,previewDecodeMs=watch.Elapsed.TotalMilliseconds,cpuMs=(self.TotalProcessorTime-cpu).TotalMilliseconds,workingSetMb=self.WorkingSet64/1048576d,peakWorkingSetMb=self.PeakWorkingSet64/1048576d});
            }
        }
        string? evidence=Environment.GetEnvironmentVariable("REPLAYLOCAL_TEST_EVIDENCE");
        if(evidence!=null){Directory.CreateDirectory(evidence);File.WriteAllText(Path.Combine(evidence,"performance.json"),JsonSerializer.Serialize(measurements,new JsonSerializerOptions{WriteIndented=true}));}
        Assert.Equal(6,measurements.Count);
    }
}
