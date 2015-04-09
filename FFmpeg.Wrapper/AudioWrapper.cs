using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace FFmpeg.Wrapper
{
    public unsafe class AudioWrapper : IDisposable
    {
        public int Frequency {get; set;}
        public int Channels { get; set; }
        public long Samples { get; set; }
        private string _filename;

        private AVFormatContext* _formatContext = null;
        private AVStream* stream = null;
        private AVFrame* frame = null;
        private AVFrame* convFrame = null;
        private SwrContext* swrContext = null;
        private AVPacket pkt;
        private float[] leftOvers = new float[0];

        private bool _opened = false;


        public void Open(string filename)
        {
            if (_opened)
                return;

            FFmpegInvoke.av_register_all();
            FFmpegInvoke.avcodec_register_all();

            AVFormatContext* formatContext = FFmpegInvoke.avformat_alloc_context();
            _formatContext = formatContext;

            if (FFmpegInvoke.avformat_open_input(&formatContext, filename, null, null) != 0)
                throw new Exception("Could not open file");

            if (FFmpegInvoke.avformat_find_stream_info(formatContext, null) != 0)
                throw new Exception("Could not find stream info");



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
            double time_base = (double)stream->time_base.num / (double)stream->time_base.den;
            Samples = (long)((double)stream->duration * time_base * Frequency * Channels);

            var audioCodec = FFmpegInvoke.avcodec_find_decoder(codecContext->codec_id);

            codecContext->request_sample_fmt = AVSampleFormat.AV_SAMPLE_FMT_S16;

            if (audioCodec == null)
                throw new Exception("Unsupported audio codec");

            if (FFmpegInvoke.avcodec_open2(stream->codec, audioCodec, null) < 0)
                throw new Exception("Could not open codec");


            frame = FFmpegInvoke.avcodec_alloc_frame();
            convFrame = FFmpegInvoke.avcodec_alloc_frame();

            FFmpegInvoke.av_frame_set_channel_layout(convFrame, (int)codecContext->channel_layout);
            FFmpegInvoke.av_frame_set_sample_rate(convFrame, codecContext->sample_rate);
            FFmpegInvoke.av_frame_set_channels(convFrame, 2);
            convFrame->format = (int)AVSampleFormat.AV_SAMPLE_FMT_FLT;
            pkt = new AVPacket();

            fixed (AVPacket* pPacket = &pkt)
            {
                FFmpegInvoke.av_init_packet(pPacket);
            }
            
            swrContext = FFmpegInvoke.swr_alloc_set_opts(
                    null,
                    (long)codecContext->channel_layout,
                    AVSampleFormat.AV_SAMPLE_FMT_FLT,
                    codecContext->sample_rate,
                    (long)codecContext->channel_layout,
                    codecContext->sample_fmt,
                    codecContext->sample_rate,
                    0,
                    null);

            _opened = true;
        }

        public bool Read(float[] buffer)
        {
            int index = 0;

            //First should write the leftovers

            var cpSize = Math.Min(buffer.Length, leftOvers.Length);
            Array.Copy(leftOvers, buffer, cpSize);
            var leftOverCount = leftOvers.Length - cpSize;
            if (leftOverCount > 0)
            {
                var tmp = new float[leftOverCount];
                Array.Copy(leftOvers, leftOvers.Length - leftOverCount, tmp, 0, leftOverCount);
                leftOvers = tmp;
            }

            index += cpSize;
            if (index >= buffer.Length)
                return true;


            fixed (AVPacket* pPacket = &pkt)
            while (FFmpegInvoke.av_read_frame(_formatContext, pPacket) == 0)
            {
                if (pkt.stream_index == stream->index)
                {
                    int gotFrame = 0;
                    var rtn = FFmpegInvoke.avcodec_decode_audio4(stream->codec, frame, &gotFrame, pPacket);

                    if (gotFrame != 0)
                    {
                        FFmpegInvoke.av_frame_unref(convFrame);
                        FFmpegInvoke.av_frame_set_channel_layout(convFrame, (int)stream->codec->channel_layout);
                        FFmpegInvoke.av_frame_set_sample_rate(convFrame, stream->codec->sample_rate);
                        FFmpegInvoke.av_frame_set_channels(convFrame, 2);
                        convFrame->format = (int)AVSampleFormat.AV_SAMPLE_FMT_FLT;


                        FFmpegInvoke.swr_convert_frame(swrContext, convFrame, frame);

                        //16 bits per sample and two channels
                        int planeSize;
                        int size = FFmpegInvoke.av_samples_get_buffer_size(&planeSize, 2, convFrame->nb_samples, AVSampleFormat.AV_SAMPLE_FMT_FLT, 1) / sizeof(float);

                        cpSize = Math.Min(size, buffer.Length - index);
                        Marshal.Copy(new IntPtr(convFrame->data_0), buffer, index, cpSize);

                        //Copy leftOvers in a new array
                        leftOverCount = size - cpSize;
                        if (leftOverCount > 0)
                        {
                            leftOvers = new float[leftOverCount];
                            Marshal.Copy(new IntPtr(convFrame->data_0 + sizeof(float) * cpSize), leftOvers, 0, leftOverCount);
                        }
                        index += cpSize;
                        if (index >= buffer.Length)
                        {
                            FFmpegInvoke.av_free_packet(pPacket);
                            return true;
                        }
                    }
                }

                FFmpegInvoke.av_free_packet(pPacket);
            }

            return false;

        }

        public void Close()
        {
            if (!_opened)
                return;

            FFmpegInvoke.av_free(frame);
            FFmpegInvoke.av_free(convFrame);
            FFmpegInvoke.avcodec_close(stream->codec);
            
            fixed (SwrContext** pSwrContext = &swrContext)
            {
                FFmpegInvoke.swr_free(pSwrContext);
            }

            fixed (AVFormatContext** pFormatContext = &_formatContext)
            {
                FFmpegInvoke.avformat_close_input(pFormatContext);
            }

            _formatContext = null;
            stream = null;
            frame = null;
            convFrame = null;
            swrContext = null;

            _opened = false;
        }


        public void Dispose()
        {
            Close();
        }
    }
}
