namespace Kairos.Engine.Audio;

public static class AudioSaveFinalizer
{
    static readonly SemaphoreSlim Gate = new(1);
    public static async Task<string> FinalizeAsync(string path, AudioLayout sessionLayout, CancellationToken ct)
    {
        // Snapshot before awaiting: callers can dispose/rebuild their pipeline while an older file finishes.
        var layout = new AudioLayout(sessionLayout.Version, sessionLayout.Tracks.ToArray());
        await Gate.WaitAsync(ct).ConfigureAwait(false);
        string temp = path + "." + Guid.NewGuid().ToString("N") + ".audio.part";
        try
        {
            await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                var probe = AudioContainer.Probe(path);
                if (probe.Streams.Count != layout.Tracks.Count) throw new ArgumentException("Recorded audio tracks do not match the session.");
                var actual = layout with { Tracks = layout.Tracks.Select((t,i) => t with { StreamIndex = probe.Streams[i].StreamIndex }).ToArray() };
                AudioContainer.RemuxWithLayout(path, temp, actual, ct);
                var read = AudioContainer.Probe(temp);
                if (read.Layout == null || !read.Layout.Tracks.Select(t => t.Id).SequenceEqual(layout.Tracks.Select(t => t.Id)))
                    throw new IOException("Audio metadata could not be verified; original retained.");
                ct.ThrowIfCancellationRequested();
                File.Replace(temp, path, null);
            }, ct).ConfigureAwait(false);
            return path;
        }
        finally
        {
            try { if (File.Exists(temp)) File.Delete(temp); }
            finally { Gate.Release(); }
        }
    }
}
