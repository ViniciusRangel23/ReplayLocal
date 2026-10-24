// Replay Local: modified 2026-10-24; independent fork of BasilForge/Kairos. See MODIFICATIONS.md. GPL-2.0-or-later.
using System.Collections.ObjectModel;
using System.Windows.Input;
using Kairos.App;
using Kairos.Engine.Audio;
using Kairos.Settings;

namespace Kairos.Ui;

public sealed partial class SettingsViewModel
{
    public ObservableCollection<ApplicationAudioRow> ApplicationAudioRows{get;}=[];
    public IReadOnlyList<AudioTargetCandidate> AudioCandidates=>(_caps?.AudioApplications??[]).DistinctBy(c=>c.Target.ExecutablePath,StringComparer.OrdinalIgnoreCase).ToArray();
    AudioTargetCandidate? _selectedAudioCandidate;
    public AudioTargetCandidate? SelectedAudioCandidate{get=>_selectedAudioCandidate;set{_selectedAudioCandidate=value;Raise(nameof(SelectedAudioCandidate),nameof(CanAddAudioApplication),nameof(CanAddGameAudio));CommandManager.InvalidateRequerySuggested();}}
    public bool ApplicationAudioSupported=>_caps?.ApplicationAudioSupported==true;
    public bool UseApplicationAudio
    {
        get=>_s.Audio.Mode==AudioCaptureMode.Applications;
        set
        {
            if(value&&!ApplicationAudioSupported){AudioValidationMessage=Text.Get("audio.appsUnsupported");RaiseAll();return;}
            Set(()=>_s.Audio.Mode=value?AudioCaptureMode.Applications:AudioCaptureMode.Legacy);
        }
    }
    public bool UseLegacyAudio=>!UseApplicationAudio;
    public bool CanAddGameAudio=>ApplicationAudioSupported && ApplicationAudioRows.Count<4 && !_s.Audio.Applications.Any(a=>a.AutomaticGame) && !Recording;
    public ICommand AddGameAudioCommand{get;private set;}=null!;
    public bool CanAddAudioApplication=>ApplicationAudioRows.Count<4&&SelectedAudioCandidate!=null&&!Recording;
    public string AudioValidationMessage{get;private set;}="";
    public ICommand AddAudioApplicationCommand{get;private set;}=null!;
    public ICommand RemoveAudioApplicationCommand{get;private set;}=null!;

    void InitializeApplicationAudioCommands()
    {
        AddGameAudioCommand=new RelayCommand(_=>
        {
            if(!CanAddGameAudio)return;
            AudioValidationMessage="";
            Set(()=>_s.Audio.Applications.Add(new(){AutomaticGame=true,Name=Text.Get("audio.gameSound")}));
            RebuildApplicationAudioRows();
        },_=>CanAddGameAudio);
        AddAudioApplicationCommand=new RelayCommand(_=>AddAudioApplication(),_=>CanAddAudioApplication);
        RemoveAudioApplicationCommand=new RelayCommand(o=>
        {
            if(Recording||o is not ApplicationAudioRow row)return;
            Set(()=>_s.Audio.Applications.RemoveAll(a=>a.Id==row.Id));RebuildApplicationAudioRows();
        },_=>!Recording);
    }
    void AddAudioApplication()
    {
        if(!CanAddAudioApplication)return;
        var candidate=SelectedAudioCandidate!;
        if(_s.Audio.Applications.Any(a=>!a.AutomaticGame && ApplicationAudioIdentity.Matches(a.Target,candidate.Target)))
        {AudioValidationMessage=Text.Get("audio.appsDuplicate");RaiseAll();return;}
        AudioValidationMessage="";
        Set(()=>_s.Audio.Applications.Add(new(){Target=candidate.Target,Name=Path.GetFileNameWithoutExtension(candidate.Target.ExecutableName)}));
        RebuildApplicationAudioRows();
    }
    void RebuildApplicationAudioRows()
    {
        ApplicationAudioRows.Clear();
        foreach(var a in _s.Audio.Applications)ApplicationAudioRows.Add(new(a,()=>{AudioValidationMessage="";Set(()=>{});}));
        _appAudioRows=ApplicationAudioRows.Where(r=>!r.IsAutomaticGame).ToArray();
        UpdateApplicationAudioStatus();
        Raise(nameof(ApplicationAudioRows),nameof(CanAddAudioApplication),nameof(CanAddGameAudio));
    }
    void UpdateApplicationAudioStatus()
    {
        foreach(var row in ApplicationAudioRows)
        {
            var status=_status?.AudioSources?.FirstOrDefault(s=>s.Id==row.Id);
            string state=SourcePending(row)?"pending":!row.Enabled?"disabled":_status==null?"offline":
                _status.CaptureEnabled==false?"captureOff":status?.State??"unavailable";
            row.Update(state,state=="available"?status?.PeakDb??Engine.LevelMeter.Silent:Engine.LevelMeter.Silent,status?.ApplicationName);
        }
        Raise(nameof(CanAddAudioApplication),nameof(CanAddGameAudio));
        RaiseAudioFeedback();
    }
    string? AudioApplyError()
    {
        if(ApplicationAudioSettings.Validate(_s.Audio) is {} error)return error;
        if(UseApplicationAudio&&!ApplicationAudioSupported)return Text.Get("audio.appsUnsupported");
        if(Recording&&SettingsDiff.NeedsRebuild(SettingsStore.Deserialize(_savedJson),_s))return Text.Get("audio.stopRecordingFirst");
        return null;
    }
}
