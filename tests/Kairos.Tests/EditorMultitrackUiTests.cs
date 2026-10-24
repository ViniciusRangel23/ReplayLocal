using Kairos.Engine.Audio;
using Kairos.Engine.Edit;
using Kairos.Ui.Edit;

namespace Kairos.Tests;

public static class EditorMultitrackUiTests
{
    [Test]
    public static void Audio_list_gets_remaining_inspector_height()
    {
        Exception? failure=null;
        var thread=new Thread(()=>
        {
            try
            {
                var panel=new FitPanel();panel.Children.Add(new System.Windows.Controls.Border{Height=80});
                var scroll=new System.Windows.Controls.ScrollViewer{Content=new System.Windows.Controls.Border{Height=500}};
                var grid=new System.Windows.Controls.Grid();grid.Children.Add(scroll);FitPanel.SetFlexible(grid,true);panel.Children.Add(grid);
                panel.Measure(new(220,150));panel.Arrange(new(0,0,220,150));
                Assert.True(grid.ActualHeight<=70&&grid.ActualHeight>0,"audio list stays inside inspector");
            }catch(Exception e){failure=e;}
        });thread.SetApartmentState(ApartmentState.STA);thread.Start();thread.Join();
        Assert.True(failure==null,failure?.ToString()??"");
    }
    static EditorViewModel Model()=>new(EditProject.For("synthetic.mp4",4) with { Audio=AudioEdit.Create(AudioEditTests.Layout) },false,EditorMode.Library,16/9d,4);
    [Test]
    public static void Named_controls_have_undo_and_solo_is_preview_only()
    {
        var vm=Model();Assert.Equal(3,vm.AudioTracks.Count);Assert.Equal("Discord",vm.AudioTracks[1].Name);
        Assert.Equal(AudioExportMode.Mixed,vm.ExportAudioMode);
        var project=vm.Project;
        vm.AudioTracks[1].Solo=true;
        Assert.True(ReferenceEquals(project,vm.Project)&&vm.SoloSourceIds.Contains("discord"),"solo does not edit project");
        vm.AudioTracks[1].Muted=true;Assert.True(vm.Project.Audio!.Levels[1].Muted,"mute enters project");
        vm.UndoCommand.Execute(null);Assert.True(!vm.AudioTracks[1].Muted,"undo restores row");
        vm.AudioTracks[0].GainPercent=50;Assert.Equal(0.5,vm.Project.Audio!.Levels[0].Gain);
        vm.SelectPart(0);vm.AudioTracks[0].PartGainPercent=25;Assert.Equal(0.25,vm.Project.Kept[0].AudioGains!["game"]);
    }
    [Test]
    public static void Legacy_interpretation_is_explicit_and_reset_keeps_layout()
    {
        var layout=new AudioLayout(1,AudioEditTests.Layout.Tracks.Take(3).Select(t=>t with {Role=AudioRole.Generic}).ToArray());
        var vm=new EditorViewModel(EditProject.For("external.mp4",4) with {Audio=AudioEdit.Create(layout)},false,EditorMode.Library,1.7,3);
        Assert.True(vm.CanUseLegacyAudioLayout&&vm.AudioTracks.Count==3,"no implicit inference");
        vm.UseLegacyAudioLayoutCommand.Execute(null);
        Assert.Equal(2,vm.AudioTracks.Count);Assert.Equal("microphone",vm.AudioTracks[1].SourceId);
        vm.AudioTracks[0].GainPercent=50;vm.ConfirmResetCommand.Execute(null);
        Assert.True(vm.Project.Audio!=null&&vm.AudioTracks.Count==2,"reset does not destroy layout");
    }
    [Test]
    public static void Unchanged_audio_clip_can_open_export_chooser()
    {
        var vm=Model();Assert.True(vm.Project.IsUnchanged,"fixture unchanged");
        Assert.True(vm.PrimaryCommand.CanExecute(null),"export format conversion is available without an edit");
        bool asked=false;vm.SaveAsked+=()=>asked=true;vm.PrimaryCommand.Execute(null);
        Assert.True(asked,"export chooser reached");
    }
}
