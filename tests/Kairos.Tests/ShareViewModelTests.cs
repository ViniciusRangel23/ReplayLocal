// Replay Local: modified 2026-10-23; independent fork of BasilForge/Kairos. See MODIFICATIONS.md. GPL-2.0-or-later.
using Kairos.App;
using Kairos.Engine.Share;
using Kairos.Settings;
using Kairos.Ui;

namespace Kairos.Tests;

public static class ShareViewModelTests
{
    const string Clip = @"D:\Clips\ELDEN RING\ELDEN RING 2026-10-12 21-04-55.mp4";

    sealed class FakeHost : IShareHost
    {
        public MediaInfo Info = new(300, 2560, 1440, 60, "av1", "aac", 192_000, 3, Bytes: 333_000_000);
        public readonly List<(string Source, string Target, double Start, double End)> Cuts = [];
        public Func<SharePlan, ShareResult?>? OnTranscode = null;
        public readonly List<(string Source, string Target, SharePlan Plan, long MaxBytes)> Transcodes = [];
        public (string? File, string? Text)? Clipboard;
        public ShareSettings? Remembered;

        public int Probes;
        public MediaInfo Probe(string clip)
        {
            Probes++;
            return Info;
        }
        public SharePicture? Picture = new(null, 2560, 1440, 2_000_000);
        public SharePicture? ReadPicture(string path) => Picture;
        public readonly List<(string Source, string Target, long MaxBytes)> Jpegs = [];
        public ShareResult MakeJpeg(string source, string target, long maxBytes)
        {
            Jpegs.Add((source, target, maxBytes));
            return new ShareResult(target, maxBytes - 1, "jpeg");
        }
        public int Jobs() => Jpegs.Count;
        public readonly List<string> CopiedPictures = [];
        public void CopyPicture(string path) => CopiedPictures.Add(path);

        public ShareResult Transcode(string source, string target, SharePlan plan, long maxBytes, IProgress<double> progress, CancellationToken ct)
        {
            Transcodes.Add((source, target, plan, maxBytes));
            progress.Report(0.5);
            return OnTranscode?.Invoke(plan) ?? new ShareResult(target, 18_200_000, "h264_nvenc");
        }

        public ShareResult Cut(string source, string target, double start, double end, IProgress<double> progress, CancellationToken ct)
        {
            Cuts.Add((source, target, start, end));
            progress.Report(0.5);
            return new ShareResult(target, 33_000_000, "copy");
        }

        public void CopyToClipboard(string? file, string? text) => Clipboard = (file, text);
        public void GrabFrames(string clip, double[] times, int width, bool exact, Action<int, System.Windows.Media.Imaging.BitmapSource?> done) { }
        public void ReleaseFrames() { }
        public readonly List<(double From, double To)> Plays = [];
        public int Stops;
        public Action<double>? Position;
        public Action<string?>? Ended;
        public System.Windows.Media.Imaging.BitmapSource? Play(string clip, double from, double to, int width, int height,
            Action<double> position, Action<string?> ended)
        {
            Plays.Add((from, to));
            Position = position;
            Ended = ended;
            return null;
        }
        public void StopPlaying() => Stops++;
        public double Volume = -1;
        public void SetPlaybackVolume(double volume) => Volume = volume;
        readonly List<Action> _later = [];
        public void After(TimeSpan delay, Action action) => _later.Add(action);
        /// <summary>Runs what was put off (the "copied" check going away).</summary>
        public void RunLater()
        {
            var due = _later.ToList();
            _later.Clear();
            due.ForEach(a => a());
        }
        public void ShowInFolder(string path) { }
        public void Remember(ShareSettings prefs) => Remembered = prefs;
        public void Post(Action action) => action();
        public void Run(Action work) => work();
        public string Folder => @"C:\Share";
    }

