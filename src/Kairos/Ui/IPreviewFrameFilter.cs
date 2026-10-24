namespace Kairos.Ui;

/// <summary>A single playback generation owns this filter and disposes it on its decode thread.</summary>
public interface IPreviewFrameFilter : IDisposable
{
    void Apply(byte[] pixels, double time);
}

internal sealed class DelegateFrameFilter(Action<byte[], double> apply) : IPreviewFrameFilter
{
    public void Apply(byte[] pixels, double time) => apply(pixels, time);
    public void Dispose() { }
}
