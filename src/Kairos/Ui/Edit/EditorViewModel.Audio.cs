using System.Collections.ObjectModel;
using System.Windows.Input;
using Kairos.App;
using Kairos.Engine.Audio;
using Kairos.Engine.Edit;

namespace Kairos.Ui.Edit;

public sealed partial class EditorViewModel
{
    public ObservableCollection<AudioTrackViewModel> AudioTracks{get;}=[];
    string? _selectedAudioId;
    public AudioTrackViewModel? SelectedAudioTrack=>AudioTracks.FirstOrDefault(t=>t.SourceId==_selectedAudioId);
    public bool IsAudioPage=>Page==InspectorPage.Audio;
    public void SelectAudioTrack(string sourceId)
    {
        if(!AudioTracks.Any(t=>t.SourceId==sourceId))return;
        _ids.Clear();_parts.Clear();_mode=EditMode.None;
        ChoiceChanged();
        _selectedAudioId=sourceId;
        RaisePage();
    }
    readonly HashSet<string> _soloSourceIds=[];
    public IReadOnlySet<string> SoloSourceIds=>_soloSourceIds;
    public bool HasDynamicAudio=>!IsPicture&&Project.Audio!=null;
    public bool HasLegacyAudio=>HasSound&&!HasDynamicAudio;
    public bool CanUseLegacyAudioLayout=>Project.Audio is {} audio&&audio.Layout.Tracks.Count==3&&audio.Layout.Tracks.All(t=>t.Role==AudioRole.Generic);
    public ICommand UseLegacyAudioLayoutCommand{get;private set;}=null!;
    AudioExportMode _exportAudioMode;
    public AudioExportMode ExportAudioMode{get=>_exportAudioMode;set{_exportAudioMode=value;Raise(nameof(ExportAudioMode));Raise(nameof(ExportSeparateAudio));}}
    public bool ExportSeparateAudio{get=>ExportAudioMode==AudioExportMode.Separate;set=>ExportAudioMode=value?AudioExportMode.Separate:AudioExportMode.Mixed;}

    void InitializeAudioControls()
    {
        UseLegacyAudioLayoutCommand=new RelayCommand(_=>
        {
            if(!CanUseLegacyAudioLayout)return;
            var tracks=Project.Audio!.Layout.Tracks;
            var layout=new AudioLayout(1,[tracks[0] with {Id="mix",Name=Text.Get("editor.audioMix"),Role=AudioRole.Mix},
                tracks[1] with {Id="system",Name=Text.Get("editor.game"),Role=AudioRole.Application},
                tracks[2] with {Id="microphone",Name=Text.Get("editor.mic"),Role=AudioRole.Microphone}]);
            Apply(Project with {Audio=AudioEdit.Create(layout)});
        },_=>CanUseLegacyAudioLayout);
        RefreshAudioRows();
    }
    internal AudioLevel AudioLevel(string id)=>Project.Audio?.Levels.FirstOrDefault(l=>l.SourceId==id)??new(id);
    internal void SetAudioLevel(string id,Func<AudioLevel,AudioLevel> change,bool amend)
    {
        if(Project.Audio==null)return;
        var next=Project with {Audio=Project.Audio with {Levels=Project.Audio.Levels.Select(l=>l.SourceId==id?change(l):l).ToArray()}};
        if(amend)Amend("audio:"+id,next);else Apply(next);
    }
    internal void SetAudioSolo(string id,bool enabled)
    {
        bool changed=enabled?_soloSourceIds.Add(id):_soloSourceIds.Remove(id);
        if(!changed)return;
        foreach(var row in AudioTracks)row.Refresh();
        Raise(nameof(SoloSourceIds));
    }
    internal double PartAudioGain(string id)=>FirstPart?.AudioGains?.GetValueOrDefault(id,1)??1;
    internal void SetPartAudioGain(string id,double gain)
    {
        if(Math.Abs(PartAudioGain(id)-gain)<1e-9)return;
        ChangeParts("audio:"+id,k=>
        {
            var gains=k.AudioGains?.ToDictionary(kv=>kv.Key,kv=>kv.Value)??[];gains[id]=gain;
            return k with {AudioGains=gains};
        });
    }
    void RefreshAudioRows()
    {
        var ids=Project.Audio?.Levels.Select(l=>l.SourceId).ToArray()??[];
        if(!AudioTracks.Select(t=>t.SourceId).SequenceEqual(ids))
        {
            AudioTracks.Clear();foreach(string id in ids)AudioTracks.Add(new(this,id));
            _soloSourceIds.RemoveWhere(id=>!ids.Contains(id));
        }
        if(!ids.Contains(_selectedAudioId))_selectedAudioId=null;
        foreach(var row in AudioTracks)row.Refresh();
        Raise(nameof(SelectedAudioTrack));
        Raise(nameof(HasDynamicAudio));Raise(nameof(HasLegacyAudio));Raise(nameof(CanUseLegacyAudioLayout));
    }
}
