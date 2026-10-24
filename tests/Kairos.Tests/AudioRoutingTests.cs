using Kairos.Engine.Audio;
using Kairos.Settings;

namespace Kairos.Tests;

public static class AudioRoutingTests
{
    [Test]
    public static void Sources_only_reach_mix_and_own_track()
    {
        var s = ApplicationAudioSettingsTests.Config(4);
        s.Audio.SelectMic("specific-device");
        var routes = AudioRoutingPlan.Build(s);
        Assert.Equal(5, routes.Count);
        Assert.True(routes.Select(r => r.SourceId).Distinct().Count() == 5, "unique routes");
        foreach (var r in routes) Assert.Equal(1u | (1u << r.MixerIndex), r.MixerMask);
        Assert.Equal(5, routes.Last().MixerIndex);
        Assert.True(!routes.Any(r => r.SourceId is "system" or "game"), "no global or attached duplicate");
    }
    [Test]
    public static void Disabled_application_preserves_slot_and_mic_needs_consent()
    {
        var s = ApplicationAudioSettingsTests.Config();
        s.Audio.Applications[0].Enabled = false;
        s.Audio.Mic = true;
        var routes = AudioRoutingPlan.Build(s);
        Assert.Equal(3, routes.Count);
        Assert.Equal(1, routes[0].MixerIndex);
        Assert.Equal(0u, routes[0].MixerMask);
        Assert.Equal(2, routes[1].MixerIndex);
    }
}
