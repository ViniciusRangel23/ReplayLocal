using Kairos.Engine.Audio;
using Kairos.Engine.Edit;
using Kairos.Ui.Edit;
namespace Kairos.Tests;
public static class AudioTimelineTests
{
    static EditProject Project(int count)=>EditProject.For("synthetic.mp4",12) with {Audio=AudioEdit.Create(new(1,
        new[]{new AudioTrackInfo("mix",1,"Mix",AudioRole.Mix)}.Concat(Enumerable.Range(0,count).Select(i=>new AudioTrackInfo($"source{i}",i+2,$"Source {i}",AudioRole.Application))).ToArray()))};
    [Test]
    public static void Each_source_reserves_a_timeline_lane_below_video()
    {
        var p=Project(4);var vm=new EditorViewModel(p,false,EditorMode.Library,16/9d,5);
        var layout=new TimelineLayout(p,vm.Rows,900);
        Assert.Near(layout.RowHeightOf(0)*5,layout.RowTop(1)-layout.RowTop(0),0.001,"video plus four source lanes precede text");
        var smaller=new TimelineLayout(Project(2),vm.Rows,900);
        Assert.Near(layout.RowHeightOf(0)*2,layout.Height-smaller.Height,0.001,"more recorded sources add matching lanes");
    }
    [Test]
    public static void Source_ids_names_and_cut_boxes_follow_the_video_at_any_zoom()
    {
        var p=Project(5).SplitAt(3).SplitAt(8).SetSpeed(0,3,2);
        var vm=new EditorViewModel(p,false,EditorMode.Library,16/9d,6);
        foreach(double zoom in new[]{1d,3d})
        {
            var layout=new TimelineLayout(p,vm.Rows,900,zoom,40,maxHeight:260);
            Assert.Equal(5,layout.AudioRows.Count);
            Assert.True(!layout.AudioRows.Any(r=>r.Id=="mix"),"default mixture not duplicated as a source");
            Assert.True(layout.Height<=260,"five sources fit timeline cap");
            for(int r=0;r<layout.AudioRows.Count;r++)
            {
                Assert.Equal($"source{r}",layout.AudioRows[r].Id);Assert.Equal($"Source {r}",layout.AudioRows[r].Name);
                var video=layout.PartBoxes();var audio=layout.AudioBoxes(r);
                Assert.Equal(video.Count,audio.Count);
                for(int i=0;i<video.Count;i++){Assert.Near(video[i].X,audio[i].X,0.001);Assert.Near(video[i].Width,audio[i].Width,0.001);}
                double y=layout.AudioRowTop(r)+layout.AudioRowHeight/2;
                Assert.Equal(r,layout.AudioRowAt(y));Assert.Equal(-2,layout.RowAt(y));
                Assert.Equal(System.Windows.Input.Cursors.Arrow,EditTimeline.CursorAt(layout,new(TimelineLayout.HeadWidth+8,y)));
            }
            Assert.Equal(1,layout.RowAt(layout.RowTop(1)+1));
        }
    }
    [Test]
    public static void Source_selection_opens_only_its_controls_and_preserves_undo()
    {
        var vm=new EditorViewModel(Project(4),false,EditorMode.Library,16/9d,5);
        vm.SelectPart(0);vm.SelectAudioTrack("source2");
        Assert.True(vm.IsAudioPage&&vm.SelectedParts.Count==0,"audio selection clears video selection");
        Assert.Equal("Source 2",vm.PageTitle);Assert.True(!vm.DeleteCommand.CanExecute(null),"audio selection cannot delete video");
        var selected=vm.SelectedAudioTrack!;selected.GainPercent=40;selected.Muted=true;
        Assert.Equal(0.4,vm.Project.Audio!.Levels[2].Gain);Assert.True(selected.Muted,"mute applied");
        Assert.Equal(1d,vm.AudioTracks[0].GainPercent/100);
        vm.UndoCommand.Execute(null);Assert.True(!selected.Muted&&vm.IsAudioPage,"undo preserves selected source");
        var before=vm.Project;selected.Solo=true;Assert.True(ReferenceEquals(before,vm.Project),"solo remains preview only");
        bool closed=false;vm.Closed+=()=>closed=true;
        vm.KeyCommand("Escape");Assert.True(!vm.IsAudioPage&&!closed,"Escape clears audio selection before leaving editor");
        vm.SelectAudioTrack("source1");vm.SelectPart(0);Assert.True(!vm.IsAudioPage&&vm.IsPartPage,"video click returns part controls");
        vm.SelectAudioTrack("missing");Assert.True(vm.IsPartPage,"unknown source is not selectable");
    }
    [Test]
    public static void Click_hit_chooses_the_correct_audio_source_without_moving_a_bar()
    {
        Exception? failure=null;var thread=new Thread(()=>
        {
            try
            {
                var vm=new EditorViewModel(Project(3),false,EditorMode.Library,16/9d,4);
                var timeline=new EditTimeline(vm);var layout=new TimelineLayout(vm.Project,vm.Rows,900);
                var before=vm.Project;
                Assert.True(timeline.ChooseAudio(layout,new(20,layout.AudioRowTop(1)+5)),"audio row clicked");
                Assert.Equal("source1",vm.SelectedAudioTrack!.SourceId);
                Assert.True(ReferenceEquals(before,vm.Project),"selecting a lane never changes audio or video timing");
                Assert.True(!timeline.ChooseAudio(layout,new(20,layout.RowTop(0)+5)),"video is not an audio hit");
            }catch(Exception e){failure=e;}
        });thread.SetApartmentState(ApartmentState.STA);thread.Start();thread.Join();Assert.True(failure==null,failure?.ToString()??"");
    }
    [Test]
    public static void Silent_video_has_no_audio_rows_and_mixed_clip_has_one()
    {
        var p=EditProject.For("silent.mp4",12);var vm=new EditorViewModel(p,false,EditorMode.Library,16/9d,0);
        Assert.Equal(0,new TimelineLayout(p,vm.Rows,900).AudioRows.Count);
        var mix=Project(0);Assert.Equal(1,new TimelineLayout(mix,vm.Rows,900).AudioRows.Count);
        Assert.Equal("mix",new TimelineLayout(mix,vm.Rows,900).AudioRows[0].Id);
    }
}
