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
        private AVStream* _stream = null;
        private AVFrame* _frame = null;
        private AVFrame* _convFrame = null;
        private SwrContext* _swrContext = null;
        private AVPacket _pkt;
        private float[] _leftOvers = new float[0];

        private bool _opened = false;


        public void Open(string filename)
        {
            if (_opened)
                return;

            WrapperConfig.Init();

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
                    _stream = formatContext->streams[i];
                    break;
                }
            }

            if (_stream == null)
                throw new Exception("Could not find audio stream");

            var codecContext = _stream->codec;
            Channels = codecContext->channels;
            Frequency = codecContext->sample_rate;
            double time_base = (double)_stream->time_base.num / (double)_stream->time_base.den;
            Samples = (long)((double)_stream->duration * time_base * Frequency * Channels);

            var audioCodec = FFmpegInvoke.avcodec_find_decoder(codecContext->codec_id);

            codecContext->request_sample_fmt = AVSampleFormat.AV_SAMPLE_FMT_S16;

            if (audioCodec == null)
                throw new Exception("Unsupported audio codec");

            if (FFmpegInvoke.avcodec_open2(_stream->codec, audioCodec, null) < 0)
                throw new Exception("Could not open codec");


            _frame = FFmpegInvoke.avcodec_alloc_frame();
            _convFrame = FFmpegInvoke.avcodec_alloc_frame();

            FFmpegInvoke.av_frame_set_channel_layout(_convFrame, (int)codecContext->channel_layout);
            FFmpegInvoke.av_frame_set_sample_rate(_convFrame, codecContext->sample_rate);
            FFmpegInvoke.av_frame_set_channels(_convFrame, 2);
            _convFrame->format = (int)AVSampleFormat.AV_SAMPLE_FMT_FLT;
            _pkt = new AVPacket();

            fixed (AVPacket* pPacket = &_pkt)
            {
                FFmpegInvoke.av_init_packet(pPacket);
            }
            
            _swrContext = FFmpegInvoke.swr_alloc_set_opts(
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

            var cpSize = Math.Min(buffer.Length, _leftOvers.Length);
            Array.Copy(_leftOvers, buffer, cpSize);
            var leftOverCount = _leftOvers.Length - cpSize;
            if (leftOverCount > 0)
            {
                var tmp = new float[leftOverCount];
                Array.Copy(_leftOvers, _leftOvers.Length - leftOverCount, tmp, 0, leftOverCount);
                _leftOvers = tmp;
            }
            else
            {
                _leftOvers = new float[0];
            }

            index += cpSize;
            if (index >= buffer.Length)
                return true;


            fixed (AVPacket* pPacket = &_pkt)
            while (FFmpegInvoke.av_read_frame(_formatContext, pPacket) == 0)
            {
                if (_pkt.stream_index == _stream->index)
                {
                    int gotFrame = 0;
                    var rtn = FFmpegInvoke.avcodec_decode_audio4(_stream->codec, _frame, &gotFrame, pPacket);

                    if (gotFrame != 0)
                    {
                        FFmpegInvoke.av_frame_unref(_convFrame);
                        FFmpegInvoke.av_frame_set_channel_layout(_convFrame, (int)_stream->codec->channel_layout);
                        FFmpegInvoke.av_frame_set_sample_rate(_convFrame, _stream->codec->sample_rate);
                        FFmpegInvoke.av_frame_set_channels(_convFrame, 2);
                        _convFrame->format = (int)AVSampleFormat.AV_SAMPLE_FMT_FLT;


                        FFmpegInvoke.swr_convert_frame(_swrContext, _convFrame, _frame);

                        //16 bits per sample and two channels
                        int planeSize;
                        int size = FFmpegInvoke.av_samples_get_buffer_size(&planeSize, 2, _convFrame->nb_samples, AVSampleFormat.AV_SAMPLE_FMT_FLT, 1) / sizeof(float);

                        cpSize = Math.Min(size, buffer.Length - index);
                        Marshal.Copy(new IntPtr(_convFrame->data_0), buffer, index, cpSize);

                        //Copy leftOvers in a new array
                        leftOverCount = size - cpSize;
                        if (leftOverCount > 0)
                        {
                            _leftOvers = new float[leftOverCount];
                            Marshal.Copy(new IntPtr(_convFrame->data_0 + sizeof(float) * cpSize), _leftOvers, 0, leftOverCount);
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

            FFmpegInvoke.av_free(_frame);
            FFmpegInvoke.av_free(_convFrame);
            FFmpegInvoke.avcodec_close(_stream->codec);
            
            fixed (SwrContext** pSwrContext = &_swrContext)
            {
                FFmpegInvoke.swr_free(pSwrContext);
            }

            fixed (AVFormatContext** pFormatContext = &_formatContext)
            {
                FFmpegInvoke.avformat_close_input(pFormatContext);
            }

            _formatContext = null;
            _stream = null;
            _frame = null;
            _convFrame = null;
            _swrContext = null;

            _opened = false;
        }


        public void Dispose()
        {
            Close();
        }
    }
}
