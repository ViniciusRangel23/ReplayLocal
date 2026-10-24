// Replay Local: modified 2026-10-23; independent fork of BasilForge/Kairos. See MODIFICATIONS.md. GPL-2.0-or-later.
using Kairos.Engine.Share;

namespace Kairos.Tests;

/// <summary>The FFmpeg 8 DLLs the tests run on: OBS's own, or the installed Kairos's copy of them.</summary>
static class TestFfmpeg
{
    /// <summary>The folder with the FFmpeg 8 DLLs (OBS's, else the installed Kairos's), or null.</summary>
    public static string? Folder => new[]
        {
            Environment.GetEnvironmentVariable("KAIROS_FFMPEG") ?? "",
            @"C:\Program Files\obs-studio\bin\64bit",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Kairos", "current"),
        }
        .FirstOrDefault(d => File.Exists(Path.Combine(d, "avcodec-62.dll")));

    public static void Load()
    {
        string? dir = Folder;
        Assert.True(dir != null, "FFmpeg 8 DLLs from OBS not found");
        ClipTranscoder.Load(dir!);
    }
}
