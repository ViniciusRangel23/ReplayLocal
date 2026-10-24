using System.ComponentModel;
using Kairos.App;
using Kairos.Engine;
using Kairos.Settings;

namespace Kairos.Ui;

public sealed class ApplicationAudioRow(ApplicationAudioSettings settings,Action changed) : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public string Id=>settings.Id;
    public bool IsAutomaticGame=>settings.AutomaticGame;
    public bool MeterActive=>Enabled && State=="available";
    string? _application;
    public string TargetLabel=>settings.AutomaticGame?(_application==null?Text.Get("audio.gameAutomatic"):Text.Format("audio.gameDetected",_application)):settings.Target.ExecutableName;
    public string Name{get=>settings.Name;set{settings.Name=value;Change();}}
    public bool Enabled{get=>settings.Enabled;set{settings.Enabled=value;Change();}}
    public double VolumePercent{get=>settings.Volume*100;set{settings.Volume=Math.Clamp(value,0,200)/100;Change();}}
    string _state="unavailable";double _peak=LevelMeter.Silent;
    public string State=>_state;
    public string StateLabel=>Text.Get((_state is "offline" or "captureOff"?"audio.feedback.":"audio.state.")+_state);
    public double PeakDb=>_peak;
    public double Level=>LevelMeter.ToBar(_peak)*100;
    void Change(){changed();PropertyChanged?.Invoke(this,new(null));}
    public void Update(string state,double peak,string? application=null){_state=state;_peak=double.IsFinite(peak)?Math.Clamp(peak,Engine.LevelMeter.Silent,0):Engine.LevelMeter.Silent;_application=application;PropertyChanged?.Invoke(this,new(null));}
}
