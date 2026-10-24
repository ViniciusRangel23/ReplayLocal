// Replay Local: modified 2026-10-23; independent fork of BasilForge/Kairos. See MODIFICATIONS.md. GPL-2.0-or-later.
using System.Globalization;
using Kairos.App;

namespace Kairos.Ui;

/// <summary>What the Status page says: the instant replay card and the recording card.</summary>
public static class StatusLogic
{
    /// <summary>The instant replay card: offline | error | starting | paused | off | waiting | active. A recording has its own card.</summary>
    public static string ReplayState(StatusDto? status, bool replayEnabled)
    {
        if (status == null)
            return "offline";
        var e = status.Engine;
        if (status.Error != null && e?.BufferActive != true && e?.Recording != true)
            return "error";
        if (status.CaptureEnabled == false) return "captureOff";
        if (e == null)
            return "starting";
        if (!replayEnabled)
            return "off"; // a plain recorder: nothing kept in the background
        if (status.PausedByUser)
            return "paused";
        return status.WaitingForGame ? "waiting" : "active";
    }

    /// <summary>The line under the instant replay title: only what is wrong, or how to turn it on (the hotkeys sit in the buttons).</summary>
    public static string ReplayDetail(string state, string? error) => state switch
    {
        "error" => error ?? "",
        "captureOff" => Text.Get("status.captureOffHint"),
        "off" => Text.Get("status.offHint"),
        _ => "",
    };

    /// <summary>The recording card: idle | recording | paused.</summary>
    public static string RecordState(StatusDto? status) => status?.Engine is { Recording: true } e
        ? e.RecordingPaused ? "paused" : "recording"
        : "idle";

    /// <summary>"12:34 • 1.2 GB": how long the recording is (without its pauses) and how big so far.</summary>
    public static string RecordingDetail(double seconds, long bytes) =>
        Text.Ltr($"{Text.Duration((int)seconds)} • {Size(bytes)}");

    public static string Size(long bytes) => bytes >= 1L << 30
        ? (bytes / (double)(1L << 30)).ToString("0.0", CultureInfo.InvariantCulture) + " GB"
        : $"{bytes >> 20} MB";
}