    static (ShareViewModel Vm, FakeHost Host) Open(Action<ShareSettings>? prefs = null, MediaInfo? info = null, SharePlatform? platform = null)
    {
        Text.Language = "en";
        var host = new FakeHost();
        if (info != null)
            host.Info = info;
        var saved = new ShareSettings();
        prefs?.Invoke(saved);
        var vm = new ShareViewModel(host, () => saved);
        vm.Open(Clip);
        if (platform != null)
            vm.Platform = platform;
        return (vm, host);
    }

    /// <summary>Opened, with the cut moved to the last 30 seconds of the 5:00 clip.</summary>
    static (ShareViewModel Vm, FakeHost Host) OpenCut()
    {
        var (vm, host) = Open(platform: SharePlatform.Copy);
        vm.TrimStart = 270;
        return (vm, host);
    }

    [Test]
    public static void Copy_of_a_cut_is_cut_without_converting()
    {
        var (vm, host) = OpenCut();
        vm.CopyCommand.Execute(null);
        Assert.Equal(0, host.Transcodes.Count, "not converted");
        var cut = host.Cuts.Single();
        Assert.Equal((Clip, 270.0, 300.0), (cut.Source, cut.Start, cut.End), "the part");
        Assert.Equal(Path.Combine(@"C:\Share", "Copy", "ELDEN RING 2026-10-12 21-04-55.mp4"), cut.Target, "target");
        Assert.Equal((cut.Target, (string?)null), host.Clipboard!.Value, "on the clipboard");
        Assert.True(vm.IsCopied, "copied");
    }

    [Test]
    public static void Copy_works_without_picking_a_platform()
    {
        var (vm, host) = Open();
        Assert.True(vm.CopyCommand.CanExecute(null), "available at once");
        vm.CopyCommand.Execute(null);
        Assert.Equal(Clip, host.Clipboard!.Value.File, "copied");
        Assert.Equal((SharePlatform?)SharePlatform.Copy, vm.Platform, "local destination selected");
    }

    [Test]
    public static void An_unreadable_clip_hides_the_choices_and_offers_to_try_again()
    {
        Text.Language = "en";
        var host = new FakeHost();
        var vm = new ShareViewModel(new ThrowingProbe(host), () => new ShareSettings());
        vm.Open(Clip);
        Assert.True(vm.IsFailed && !vm.HasClip, "failed without a clip");
        Assert.Equal("Try again", vm.RetryLabel, "label");
        Assert.True(vm.ErrorDetail.Contains("not found"), vm.ErrorDetail);
    }

    const string Shot = @"D:\Clips\ELDEN RING\ELDEN RING 2026-10-15 21-30-00.png";

    static (ShareViewModel Vm, FakeHost Host) OpenShot(long bytes = 2_000_000, SharePlatform? platform = null)
    {
        Text.Language = "en";
        var host = new FakeHost { Picture = new SharePicture(null, 2560, 1440, bytes) };
        var vm = new ShareViewModel(host, () => new ShareSettings());
        vm.Open(Shot);
        if (platform != null)
            vm.Platform = platform;
        return (vm, host);
    }

    [Test]
    public static void A_screenshot_opens_as_a_picture_without_trim_or_player()
    {
        var (vm, host) = OpenShot();
        Assert.True(vm.IsPicture && vm.HasClip && !vm.HasVideo, "a picture to share, not a video");
        Assert.Equal(0, host.Probes, "no video reading");
        Assert.Near(16.0 / 9, vm.PreviewAspect, 0.001, "shown in its own shape");
        Assert.True(!vm.PlayCommand.CanExecute(null), "nothing to play");
        Assert.True(vm.IsChoosing, "ready to choose where");
        Assert.Equal("Share screenshot", vm.Title, "the panel says what it shares");
    }

    [Test]
    public static void Closing_hides_the_panel()
    {
        var (vm, _) = Open();
        vm.CloseCommand.Execute(null);
        Assert.True(!vm.IsOpen, "closed");
    }

