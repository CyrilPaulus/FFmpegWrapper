using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace FFmpeg.Wrapper
{
    public unsafe class AudioWrapper
    {
        public int Frequency {get; set;}
        public int Channels { get; set; }
        private string _filename;

        public void Decode(string filename, MemoryStream output)
        {
            
            var writer = new BinaryWriter(output);
            FFmpegInvoke.av_register_all();
            FFmpegInvoke.avcodec_register_all();


            AVFormatContext* formatContext = FFmpegInvoke.avformat_alloc_context();
            
            if (FFmpegInvoke.avformat_open_input(&formatContext, filename, null, null) != 0)
                throw new Exception("Could not open file");

            if (FFmpegInvoke.avformat_find_stream_info(formatContext, null) != 0)
                throw new Exception("Could not find stream info");

            AVStream* stream = null;
            for (int i = 0; i < formatContext->nb_streams; i++)
            {
                if (formatContext->streams[i]->codec->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO)
                {
                    stream = formatContext->streams[i];
                    break;
                }
            }

            if (stream == null)
                throw new Exception("Could not find audio stream");

            
            var codecContext = stream->codec;            
            Channels = codecContext->channels;
            Frequency = codecContext->sample_rate;
            var audioCodec = FFmpegInvoke.avcodec_find_decoder(codecContext->codec_id);

            if (audioCodec == null)
                throw new Exception("Unsupported audio codec");

            if (FFmpegInvoke.avcodec_open2(stream->codec, audioCodec, null) < 0)
                throw new Exception("Could not open codec");

            var frame = FFmpegInvoke.avcodec_alloc_frame();
            var pkt = new AVPacket();

            FFmpegInvoke.av_init_packet(&pkt);

            while (FFmpegInvoke.av_read_frame(formatContext, &pkt) == 0)
            {
                if (pkt.stream_index == stream->index)
                {
                    int gotFrame = 0;
                    var rtn = FFmpegInvoke.avcodec_decode_audio4(codecContext, frame, &gotFrame, &pkt);
                    //if ( rtn < 0)
//                        throw new Exception("Error while decoding audio");

                    if (gotFrame != 0)
                    {
                        //16 bits per sample and two channels
                        int planeSize;
                        int size = FFmpegInvoke.av_samples_get_buffer_size(&planeSize, codecContext->channels, frame->nb_samples, codecContext->sample_fmt, 1);                        
                        var buff = new UInt16[planeSize * codecContext->channels];
                        for (int nb = 0; nb < planeSize / sizeof(UInt16); nb++)
                        {
                            for (int ch = 0; ch < codecContext->channels; ch++)
                            {
                                writer.Write(((UInt16*)frame->extended_data[ch])[nb]);                        
                            }
                        }

                        

                    }
                }                

                FFmpegInvoke.av_free_packet(&pkt);
            }

                     
            FFmpegInvoke.av_free(frame);
            FFmpegInvoke.avcodec_close(codecContext);
            FFmpegInvoke.avformat_close_input(&formatContext);

        }

        
    }
}
