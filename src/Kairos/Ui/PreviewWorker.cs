namespace Kairos.Ui;

/// <summary>Decode work and its resources outlive Stop until the worker itself has returned.</summary>
internal sealed class PreviewWorker(IPreviewFrameFilter? filter, Action<PreviewWorker> decode)
{
    readonly object _gate = new();
    readonly AutoResetEvent _room = new(false);
    readonly CancellationTokenSource _cancel = new();
    public CancellationToken Token => _cancel.Token;
    Thread? _thread;
    bool _ended;
    volatile bool _stopping;
    public IPreviewFrameFilter? Filter => filter;
    public bool Stopping => _stopping;
    public Exception? Failure { get; private set; }
    public void Start()
    {
        _thread = new Thread(Run) { IsBackground = true, Name = "Replay Local preview", Priority = ThreadPriority.AboveNormal };
        _thread.Start();
    }
    void Run()
    {
        try { decode(this); }
        catch (Exception e) { Failure = e; }
        finally
        {
            try { filter?.Dispose(); }
            finally { lock (_gate) { _ended = true; _room.Dispose(); _cancel.Dispose(); } }
        }
    }
    public void Wait(int milliseconds) => _room.WaitOne(milliseconds);
    public void Wake() { lock (_gate) { if (!_ended) _room.Set(); } }
    public void Stop() { _stopping = true; lock(_gate) { if(!_ended) _cancel.Cancel(); } Wake(); }
    internal bool WaitForExit(int milliseconds) => _thread?.Join(milliseconds) ?? true;
}
