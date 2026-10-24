namespace Kairos.App;

public interface ICaptureSession : IDisposable
{
    bool Recording { get; }
    void StopRecording();
    void SetReplayActive(bool active);
}

/// <summary>Owns capture sources. Pausing replay keeps them; disabling releases them.</summary>
public sealed class CaptureController(Func<ICaptureSession> create, Action suspendVoice)
{
    public ICaptureSession? Session { get; private set; }
    public void Enable() => Session ??= create();
    public void SetReplayPaused(bool paused) => Session?.SetReplayActive(!paused);
    public void Disable()
    {
        suspendVoice();
        var session = Session;
        if (session == null) return;
        session.StopRecording();
        session.Dispose();
        Session = null;
    }
}
