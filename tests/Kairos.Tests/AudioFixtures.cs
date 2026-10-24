using FFmpeg.AutoGen;
using Kairos.Engine.Audio;
using Kairos.Engine.Share;

namespace Kairos.Tests;

static unsafe class AudioFixtures
{
    public static string Create(double seconds=2, double lateSeconds=0, double shortSeconds=0,int sourceCount=3, double amplitude=0.15, double middleGapSeconds=0)
    {
        TestFfmpeg.Load();
        string dir=Path.Combine(Path.GetTempPath(),"replay-tones",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
        bool lossless=amplitude>1;
        string path=Path.Combine(dir,lossless?"tones.mov":"tones.mp4");
        AVFormatContext* input=null; AVFormatContext* output=null;
        AVPacket* packet=ffmpeg.av_packet_alloc(); AVFrame* frame=null;
        var encoders=new List<IntPtr>();
        try
        {
            string fixture=Path.Combine(AppContext.BaseDirectory,"Fixtures","Share","clip-h264.mp4");
            ClipTranscoder.Check(ffmpeg.avformat_open_input(&input,fixture,null,null),"fixture input");
            ClipTranscoder.Check(ffmpeg.avformat_find_stream_info(input,null),"fixture info");
            int video=-1;
            for(int i=0;i<input->nb_streams;i++) if(input->streams[i]->codecpar->codec_type==AVMediaType.AVMEDIA_TYPE_VIDEO){video=i;break;}
            ClipTranscoder.Check(ffmpeg.avformat_alloc_output_context2(&output,null,lossless?"mov":"mp4",path),"tone output");
            var v=ffmpeg.avformat_new_stream(output,null);
            ffmpeg.avcodec_parameters_copy(v->codecpar,input->streams[video]->codecpar);v->codecpar->codec_tag=0;v->time_base=input->streams[video]->time_base;
            for(int t=0;t<=sourceCount;t++)
            {
                var codec=ffmpeg.avcodec_find_encoder(lossless?AVCodecID.AV_CODEC_ID_PCM_F32LE:AVCodecID.AV_CODEC_ID_AAC);
                var encoder=ffmpeg.avcodec_alloc_context3(codec); encoders.Add((IntPtr)encoder);
                encoder->sample_rate=48000;encoder->sample_fmt=lossless?AVSampleFormat.AV_SAMPLE_FMT_FLT:AVSampleFormat.AV_SAMPLE_FMT_FLTP;
                encoder->bit_rate=192000; encoder->time_base=new(){num=1,den=48000};encoder->flags|=ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;
                ffmpeg.av_channel_layout_default(&encoder->ch_layout,2);
                ClipTranscoder.Check(ffmpeg.avcodec_open2(encoder,codec,null),"fixture aac");
                var s=ffmpeg.avformat_new_stream(output,null);ffmpeg.avcodec_parameters_from_context(s->codecpar,encoder);s->time_base=encoder->time_base;
                s->disposition=t==0?ffmpeg.AV_DISPOSITION_DEFAULT:0;
            }
            var tracks=new AudioTrackInfo[]{new("mix",1,"Mistura",AudioRole.Mix),new("game",2,"Jogo",AudioRole.Application),new("discord",3,"Discord",AudioRole.Application),new("spotify",4,"Spotify",AudioRole.Application),new("microphone",5,"Microfone",AudioRole.Microphone)}.Take(sourceCount+1).ToArray();
            ffmpeg.av_dict_set(&output->metadata,"comment",AudioLayoutMetadata.Write(new(1,tracks)),0);
            ClipTranscoder.Check(ffmpeg.avio_open(&output->pb,path,ffmpeg.AVIO_FLAG_WRITE),"fixture write");
            ClipTranscoder.Check(ffmpeg.avformat_write_header(output,null),"fixture header");
            while(ffmpeg.av_read_frame(input,packet)>=0)
            {
                if(packet->stream_index==video && packet->pts*ffmpeg.av_q2d(input->streams[video]->time_base)<seconds)
                {
                    ffmpeg.av_packet_rescale_ts(packet,input->streams[video]->time_base,v->time_base);packet->stream_index=v->index;packet->pos=-1;
                    ClipTranscoder.Check(ffmpeg.av_interleaved_write_frame(output,packet),"fixture video packet");
                }
                ffmpeg.av_packet_unref(packet);
            }
            for(int t=0;t<=sourceCount;t++)
            {
                var encoder=(AVCodecContext*)encoders[t]; var stream=output->streams[t+1];
                frame=ffmpeg.av_frame_alloc(); frame->format=(int)encoder->sample_fmt;frame->sample_rate=48000;frame->nb_samples=lossless?1024:encoder->frame_size;
                ffmpeg.av_channel_layout_copy(&frame->ch_layout,&encoder->ch_layout);ffmpeg.av_frame_get_buffer(frame,0);
                int duration=(int)((t==3&&shortSeconds>0?shortSeconds:seconds)*48000);
                int offset=t==2?(int)(lateSeconds*48000):0;
                for(int at=0;at<duration;at+=frame->nb_samples)
                {
                    ffmpeg.av_frame_make_writable(frame); frame->pts=at+offset+(t==1 && at>=48000?(long)(middleGapSeconds*48000):0);
                    for(int i=0;i<frame->nb_samples;i++)
                    {
                        double time=(at+i)/48000.0;
                        double sample=0;
                        if(t==0){for(int f=1;f<=sourceCount;f++)sample+=amplitude*Math.Sin(2*Math.PI*(220*(f+1))*time);}
                        else sample=amplitude*Math.Sin(2*Math.PI*(220*(t+1))*time);
                        if(lossless){((float*)frame->data[0])[i*2]=(float)sample;((float*)frame->data[0])[i*2+1]=(float)sample;}
                        else {((float*)frame->data[0])[i]=(float)sample;((float*)frame->data[1])[i]=(float)sample;}
                    }
                    ClipTranscoder.Check(ffmpeg.avcodec_send_frame(encoder,frame),"fixture audio frame");Drain(encoder,packet,stream,output);
                }
                ffmpeg.avcodec_send_frame(encoder,null);Drain(encoder,packet,stream,output);ffmpeg.av_frame_free(&frame);
            }
            ClipTranscoder.Check(ffmpeg.av_write_trailer(output),"fixture trailer");
        }
        finally
        {
            if(frame!=null)ffmpeg.av_frame_free(&frame);
            foreach(var p in encoders){var e=(AVCodecContext*)p;ffmpeg.avcodec_free_context(&e);}
            ffmpeg.av_packet_free(&packet);
            if(output!=null){if(output->pb!=null)ffmpeg.avio_closep(&output->pb);ffmpeg.avformat_free_context(output);}
            if(input!=null)ffmpeg.avformat_close_input(&input);
        }
        return path;
    }
    static void Drain(AVCodecContext* encoder,AVPacket* packet,AVStream* stream,AVFormatContext* output)
    {
        while(ffmpeg.avcodec_receive_packet(encoder,packet)>=0)
        {
            ffmpeg.av_packet_rescale_ts(packet,encoder->time_base,stream->time_base);packet->stream_index=stream->index;
            ClipTranscoder.Check(ffmpeg.av_interleaved_write_frame(output,packet),"fixture audio packet");ffmpeg.av_packet_unref(packet);
        }
    }
    public static double Magnitude(float[] samples,double frequency,int sampleRate=16000)
    {
        int start=Math.Min(samples.Length/4,4000),count=Math.Min(samples.Length-start,16000);
        if(count<=0)return 0;
        double re=0,im=0;
        for(int i=0;i<count;i++){double phase=2*Math.PI*frequency*i/sampleRate;re+=samples[start+i]*Math.Cos(phase);im+=samples[start+i]*Math.Sin(phase);}
        return 2*Math.Sqrt(re*re+im*im)/count;
    }
}
