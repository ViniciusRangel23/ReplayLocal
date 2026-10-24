using Kairos.Engine.Audio;
using Kairos.Engine.Edit;

namespace Kairos.Tests;

public static class AudioEditTests
{
    public static AudioLayout Layout => new(1,[new("mix",1,"Mistura",AudioRole.Mix),new("game",2,"Jogo",AudioRole.Application),new("discord",3,"Discord",AudioRole.Application),new("spotify",4,"Spotify",AudioRole.Application)]);
    [Test]
    public static void Per_source_gains_and_solo_never_duplicate_mix()
    {
        var p = EditProject.For("fixture.mp4", 4) with { Audio = AudioEdit.Create(Layout) };
        p = p with { Audio = p.Audio! with { Levels = [new("game",0.5),new("discord",1,true),new("spotify",2)] } };
        var part = p.Kept[0] with { AudioGains = new Dictionary<string,double> { ["spotify"] = 0.5 } };
        var gains = AudioEdit.ResolveGains(p,part);
        Assert.Equal(3,gains.Count); Assert.True(!gains.ContainsKey("mix"),"no double mixing");
        Assert.Equal(0.5,gains["game"]); Assert.Equal(0d,gains["discord"]); Assert.Equal(1d,gains["spotify"]);
        var solo = AudioEdit.ResolveGains(p,part,new HashSet<string> { "spotify" });
        Assert.Equal(0d,solo["game"]); Assert.Equal(1d,solo["spotify"]);
        Assert.Equal(0.5,AudioEdit.ResolveGains(p,part)["game"],"solo does not modify project");
    }
    [Test]
    public static void Split_and_speed_keep_source_ids_and_part_gains()
    {
        var p=EditProject.For("fixture.mp4",4) with { Audio=AudioEdit.Create(Layout) };
        p=p with { Kept=[p.Kept[0] with { AudioGains=new Dictionary<string,double>{{"discord",0.4}} }] };
        p=p.SplitAt(2).SetSpeed(0,2,2);
        Assert.Equal(2,p.Kept.Count);
        Assert.True(p.Kept.All(k=>k.AudioGains!["discord"]==0.4),"gains survive split");
    }
    [Test]
    public static void Generic_streams_only_include_first_until_explicitly_selected()
    {
        var layout = Layout with { Tracks=Layout.Tracks.Take(2).Select(t=>t with {Role=AudioRole.Generic}).ToArray() };
        var state=AudioEdit.Create(layout);
        Assert.True(state.Levels[0].Included && !state.Levels[1].Included,"unknown tracks not auto-summed");
    }
}
