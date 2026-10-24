using System.Security.Cryptography;
using Kairos.Engine.Audio;

namespace Kairos.Tests;

public static class AudioSaveFinalizerTests
{
    static string CopyFixture()
    {
        string dir = Path.Combine(Path.GetTempPath(), "replay-finalize-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "saved.mp4");
        File.Copy(Path.Combine(AppContext.BaseDirectory,"Fixtures","Share","clip-av1-2tracks.mp4"),path);
        return path;
    }
    [Test]
    public static void Finalized_file_retains_layout_and_packets()
    {
        TestFfmpeg.Load(); string path = CopyFixture();
        string hash = AudioContainer.PacketDigest(path);
        var layout = new AudioLayout(1,[new("mix",1,"Mistura",AudioRole.Mix),new("game",2,"Jogo",AudioRole.Application)]);
        Assert.Equal(path, AudioSaveFinalizer.FinalizeAsync(path,layout,default).GetAwaiter().GetResult());
        Assert.Equal("game",AudioContainer.Probe(path).Layout!.Tracks[1].Id);
        Assert.Equal(hash, AudioContainer.PacketDigest(path));
        Assert.Equal(1,Directory.GetFiles(Path.GetDirectoryName(path)!).Length);
    }
    [Test]
    public static void Invalid_layout_and_cancellation_preserve_original()
    {
        TestFfmpeg.Load(); string path = CopyFixture();
        string hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
        foreach(var ct in new[] { CancellationToken.None, new CancellationToken(true) })
        {
            bool failed = false;
            try { AudioSaveFinalizer.FinalizeAsync(path,new(1,[new("mix",1,"Mix",AudioRole.Mix)]),ct).GetAwaiter().GetResult(); }
            catch(Exception e) when(e is OperationCanceledException or ArgumentException) { failed=true; }
            Assert.True(failed,"invalid operation rejected");
            Assert.Equal(hash,Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))));
        }
        Assert.Equal(1,Directory.GetFiles(Path.GetDirectoryName(path)!).Length);
    }
}
