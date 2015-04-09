using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace FFmpeg.Wrapper
{
    public unsafe class VideoWrapper : IDisposable
    {

        private const double SKIP_FRAME_DIFF = 0.010;
        public int Width { get; set; }
        public int Height { get; set; }
        
        public long FrameCount { get; set; }
        public bool Paused { get; set; }
        public float Duration
        {
            get;
            private set;

        }

        public bool Loop
        {
            get
            {
                return _loop;
            }
            set
            {
                _loop = value;
                _loopTime = 0;
            }
        }
        public double Position
        {
            get
            {
                return _position;
            }
            set
            {
                _position = value;
                SetPosition(value);
            }
        }



        private bool _loop;
        private double _loopTime;
        
        public double _frameduration;

        public VideoWrapper()
        {
      
        }


        AVPacket _packet;
        private AVFormatContext* _pFormatContext;
        AVStream* _pStream = null;
        AVFrame* _pDecodedFrame;
        AVPicture* _pConvertedFrame;
        Byte* _pConvertedFrameBuffer;
        SwsContext* _pConvertContext;
        bool _opened = false;
        double _videoClock;
        private bool _lastFrameValid;
        private double _position;
        
        public void Open(string filename)
        {
            WrapperConfig.Init();            

            AVFormatContext* pFormatContext = FFmpegInvoke.avformat_alloc_context();
            _pFormatContext = pFormatContext;
                        
            if (FFmpegInvoke.avformat_open_input(&pFormatContext, filename, null, null) != 0)
                throw new Exception("Could not open file");            

            if (FFmpegInvoke.avformat_find_stream_info(pFormatContext, null) != 0)
                throw new Exception("Could not find stream info");

            for (int i = 0; i < pFormatContext->nb_streams; i++)
            {
                if (pFormatContext->streams[i]->codec->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
                {
                    _pStream = pFormatContext->streams[i];
                    break;
                }
            }

            if (_pStream == null)
                throw new Exception("Could not found video stream");

            AVCodecContext codecContext = *(_pStream->codec);
            codecContext.workaround_bugs = FFmpegInvoke.FF_BUG_AUTODETECT;
                        
            _frameduration = 1 / q2d(_pStream->r_frame_rate);
            FrameCount = _pStream->nb_frames;
            Duration = (float)pFormatContext->duration / FFmpegInvoke.AV_TIME_BASE;
            Width = codecContext.width;
            Height = codecContext.height;


            AVPixelFormat sourcePixFmt = codecContext.pix_fmt;
            AVCodecID codecId = codecContext.codec_id;
            var convertToPixFmt = AVPixelFormat.AV_PIX_FMT_RGB24;            
            _pConvertContext = FFmpegInvoke.sws_getContext(Width, Height, sourcePixFmt,
                                                                       Width, Height, convertToPixFmt,
                                                                       FFmpegInvoke.SWS_FAST_BILINEAR, null, null, null);

            if (_pConvertContext == null)
                throw new Exception("Could not initialize the conversion context");

            _pConvertedFrame = (AVPicture*)FFmpegInvoke.avcodec_alloc_frame();
            int convertedFrameBufferSize = FFmpegInvoke.avpicture_get_size(convertToPixFmt, Width, Height);
            _pConvertedFrameBuffer = (byte*)FFmpegInvoke.av_malloc((uint)convertedFrameBufferSize);
            FFmpegInvoke.avpicture_fill(_pConvertedFrame, _pConvertedFrameBuffer, convertToPixFmt, Width, Height);

            AVCodec* pCodec = FFmpegInvoke.avcodec_find_decoder(codecId);
            if (pCodec == null)
                throw new Exception("Unsupported codec");
                        
            if (FFmpegInvoke.avcodec_open2(_pStream->codec, pCodec, null) < 0)
                throw new Exception("Could not open codec");

            _pDecodedFrame = FFmpegInvoke.avcodec_alloc_frame();

            _packet = new AVPacket();           

            fixed (AVPacket* pPacket = &_packet)
            {             
                FFmpegInvoke.av_init_packet(pPacket);
            }

            _opened = true;
        }

        private bool DecodeFrame()
        {
            
            fixed (AVPacket* pPacket = &_packet)
            {                
                int frameFinished = 0;
                double pts = 0;
                
                while (frameFinished == 0)
                {
                    if (FFmpegInvoke.av_read_frame(_pFormatContext, pPacket) < 0)
                        return false;

                    if (pPacket->stream_index == _pStream->index) {

                        FFmpegInvoke.avcodec_decode_video2(_pStream->codec, _pDecodedFrame, &frameFinished, pPacket);
                        Console.WriteLine("PTS " + pts);

                        if (frameFinished != 0)
                        {
                            pts = _pDecodedFrame->best_effort_timestamp;
                            pts *= q2d(_pStream->time_base);
                            Console.WriteLine("PKT_PTS" + pts);
                            SynchronizeTime(pts);
                        }

                    }

                    FFmpegInvoke.av_free_packet(pPacket);
                }    
                return true;
            }
        }

        private void SynchronizeTime(double pts)
        {
            double frameDelay;

            if (pts != 0)
                _videoClock = pts;
            else
                pts = _videoClock;

            frameDelay = q2d(_pStream->codec->time_base);
            frameDelay += _pDecodedFrame->repeat_pict * (frameDelay * 0.5);
            _videoClock += frameDelay;
        }

        /// <summary>
        /// Read a new frame, don't touch buffer if no need for new frame
        /// </summary>
        /// <param name="time"></param>
        /// <param name="outBfr"></param>
        public void ReadFrame(double time, byte[] outBfr) {
            double timeDiff;

            if (!_opened)
                return;

            if (Paused)
                return;

            double currentTime;
            if (Loop)
                currentTime = time - _loopTime;
            else
                currentTime = time;

            if (_lastFrameValid)
            {
                timeDiff = currentTime - _videoClock;

                //No need for new frame
                if (timeDiff < _frameduration)
                    return;
            }

            bool success = DecodeFrame();
            timeDiff = currentTime - _videoClock;

            //Skip frames :
            if (timeDiff >= Math.Max(_frameduration, SKIP_FRAME_DIFF))
            {
                int dropFrameCount = (int)(timeDiff / _frameduration);
                _videoClock = _videoClock + dropFrameCount * _frameduration;
                Console.WriteLine(dropFrameCount);
                for (int i = 0; i < dropFrameCount; i++)
                    success = DecodeFrame();
            }

            if (!success)
            {
                if (Loop)
                {
                    Position = 0;
                    _loopTime = time;
                }
                return;
            }

            //We can finally decode a frame !

            byte** src = &_pDecodedFrame->data_0;
            byte** dst = &_pConvertedFrame->data_0;
            FFmpegInvoke.sws_scale(_pConvertContext, src, _pDecodedFrame->linesize, 0,
                                    Height, dst, _pConvertedFrame->linesize);

            byte* convertedFrameAddress = _pConvertedFrame->data_0;

            var imageBufferPtr = new IntPtr(convertedFrameAddress);

            Marshal.Copy(imageBufferPtr, outBfr, 0, Width * Height * 3);

            if (!_lastFrameValid)
                _lastFrameValid = true;
        }

        private void SetPosition(double time)
        {
            if (!_opened)
                return;

            if (time < 0)
                time = 0;

            int seekFlags = FFmpegInvoke.AVSEEK_FLAG_BACKWARD;
            _videoClock = time;
            _lastFrameValid = false;

            if (FFmpegInvoke.av_seek_frame(_pFormatContext, _pStream->index, (long)Math.Round(time / q2d(_pStream->time_base)), seekFlags) < 0)
                return;

            FFmpegInvoke.avcodec_flush_buffers(_pStream->codec);

        }

        public void Close()
        {
            if (!_opened)
                return;
                                    
            FFmpegInvoke.av_free(_pConvertedFrame);
            FFmpegInvoke.av_free(_pConvertedFrameBuffer);
            FFmpegInvoke.sws_freeContext(_pConvertContext);

            FFmpegInvoke.av_free(_pDecodedFrame);
            FFmpegInvoke.avcodec_close(_pStream->codec);
            fixed (AVFormatContext** pFormatContext = &_pFormatContext)
            {
                FFmpegInvoke.avformat_close_input(pFormatContext);
            }

            _videoClock = 0;
            _pFormatContext = null;
            _pStream = null;
            _pDecodedFrame = null;
            _pConvertedFrame = null;
            _pConvertedFrameBuffer = null;
            _pConvertContext = null;
            _opened = false;
        }

        public void Dispose()
        {            
            Close();
        }

        private double q2d(AVRational r)
        {
            return (double)r.num / (double)r.den;
        }
    }

}


