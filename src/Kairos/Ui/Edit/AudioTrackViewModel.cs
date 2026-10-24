using System.ComponentModel;

namespace Kairos.Ui.Edit;

public sealed class AudioTrackViewModel(EditorViewModel owner,string sourceId) : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public string SourceId=>sourceId;
    public string Name=>owner.Project.Audio?.Layout.Tracks.FirstOrDefault(t=>t.Id==sourceId)?.Name??sourceId;
    public double GainPercent{get=>owner.AudioLevel(sourceId).Gain*100;set{double g=Math.Clamp(value,0,200)/100;if(g!=owner.AudioLevel(sourceId).Gain)owner.SetAudioLevel(sourceId,l=>l with {Gain=g},true);}}
    public bool Muted{get=>owner.AudioLevel(sourceId).Muted;set{if(value!=Muted)owner.SetAudioLevel(sourceId,l=>l with {Muted=value},false);}}
    public bool IncludeInMix{get=>owner.AudioLevel(sourceId).Included;set{if(value!=IncludeInMix)owner.SetAudioLevel(sourceId,l=>l with {Included=value},false);}}
    public bool Solo{get=>owner.SoloSourceIds.Contains(sourceId);set=>owner.SetAudioSolo(sourceId,value);}
    public double PartGainPercent{get=>owner.PartAudioGain(sourceId)*100;set=>owner.SetPartAudioGain(sourceId,Math.Clamp(value,0,200)/100);}
    internal void Refresh()=>PropertyChanged?.Invoke(this,new(null));
}
