using System.Text.Json;
using Kairos.App;
using Kairos.Engine;
using Kairos.Settings;

namespace Kairos.Ui;

public sealed partial class SettingsViewModel
{
    string? _audioSavedJson;
    AudioSettings? _audioSaved;
    AudioSettings SavedAudio
    {
        get
        {
            if(_audioSaved==null || _audioSavedJson!=_savedJson)
            {
                _audioSavedJson=_savedJson;
                try{_audioSaved=SettingsStore.Deserialize(string.IsNullOrEmpty(_savedJson)?"{}":_savedJson).Audio;}
                catch(JsonException){_audioSaved=new();}
            }
            return _audioSaved;
        }
    }
    bool SourcePending(ApplicationAudioRow row)
    {
        var current=_s.Audio.Applications.FirstOrDefault(a=>a.Id==row.Id);
        if(current==null)return true;
        var before=SavedAudio.Applications.FirstOrDefault(a=>a.Id==row.Id);
        return SavedAudio.Mode!=_s.Audio.Mode || before==null ||
            JsonSerializer.Serialize(before,Ipc.Json)!=JsonSerializer.Serialize(current,Ipc.Json);
    }
    IReadOnlyList<ApplicationAudioRow> _appAudioRows=[];
    public IReadOnlyList<ApplicationAudioRow> AppAudioRows=>_appAudioRows;
    public ApplicationAudioRow? GameAudioRow=>ApplicationAudioRows.FirstOrDefault(r=>r.IsAutomaticGame);
    public bool GameAudioEnabled
    {
        get=>_s.Audio.Applications.FirstOrDefault(a=>a.AutomaticGame)?.Enabled==true;
        set
        {
            if(!CanToggleGameAudio)return;
            var source=_s.Audio.Applications.FirstOrDefault(a=>a.AutomaticGame);
            if(source==null && !value)return;
            Set(()=>
            {
                if(source==null)_s.Audio.Applications.Add(new(){AutomaticGame=true,Name=Text.Get("audio.gameSound")});
                else source.Enabled=value;
            });
            RebuildApplicationAudioRows();
        }
    }
    public bool HasGameAudio=>GameAudioRow!=null;
    public bool CanToggleGameAudio=>UseApplicationAudio && ApplicationAudioSupported && !Recording &&
        (GameAudioRow!=null || ApplicationAudioRows.Count<4);
    public double GameAudioVolume{get=>GameAudioRow?.VolumePercent??100;set{if(GameAudioRow is {} row)row.VolumePercent=value;}}
    public string GameFeedbackState=>!UseApplicationAudio?"modeOff":GameAudioRow?.State??"disabled";
    public bool GameCapacityReached=>UseApplicationAudio && GameAudioRow==null && ApplicationAudioRows.Count>=4;
    public string GameStatusLabel=>GameCapacityReached?Text.Get("audio.feedback.gameLimit"):FeedbackLabel(GameFeedbackState);
    public string GameTargetLabel=>GameAudioRow?.TargetLabel??Text.Get("audio.gameAutomatic");
    public bool GameMeterActive=>GameAudioRow?.MeterActive==true && UseApplicationAudio;
    public double GamePeakDb=>GameMeterActive?GameAudioRow!.PeakDb:LevelMeter.Silent;
    public bool CanToggleMicrophone=>_s.Audio.MicConfirmed && _s.Audio.MicDevice!="default";
    public string MicFeedbackState
    {
        get
        {
            if(!CanToggleMicrophone)return "selectDevice";
            var saved=SavedAudio;
            if(saved.Mic!=_s.Audio.Mic || saved.MicDevice!=_s.Audio.MicDevice || saved.MicVolume!=_s.Audio.MicVolume)return "pending";
            if(!Mic)return "disabled";
            if(_caps!=null && !_caps.Mics.Any(m=>m.Value==_s.Audio.MicDevice))return "deviceMissing";
            if(_status==null)return "offline";
            if(_status.CaptureEnabled==false)return "captureOff";
            if(_status.Engine==null && _status.Error!=null)return "unavailable";
            return "available";
        }
    }
    public bool MicMeterActive=>MicFeedbackState=="available";
    public string MicStatusLabel=>FeedbackLabel(MicFeedbackState);
    static string FeedbackLabel(string state)=>Text.Get(state is "modeOff" or "selectDevice" or "deviceMissing" or "offline" or "captureOff" or "unavailable"
        ?"audio.feedback."+state:"audio.state."+state);
    void RaiseAudioFeedback()=>Raise(nameof(AppAudioRows),nameof(GameAudioRow),nameof(GameAudioEnabled),nameof(CanToggleGameAudio),
        nameof(HasGameAudio),nameof(GameCapacityReached),nameof(GameAudioVolume),nameof(GameFeedbackState),nameof(GameStatusLabel),nameof(GameTargetLabel),nameof(GameMeterActive),nameof(GamePeakDb),
        nameof(CanToggleMicrophone),nameof(MicFeedbackState),nameof(MicMeterActive),nameof(MicStatusLabel));
}
