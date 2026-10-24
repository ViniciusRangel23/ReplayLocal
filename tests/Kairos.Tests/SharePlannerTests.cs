// Replay Local: modified 2026-10-23; independent fork of BasilForge/Kairos. See MODIFICATIONS.md. GPL-2.0-or-later.
using Kairos.Engine.Share;

namespace Kairos.Tests;

public static class SharePlannerTests
{
    static string Steps(IEnumerable<SizeStep> steps) => string.Join(" ", steps.Select(s => s.Original ? $"[{s.ShortSide}]" : s.ShortSide.ToString()));
    /// <summary>A size limit like the one X has, on a copy (sizes and frame rate under a limit).</summary>
    static readonly ShareSpec Limited20Mb = new(SharePlatform.Copy, 20_000_000, double.PositiveInfinity, 2160, 60);

    [Test]
    public static void The_cut_opens_as_long_as_the_platform_takes_ending_at_the_end_of_the_clip()
    {
        Assert.Equal((0.0, 300.0), SharePlanner.DefaultTrim(300, double.PositiveInfinity), "the whole clip");
        Assert.Equal((160.0, 300.0), SharePlanner.DefaultTrim(300, 140), "X: the last 2:20");
        Assert.Equal((0.0, 12.0), SharePlanner.DefaultTrim(12, 140), "a short clip stays whole");
    }

    [Test]
    public static void A_trim_stays_inside_the_clip_and_the_platform_limit()
    {
        Assert.Equal((0.0, 300.0), SharePlanner.ClampTrim(-5, 400, 300, double.PositiveInfinity), "inside the clip");
        Assert.Equal((160.0, 300.0), SharePlanner.ClampTrim(0, 300, 300, 140), "X: the end is kept, the start follows");
        Assert.Equal((100.0, 101.0), SharePlanner.ClampTrim(100, 100.2, 300, double.PositiveInfinity), "at least one second");
        Assert.Equal((299.0, 300.0), SharePlanner.ClampTrim(300, 300, 300, double.PositiveInfinity), "one second even at the very end");
    }

    [Test]
    public static void The_sizes_tried_are_smaller_ones_the_original_and_up_to_the_platforms_top()
    {
        Assert.Equal("480 720 1080 [1440] 2160", Steps(SharePlanner.SizeSteps(1440, 2160)), "copy of a 1440p clip: down to 480, up to 4K");
        Assert.Equal("480 720 [1080] 1440 2160", Steps(SharePlanner.SizeSteps(1080, 2160)), "copy of a 1080p clip");
        Assert.Equal("480 720 1080 1440 [2160]", Steps(SharePlanner.SizeSteps(2160, 2160)), "a 4K clip is already the top");
        Assert.Equal("480 720 1080", Steps(SharePlanner.SizeSteps(1440, 1080)), "X shows 1080p at most, so 1440p is not offered");
        Assert.Equal("480 720 [810] 1080", Steps(SharePlanner.SizeSteps(810, 1080)), "a 9:16 cut of 1440p can go up to 1080x1920");
        Assert.Equal("[360] 480 720 1080", Steps(SharePlanner.SizeSteps(360, 1080)), "a tiny clip");
    }

    [Test]
    public static void Landscape_scales_to_the_chosen_size_up_or_down_and_keeps_even_sizes()
    {
        var f = SharePlanner.Frame(2560, 1440, vertical: false, cropPosition: 0.5, shortSide: 1080);
        Assert.Equal((1920, 1080), (f.Width, f.Height), "1440p scaled to 1080p");
        Assert.True(f.Crop == null, "no crop");
        var up = SharePlanner.Frame(2560, 1440, vertical: false, cropPosition: 0.5, shortSide: 2160);
        Assert.Equal((3840, 2160), (up.Width, up.Height), "1440p upscaled to 4K");
        var original = SharePlanner.Frame(2560, 1440, vertical: false, cropPosition: 0.5, shortSide: 0);
        Assert.Equal((2560, 1440), (original.Width, original.Height), "0 keeps the clip's own size");
        var odd = SharePlanner.Frame(1366, 768, vertical: false, cropPosition: 0.5, shortSide: 720);
        Assert.True(odd.Width % 2 == 0 && odd.Height % 2 == 0, $"even {odd.Width}x{odd.Height}");
    }

