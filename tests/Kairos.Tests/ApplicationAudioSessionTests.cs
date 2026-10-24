using Kairos.Engine.Audio;

namespace Kairos.Tests;

public static class ApplicationAudioSessionTests
{
    sealed class Backend : IApplicationAudioBackend
    {
        public int Created, Released;
        public Action? OnCreate;
        public IDisposable Create(string id, string token, uint mask, double volume) { Created++; OnCreate?.Invoke(); return new Resource(() => Released++); }
    }
    sealed class Resource(Action release) : IDisposable { public void Dispose() => release(); }

    [Test]
    public static void Only_selected_sources_are_owned_and_released()
    {
        var s = ApplicationAudioSettingsTests.Config();
        var backend = new Backend();
        var session = new ApplicationAudioSession(s.Audio, backend);
        var targets = s.Audio.Applications.Select(a => new AudioTargetCandidate(a.Target, a.Id)).ToArray();
        session.Refresh(targets);
        Assert.Equal(3, backend.Created);
        session.Refresh(targets);
        Assert.Equal(3, backend.Created, "unchanged sources not recreated");
        session.Refresh(targets.Skip(1).ToArray());
        Assert.Equal(1, backend.Released);
        Assert.Equal("unavailable", session.Status[0].State);
        session.Dispose(); session.Dispose(); session.Refresh(targets);
        Assert.Equal(3, backend.Released);
        Assert.Equal(3, backend.Created);
    }
    [Test]
    public static void Disable_during_create_revokes_late_result()
    {
        var s = ApplicationAudioSettingsTests.Config(1);
        var backend = new Backend();
        var session = new ApplicationAudioSession(s.Audio, backend);
        backend.OnCreate = session.Dispose;
        session.Refresh([new(s.Audio.Applications[0].Target, "token")]);
        Assert.Equal(1, backend.Released);
        session.Refresh([new(s.Audio.Applications[0].Target, "token")]);
        Assert.Equal(1, backend.Created);
    }
    [Test]
    public static void Disabled_or_ambiguous_source_never_opens_capture()
    {
        var s = ApplicationAudioSettingsTests.Config(2);
        s.Audio.Applications[0].Enabled = false;
        var backend = new Backend();
        using var session = new ApplicationAudioSession(s.Audio, backend);
        session.Refresh([new(s.Audio.Applications[0].Target, "a"),new(s.Audio.Applications[1].Target,"b"),new(s.Audio.Applications[1].Target,"c")]);
        Assert.Equal(0, backend.Created);
    }
    [Test]
    public static void Same_token_in_restarted_process_recreates_capture()
    {
        var settings=ApplicationAudioSettingsTests.Config(1);var backend=new Backend();
        using var session=new ApplicationAudioSession(settings.Audio,backend);
        session.Refresh([new(settings.Audio.Applications[0].Target,"same-token",100)]);
        session.Refresh([new(settings.Audio.Applications[0].Target,"same-token",101)]);
        Assert.Equal(2,backend.Created,"PID replacement cannot reuse old capture");
        Assert.Equal(1,backend.Released);
    }
}
