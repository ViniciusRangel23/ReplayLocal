namespace Kairos.Engine.Audio;

public enum AudioRole { Mix, Application, Microphone, Generic }
public sealed record AudioTrackInfo(string Id, int StreamIndex, string Name, AudioRole Role);
public sealed record AudioLayout(int Version, IReadOnlyList<AudioTrackInfo> Tracks);
public sealed record AudioProbe(IReadOnlyList<AudioTrackInfo> Streams, AudioLayout? Layout);