    [Test]
    public static void Vertical_cuts_a_nine_by_sixteen_window_where_the_user_slides_it()
    {
        var center = SharePlanner.Frame(2560, 1440, vertical: true, cropPosition: 0.5, shortSide: 0);
        Assert.Equal(new CropRect(874, 0, 810, 1440), center.Crop, "centred 9:16 window");
        Assert.Equal((810, 1440), (center.Width, center.Height), "the cut's own size");
        var full = SharePlanner.Frame(2560, 1440, vertical: true, cropPosition: 0.5, shortSide: 1080);
        Assert.Equal((1080, 1920), (full.Width, full.Height), "up to the sites' 1080x1920");
        var left = SharePlanner.Frame(2560, 1440, vertical: true, cropPosition: 0, shortSide: 0);
        Assert.Equal(0, left.Crop!.X, "left edge");
        var right = SharePlanner.Frame(1920, 1080, vertical: true, cropPosition: 1, shortSide: 0);
        Assert.Equal(1920 - 606, right.Crop!.X, "right edge");
        Assert.Equal((606, 1080), (right.Width, right.Height), "1080p source");
    }

    [Test]
    public static void A_twenty_megabyte_copy_fits_its_limit()
    {
        var spec = Limited20Mb;
        var size = SharePlanner.DefaultSize(spec, 30, 2560, 1440, 60, 192_000, vertical: false);
        Assert.Equal(new SizeStep(1080, false), size, "1440p would turn to blocks in 20 MB, 1080p does not");
        var plan = SharePlanner.Plan(spec, start: 270, duration: 30, srcWidth: 2560, srcHeight: 1440, srcFps: 60, audioBitrate: 192_000,
            vertical: false, cropPosition: 0.5, shortSide: size.ShortSide);
        double bytes = (plan.VideoBitrate + 192_000) * plan.Duration / 8;
        Assert.True(bytes <= 20_000_000 * 0.95, $"estimated {bytes / 1e6:F1} MB");
        Assert.True(plan.VideoBitrate > 3_000_000, $"still good quality at {plan.VideoBitrate / 1e6:F1} Mb/s");
        Assert.Equal((1920, 1080), (plan.Width, plan.Height), "1080p");
    }

    [Test]
    public static void Caption_tags_the_game_and_keeps_the_users_hashtags()
    {
        Assert.Equal("#EldenRing #gaming", SharePlanner.Caption("ELDEN RING™", "#gaming"), "game tag");
        Assert.Equal("#لعبة_تجريبية #clips", SharePlanner.Caption("لعبة تجريبية", "#clips"), "Arabic game");
        Assert.Equal("#gaming", SharePlanner.Caption("Desktop", "#gaming"), "no tag for the desktop");
        Assert.Equal("#Valorant", SharePlanner.Caption("Valorant", ""), "no saved tags");
    }

    [Test]
    public static void The_game_name_comes_from_the_clip_file_name()
    {
        Assert.Equal("ELDEN RING", SharePlanner.GameFromFile(@"D:\Clips\ELDEN RING\ELDEN RING 2026-10-12 21-04-55.mp4"), "per-app folder");
        Assert.Equal("Valorant", SharePlanner.GameFromFile(@"D:\Clips\Valorant 2026-10-12 21-04-55 (2).mp4"), "numbered copy");
        Assert.Equal("my clip", SharePlanner.GameFromFile(@"D:\Clips\my clip.mp4"), "renamed by the user");
    }

    [Test]
    public static void Only_the_users_own_hashtags_are_remembered_for_next_time()
    {
        Assert.Equal("#gaming #fps", SharePlanner.RememberTags("What a clutch #Valorant #gaming #fps", "Valorant"), "game tag and words dropped");
        Assert.Equal("", SharePlanner.RememberTags("no tags here", "Valorant"), "nothing to keep");
        Assert.Equal("#gaming", SharePlanner.RememberTags("#gaming #gaming", "Desktop"), "no duplicates");
    }
}
