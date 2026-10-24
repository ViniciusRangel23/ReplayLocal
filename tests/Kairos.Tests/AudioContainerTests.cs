using System.Security.Cryptography;
using Kairos.Engine.Audio;
using Kairos.Engine.Share;

namespace Kairos.Tests;

public static class AudioContainerTests
{
    [Test]
    public static void RoundTrip_survives_mp4_remux()
    {
        TestFfmpeg.Load();
        string source = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Share", "clip-av1-2tracks.mp4");
        var probe = AudioContainer.Probe(source);
        var layout = new AudioLayout(1, probe.Streams.Select((t, i) => t with { Id = "track-" + i, Name = i == 0 ? "Mistura" : "Discord", Role = i == 0 ? AudioRole.Mix : AudioRole.Application }).ToArray());
        string dir = Path.Combine(Path.GetTempPath(), "replay-audio-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        string target = Path.Combine(dir, "tagged.mp4");
        string before = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(source)));
        AudioContainer.RemuxWithLayout(source, target, layout, default);
        var read = AudioContainer.Probe(target);
        Assert.True(read.Layout != null, "layout survives MP4 muxer");
        Assert.Equal("Discord", read.Layout!.Tracks[1].Name);
        Assert.Equal(layout.Tracks[1].StreamIndex, read.Layout.Tracks[1].StreamIndex);
        Assert.Equal(ClipTranscoder.Probe(source).VideoCodec, ClipTranscoder.Probe(target).VideoCodec);
        Assert.Equal(AudioContainer.PacketDigest(source), AudioContainer.PacketDigest(target), "all encoded payloads preserved");
        Assert.Equal(before, Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(source))), "original intact");
        Assert.True(AudioContainer.DefaultAudioIndexes(target).SequenceEqual(new[] { layout.Tracks[0].StreamIndex }), "only mix is default");
    }

    [Test]
    public static void Cancelled_remux_preserves_source_and_leaves_no_target()
    {
        TestFfmpeg.Load();
        string source = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Share", "clip-h264.mp4");
        var probe = AudioContainer.Probe(source);
        string target = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".mp4");
        var cancel = new CancellationToken(true);
        bool cancelled = false;
        try { AudioContainer.RemuxWithLayout(source, target, new(1, probe.Streams), cancel); }
        catch (OperationCanceledException) { cancelled = true; }
        Assert.True(cancelled && !File.Exists(target) && File.Exists(source), "no partial output");
    }
}
