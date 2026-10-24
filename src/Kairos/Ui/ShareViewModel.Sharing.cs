// Replay Local: modified 2026-10-23; independent fork of BasilForge/Kairos. See MODIFICATIONS.md. GPL-2.0-or-later.
using Kairos.Engine.Share;
using Kairos.Settings;

namespace Kairos.Ui;

public sealed partial class ShareViewModel
{
    void Prepare()
    {
        if (!HasClip || _phase != Phase.Choosing || _platform is not { } platform)
            return;
        StopPlayback();
        _host.Remember(new ShareSettings
        {
            VerticalFor = [.. _verticalFor],
            CropPosition = _crop,
            Tags = SharePlanner.RememberTags(_caption, Game),
            PreviewVolume = _volume,
            PreviewMuted = _muted,
        });
        _resultPlatform = platform;
        _resultCaption = _caption;
        if (_picture != null)
        {
            PreparePicture(platform, _cts = new CancellationTokenSource());
            return;
        }

        var spec = CurrentSpec;
        var plan = CurrentPlan();
        string source = _clip;
        string target = ShareFiles.Target(_host.Folder, platform, _clip);
        var cts = _cts = new CancellationTokenSource();
        _progress = 0;
        _phase = Phase.Working;
        RaiseAll();

        _host.Run(() =>
        {
            try
            {
                var progress = new InlineProgress(p => _host.Post(() =>
                {
                    _progress = p;
                    Raise(nameof(ProgressPercent));
                    Raise(nameof(ProgressText));
                }));
                var result = _host.Transcode(source, target, plan, spec.MaxBytes, progress, cts.Token);
                _host.Post(() => Finish(result, cts));
            }
            catch (OperationCanceledException)
            {
                _host.Post(() => { if (_cts == cts) { _phase = Phase.Choosing; RaiseAll(); } });
            }
            catch (Exception e)
            {
                _host.Post(() => { if (_cts == cts) { _errorDetail = e.Message; _phase = Phase.Failed; RaiseAll(); } });
            }
        });
    }

    void Finish(ShareResult result, CancellationTokenSource cts)
    {
        if (_cts != cts || cts.IsCancellationRequested)
        {
            _phase = Phase.Choosing;
            RaiseAll();
            return;
        }
        _result = result;
        _host.CopyToClipboard(result.Path, null);
        _phase = Phase.Done;
        RaiseAll();
    }

    void PreparePicture(SharePlatform platform, CancellationTokenSource cts)
    {
        _host.CopyPicture(_clip);
        _result = new ShareResult(_clip, _picture!.Bytes, "picture");
        _phase = Phase.Done;
        RaiseAll();
    }

    void Edited()
    {
        if (_phase == Phase.Done) _phase = Phase.Choosing;
        RaiseAll();
    }

    void Retry()
    {
        if (_info == null)
            Open(_clip, Thumbnail);
        else
        {
            _phase = Phase.Choosing;
            RaiseAll();
        }
    }

    /// <summary>"4:29.5": the trim moves in tenths of a second.</summary>
    static string ClockTenths(double seconds)
    {
        double t = Math.Round(Math.Max(0, seconds), 1);
        int whole = (int)t;
        return $"{whole / 60}:{whole % 60:00}.{(int)Math.Round((t - whole) * 10) % 10}";
    }

    static string Size(long bytes) => bytes >= 1_000_000_000 ? $"{bytes / 1e9:0.0} GB" : $"{bytes / 1e6:0.0} MB";

    static string RoundedSize(long bytes) => bytes >= 1_000_000_000 ? $"{bytes / 1e9:0.0} GB"
        : bytes >= 10_000_000 ? $"{Math.Round(bytes / 1e6):0} MB" : $"{bytes / 1e6:0.0} MB";
}