    [Test]
    public static void One_short_line_says_where_the_cut_is_and_a_rounded_size()
    {
        var (vm, _) = OpenCut();
        Assert.Equal("4:30.0 – 5:00.0", vm.TrimRange, "the cut");
        Assert.True(System.Text.RegularExpressions.Regex.IsMatch(vm.SizeEstimate, @"^\d+ MB$"), "a plain rounded number: " + vm.SizeEstimate);
    }

    [Test]
    public static void The_preview_volume_turns_the_playback_down_or_off_and_is_kept_for_next_time()
    {
        var (vm, host) = OpenCut();
        Assert.True(vm.PreviewVolume == 1 && !vm.IsMuted, "full at first");
        vm.PlayCommand.Execute(null);
        Assert.Equal(1.0, host.Volume, "the player is told");
        vm.PreviewVolume = 0.4;
        Assert.Equal(0.4, host.Volume, "turned down while playing");
        vm.MuteCommand.Execute(null);
        Assert.True(vm.IsMuted && host.Volume == 0, "muted");
        vm.MuteCommand.Execute(null);
        Assert.True(!vm.IsMuted && host.Volume == 0.4, "back to where it was");
        vm.PreviewVolume = 0;
        Assert.True(vm.IsMuted, "all the way down is muted");
        vm.MuteCommand.Execute(null);
        Assert.True(!vm.IsMuted && vm.PreviewVolume > 0, "unmuting from zero gives some sound back");
        vm.PreviewVolume = 0.3;
        vm.MuteCommand.Execute(null);
        vm.CloseCommand.Execute(null);
        Assert.Equal((0.3, true), (host.Remembered!.PreviewVolume, host.Remembered.PreviewMuted), "kept for next time");
        var (again, _) = Open(p => { p.PreviewVolume = 0.3; p.PreviewMuted = true; });
        Assert.True(again.PreviewVolume == 0.3 && again.IsMuted, "opens as it was left");
    }

