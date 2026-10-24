using System.Text.Json;
using Kairos.Engine.Audio;

namespace Kairos.Tests;

public static class AudioLayoutTests
{
    static AudioLayout Valid() => new(1, [new("mix", 1, "Mistura", AudioRole.Mix), new("game", 2, "Jogo", AudioRole.Application)]);

    [Test]
    public static void Layout_round_trips_and_uses_real_indexes()
    {
        string json = JsonSerializer.Serialize(Valid());
        var read = AudioLayoutMetadata.TryRead(json, [1, 2]);
        Assert.True(read != null, "valid metadata");
        Assert.Equal("game", read!.Tracks[1].Id);
        Assert.Equal(2, read.Tracks[1].StreamIndex);
    }

    [Test]
    public static void Rejects_invalid_layout()
    {
        var valid = Valid();
        var invalid = new[] {
            valid with { Version = 9 },
            valid with { Tracks = [valid.Tracks[0], valid.Tracks[1] with { Id = "mix" }] },
            valid with { Tracks = [valid.Tracks[0], valid.Tracks[1] with { StreamIndex = 1 }] },
            valid with { Tracks = [valid.Tracks[0], valid.Tracks[1] with { StreamIndex = 0 }] },
            valid with { Tracks = [valid.Tracks[0], valid.Tracks[1] with { Name = new string('a', 81) }] },
            valid with { Tracks = Enumerable.Range(0, 7).Select(i => new AudioTrackInfo("id" + i, i + 1, "Faixa", AudioRole.Generic)).ToArray() },
            valid with { Tracks = [valid.Tracks[0], valid.Tracks[1] with { Role = AudioRole.Mix }] }
        };
        foreach (var item in invalid)
            Assert.True(AudioLayoutMetadata.TryRead(JsonSerializer.Serialize(item), item.Tracks.Count == 7 ? [1, 2, 3, 4, 5, 6, 7] : [1, 2]) == null, "invalid layout rejected");
        foreach (string text in new[] { "{", "null", new string(' ', 16385) })
            Assert.True(AudioLayoutMetadata.TryRead(text, [1, 2]) == null, "bad or oversized metadata rejected");
    }
}
