using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FFmpeg.Wrapper
{
    internal static class WrapperConfig
    {
        private static bool _initialized = false;

        public static void Init()
        {
            if (_initialized)
                return;

            FFmpegInvoke.av_register_all();
            FFmpegInvoke.avcodec_register_all();
            _initialized = true;
        }
    }
}
