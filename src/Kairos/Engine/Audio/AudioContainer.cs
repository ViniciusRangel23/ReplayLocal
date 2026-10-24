using System.Runtime.InteropServices;
using System.Security.Cryptography;
using FFmpeg.AutoGen;
using Kairos.Engine.Share;

namespace Kairos.Engine.Audio;

public static unsafe class AudioContainer
{
    static AVFormatContext* Open(string path)
    {
        ClipTranscoder.RequireLoaded();
        AVFormatContext* input = null;
        ClipTranscoder.Check(ffmpeg.avformat_open_input(&input, path, null, null), "Opening audio tracks");
        try { ClipTranscoder.Check(ffmpeg.avformat_find_stream_info(input, null), "Reading audio tracks"); return input; }
        catch { ffmpeg.avformat_close_input(&input); throw; }
    }

    static string Value(AVDictionary* dictionary, string key)
    {
        AVDictionaryEntry* entry = ffmpeg.av_dict_get(dictionary, key, null, 0);
        return entry == null ? "" : Marshal.PtrToStringUTF8((IntPtr)entry->value) ?? "";
    }

    public static AudioProbe Probe(string path)
    {
        var input = Open(path);
        try
        {
            var tracks = new List<AudioTrackInfo>();
            for (int i = 0; i < input->nb_streams; i++)
                if (input->streams[i]->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO)
                {
                    string name = Value(input->streams[i]->metadata, "handler_name");
                    if (name.Length == 0 || name.Length > 80 || name.Any(char.IsControl)) name = $"Faixa {tracks.Count + 1}";
                    tracks.Add(new("audio-" + i, i, name, AudioRole.Generic));
                }
            string comment = Value(input->metadata, "comment");
            var layout = comment.StartsWith(AudioLayoutMetadata.Prefix, StringComparison.Ordinal)
                ? AudioLayoutMetadata.TryRead(comment[AudioLayoutMetadata.Prefix.Length..], tracks.Select(t => t.StreamIndex).ToArray()) : null;
            return new(tracks, layout);
        }
        finally { ffmpeg.avformat_close_input(&input); }
    }

    public static void RemuxWithLayout(string source, string target, AudioLayout layout, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (Path.GetFullPath(source).Equals(Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Remux requires a different output file.");
        if (File.Exists(target)) throw new IOException("Output already exists.");
        var probe = Probe(source);
        if (AudioLayoutMetadata.TryRead(System.Text.Json.JsonSerializer.Serialize(layout), probe.Streams.Select(t => t.StreamIndex).ToArray()) == null)
            throw new ArgumentException("Audio layout does not match the source.");
        var input = Open(source);
        AVFormatContext* output = null;
        AVPacket* packet = null;
        bool complete = false, created = false;
        try
        {
            ClipTranscoder.Check(ffmpeg.avformat_alloc_output_context2(&output, null, "mp4", target), "Preparing audio metadata output");
            ffmpeg.av_dict_copy(&output->metadata, input->metadata, 0);
            ffmpeg.av_dict_set(&output->metadata, "comment", AudioLayoutMetadata.Write(layout), 0);
            for (int i = 0; i < input->nb_streams; i++)
            {
                AVStream* s = ffmpeg.avformat_new_stream(output, null);
                if (s == null) throw new OutOfMemoryException();
                ClipTranscoder.Check(ffmpeg.avcodec_parameters_copy(s->codecpar, input->streams[i]->codecpar), "Copying audio parameters");
                s->codecpar->codec_tag = 0;
                s->time_base = input->streams[i]->time_base;
                s->disposition = input->streams[i]->disposition;
                ffmpeg.av_dict_copy(&s->metadata, input->streams[i]->metadata, 0);
                if (layout.Tracks.FirstOrDefault(t => t.StreamIndex == i) is { } t)
                {
                    s->disposition &= ~ffmpeg.AV_DISPOSITION_DEFAULT;
                    if (t.Role == AudioRole.Mix || (!layout.Tracks.Any(x => x.Role == AudioRole.Mix) && t == layout.Tracks[0]))
                        s->disposition |= ffmpeg.AV_DISPOSITION_DEFAULT;
                    ffmpeg.av_dict_set(&s->metadata, "handler_name", t.Name, 0);
                }
            }
            // Reserve without overwriting an unrelated file. avio opens this file only after exclusive creation.
            using (new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { }
            created = true;
            ClipTranscoder.Check(ffmpeg.avio_open(&output->pb, target, ffmpeg.AVIO_FLAG_WRITE), "Writing audio metadata");
            ClipTranscoder.Check(ffmpeg.avformat_write_header(output, null), "Writing audio metadata header");
            packet = ffmpeg.av_packet_alloc();
            if (packet == null) throw new OutOfMemoryException();
            int status;
            while ((status = ffmpeg.av_read_frame(input, packet)) >= 0)
            {
                ct.ThrowIfCancellationRequested();
                int i = packet->stream_index;
                ffmpeg.av_packet_rescale_ts(packet, input->streams[i]->time_base, output->streams[i]->time_base);
                packet->pos = -1;
                ClipTranscoder.Check(ffmpeg.av_interleaved_write_frame(output, packet), "Copying encoded audio/video");
                ffmpeg.av_packet_unref(packet);
            }
            if (status != ffmpeg.AVERROR_EOF) ClipTranscoder.Check(status, "Reading encoded audio/video");
            ct.ThrowIfCancellationRequested();
            ClipTranscoder.Check(ffmpeg.av_write_trailer(output), "Finishing audio metadata");
            complete = true;
        }
        finally
        {
            if (packet != null) ffmpeg.av_packet_free(&packet);
            if (output != null)
            {
                if (output->pb != null) ffmpeg.avio_closep(&output->pb);
                ffmpeg.avformat_free_context(output);
            }
            ffmpeg.avformat_close_input(&input);
            if (!complete && created) File.Delete(target);
        }
    }

    internal static int[] DefaultAudioIndexes(string path)
    {
        var input = Open(path);
        try
        {
            var result = new List<int>();
            for (int i = 0; i < input->nb_streams; i++)
                if (input->streams[i]->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO && (input->streams[i]->disposition & ffmpeg.AV_DISPOSITION_DEFAULT) != 0)
                    result.Add(i);
            return result.ToArray();
        }
        finally { ffmpeg.avformat_close_input(&input); }
    }

    internal static string PacketDigest(string path)
    {
        var input = Open(path);
        AVPacket* packet = ffmpeg.av_packet_alloc();
        try
        {
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            while (ffmpeg.av_read_frame(input, packet) >= 0)
            {
                hash.AppendData(new ReadOnlySpan<byte>(packet->data, packet->size));
                ffmpeg.av_packet_unref(packet);
            }
            return Convert.ToHexString(hash.GetHashAndReset());
        }
        finally { ffmpeg.av_packet_free(&packet); ffmpeg.avformat_close_input(&input); }
    }
}