    [Test]
    public static void The_emoji_picker_offers_pictures_for_each_of_its_emoji()
    {
        Text.Language = "en";
        Assert.Equal("Popular Faces Hands Games and sports Hearts and symbols Animals and food",
            string.Join(" ", EmojiPicks.Categories.Select(c => c.Name)), "tabs, popular first");
        Assert.True(EmojiPicks.Categories.All(c => c.Emoji.Count >= 40 && c.Emoji.Count == c.Emoji.Distinct().Count()),
            "at least forty in each tab, none twice in a tab");
        Assert.Equal(40, EmojiPicks.Categories[0].Emoji.Count, "forty popular ones");
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(root.FullName, "Kairos.sln")))
            root = root.Parent!;
        foreach (var pick in EmojiPicks.All)
        {
            string file = Path.Combine(root.FullName, "src", "Kairos", "Ui", "Assets", "Emoji", pick.File);
            Assert.True(File.Exists(file), $"{pick.Emoji}: {pick.File}");
        }
        Assert.Equal("🔥", EmojiPicks.Categories[0].Emoji[0].Emoji, "fire first");
    }

    [Test]
    public static void A_click_on_the_picture_plays_and_its_play_mark_shows_only_while_paused()
    {
        var (vm, host) = OpenCut();
        Assert.True(vm.ShowPlayMark, "play mark on the paused picture");
        vm.PlayCommand.Execute(null);
        Assert.True(!vm.ShowPlayMark, "hidden while playing");
        host.Ended!(null);
        Assert.True(vm.ShowPlayMark, "back at the end");
        vm.PrepareCommand.Execute(null);
        Assert.True(vm.IsDone && vm.ShowPlayMark, "after sharing the picture is still a player");
    }

    [Test]
    public static void Play_runs_the_cut_from_its_start_and_the_playhead_follows()
    {
        var (vm, host) = OpenCut();
        vm.PlayCommand.Execute(null);
        Assert.Equal((270.0, 300.0), host.Plays.Single(), "the cut");
        Assert.True(vm.IsPlaying, "playing");
        host.Position!(281.5);
        Assert.Equal(281.5, vm.Playhead, "playhead follows");
        host.Position!(300);
        host.Ended!(null);
        Assert.True(!vm.IsPlaying, "stopped at the end");
        vm.PlayCommand.Execute(null);
        Assert.Equal((270.0, 300.0), host.Plays[1], "from the start again");
    }

    [Test]
    public static void Pausing_and_playing_again_continues_where_it_was()
    {
        var (vm, host) = OpenCut();
        vm.PlayCommand.Execute(null);
        host.Position!(284);
        vm.PlayCommand.Execute(null);
        Assert.True(!vm.IsPlaying && host.Stops == 1, "paused");
        vm.PlayCommand.Execute(null);
        Assert.Equal((284.0, 300.0), host.Plays[1], "continues");
    }

    [Test]
    public static void Clicking_the_strip_moves_the_playhead_inside_the_cut()
    {
        var (vm, host) = OpenCut();
        vm.Playhead = 290;
        vm.PlayCommand.Execute(null);
        Assert.Equal((290.0, 300.0), host.Plays.Single(), "from the clicked point");
        vm.Playhead = 10;
        Assert.Equal(270.0, vm.Playhead, "kept inside the cut");
        Assert.Equal(2, host.Plays.Count, "seeking while playing plays on from there");
    }

    [Test]
    public static void Moving_the_trim_closing_or_converting_stops_the_playback()
    {
        var (vm, host) = OpenCut();
        vm.PlayCommand.Execute(null);
        vm.TrimStart = 260;
        Assert.True(!vm.IsPlaying && host.Stops == 1, "trim moved");
        vm.PlayCommand.Execute(null);
        vm.PrepareCommand.Execute(null);
        Assert.True(!vm.IsPlaying && host.Stops == 2, "converting");
        var (other, otherHost) = Open();
        other.PlayCommand.Execute(null);
        other.CloseCommand.Execute(null);
        Assert.True(!other.IsPlaying && otherHost.Stops == 1, "closed");
    }

    [Test]
    public static void The_playhead_shows_while_playing_or_once_moved_but_not_on_the_start_handle()
    {
        var (vm, host) = OpenCut();
        Assert.True(!vm.ShowPlayhead, "hidden at the start of the cut");
        vm.PlayCommand.Execute(null);
        Assert.True(vm.ShowPlayhead, "shown while playing");
        host.Position!(284);
        vm.PlayCommand.Execute(null);
        Assert.True(vm.ShowPlayhead, "shown where it was paused");
        vm.TrimStart = 275;
        Assert.True(!vm.ShowPlayhead, "back on the start handle after a trim");
        vm.Playhead = 290;
        Assert.True(vm.ShowPlayhead, "moved by a click on the strip");
    }

    [Test]
    public static void The_end_of_an_old_playback_does_not_stop_a_newer_one()
    {
        var (vm, host) = OpenCut();
        vm.PlayCommand.Execute(null);
        var oldEnded = host.Ended!;
        vm.Playhead = 290;
        oldEnded(null);
        Assert.True(vm.IsPlaying, "the newer playback goes on");
    }

    [Test]
    public static void Edit_opens_the_editor_on_the_shared_clip()
    {
        Text.Language = "en";
        var vm = new ShareViewModel(new FakeHost(), () => new ShareSettings());
        vm.Open(Clip);
        string? asked = null;
        vm.EditRequested += path => asked = path;
        vm.EditCommand.Execute(null);
        Assert.Equal(Clip, asked, "Edit opens the editor on this clip");
    }

    /// <summary>
    /// An edit saved from the share panel's editor (a real clip: a new copy, or the original replaced) is what the panel
    /// shares from then on, and it is never removed: not when the panel closes, not when it moves on to another clip.
    /// </summary>
    [Test]
    public static void A_saved_edit_is_shared_and_kept()
    {
        Text.Language = "en";
        var vm = new ShareViewModel(new FakeHost(), () => new ShareSettings());
        vm.Open(Clip);
        string saved = Path.Combine(Path.GetTempPath(), "kairos-saved-" + Guid.NewGuid().ToString("N") + ".mp4");
        File.WriteAllText(saved, "edit");
        try
        {
            Assert.True(vm.UseSaved(saved, Clip), "taken");
            Assert.Equal(saved, vm.ClipPath, "the panel shares the saved edit");
            Assert.Equal("ELDEN RING", vm.Game, "still named after the game");
            vm.Close();
            Assert.True(File.Exists(saved), "a saved edit stays");
            Assert.True(!vm.UseSaved(saved, Clip) && File.Exists(saved), "too late (the panel closed): not shown, still kept");
        }
        finally
        {
            File.Delete(saved);
        }
    }

    /// <summary>
    /// A share edit is saved after the user closed the panel or moved on to another clip: it is not shown (the panel does
    /// not pop open, and another clip is never swapped for it), and the saved clip stays.
    /// </summary>
    [Test]
    public static void A_saved_edit_that_finishes_too_late_is_not_shown()
    {
        Text.Language = "en";
        var vm = new ShareViewModel(new FakeHost(), () => new ShareSettings());
        string saved = Path.Combine(Path.GetTempPath(), "kairos-saved-" + Guid.NewGuid().ToString("N") + ".mp4");
        File.WriteAllText(saved, "edit");
        try
        {
            vm.Open(Clip);
            vm.Close();
            Assert.True(!vm.UseSaved(saved, Clip), "refused");
            Assert.True(!vm.IsOpen, "the panel stays closed");

            const string other = @"D:\Clips\ELDEN RING\ELDEN RING 2026-10-12 22-10-00.mp4";
            vm.Open(other);
            Assert.True(!vm.UseSaved(saved, Clip), "refused");
            Assert.Equal(other, vm.ClipPath, "the other clip stays");
            Assert.True(File.Exists(saved), "the saved clip is the user's: kept");
        }
        finally
        {
            File.Delete(saved);
        }
    }
    sealed class ThrowingProbe(FakeHost inner) : IShareHost
    {
        public MediaInfo Probe(string clip) => throw new ShareException("The clip was not found: " + clip);
        public SharePicture? ReadPicture(string path) => inner.ReadPicture(path);
        public ShareResult MakeJpeg(string source, string target, long maxBytes) => inner.MakeJpeg(source, target, maxBytes);
        public void CopyPicture(string path) => inner.CopyPicture(path);
        public ShareResult Transcode(string source, string target, SharePlan plan, long maxBytes, IProgress<double> progress, CancellationToken ct) =>
            inner.Transcode(source, target, plan, maxBytes, progress, ct);
        public ShareResult Cut(string source, string target, double start, double end, IProgress<double> progress, CancellationToken ct) =>
            inner.Cut(source, target, start, end, progress, ct);
        public void CopyToClipboard(string? file, string? text) => inner.CopyToClipboard(file, text);
        public void GrabFrames(string clip, double[] times, int width, bool exact, Action<int, System.Windows.Media.Imaging.BitmapSource?> done) { }
        public void ReleaseFrames() { }
        public System.Windows.Media.Imaging.BitmapSource? Play(string clip, double from, double to, int width, int height,
            Action<double> position, Action<string?> ended) => null;
        public void StopPlaying() { }
        public void SetPlaybackVolume(double volume) { }
        public void After(TimeSpan delay, Action action) { }
        public void ShowInFolder(string path) { }
        public void Remember(ShareSettings prefs) { }
        public void Post(Action action) => action();
        public void Run(Action work) => work();
        public string Folder => inner.Folder;
    }
}
