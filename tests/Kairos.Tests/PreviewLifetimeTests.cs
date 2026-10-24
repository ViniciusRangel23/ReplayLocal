using Kairos.Ui;
using System.Diagnostics;
namespace Kairos.Tests;
public static class PreviewLifetimeTests
{
    sealed class Filter : IPreviewFrameFilter
    {
        public readonly ManualResetEventSlim Entered = new(), Release = new();
        public int Disposals;
        public void Apply(byte[] pixels, double time) { Entered.Set(); Release.Wait(); }
        public void Dispose() => Interlocked.Increment(ref Disposals);
    }
    [Test] public static void Stop_does_not_dispose_filter_while_apply_is_blocked()
    {
        var f = new Filter();
        var worker = new PreviewWorker(f, w => w.Filter!.Apply([], 0));
        worker.Start();
        try
        {
            Assert.True(f.Entered.Wait(2000), "decode entered filter");
            var clock = Stopwatch.StartNew(); worker.Stop();
            Assert.True(clock.ElapsedMilliseconds < 500, "close does not wait for decoder");
            Assert.True(!worker.WaitForExit(2200), "old decode can outlive former two-second timeout");
            Assert.Equal(0, f.Disposals, "filter remains owned by blocked worker");
        }
        finally { f.Release.Set(); }
        Assert.True(worker.WaitForExit(2000), "worker ends when work releases");
        Assert.Equal(1, f.Disposals, "exactly one disposal on decode thread");
    }
    [Test] public static void Restart_and_failure_dispose_separate_filters_once()
    {
        var a = new Filter(); var b = new Filter();
        var old = new PreviewWorker(a, w => w.Filter!.Apply([], 0)); old.Start();
        Assert.True(a.Entered.Wait(2000), "old generation entered"); old.Stop();
        var next = new PreviewWorker(b, _ => throw new InvalidOperationException("synthetic decode failure")); next.Start();
        Assert.True(next.WaitForExit(2000), "failure exits");
        Assert.Equal(1, b.Disposals, "new generation cleaned");
        Assert.Equal(0, a.Disposals, "new generation does not dispose old one");
        a.Release.Set(); Assert.True(old.WaitForExit(2000), "old generation cleans independently");
        Assert.Equal(1, a.Disposals, "old filter disposed once");
        Assert.True(next.Failure is InvalidOperationException, "failure recorded");
    }
}
