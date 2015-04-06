using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FFmpeg.Wrapper.FFmpegSharp
{
    public class DecoderException : ApplicationException
    {
        public DecoderException() { }
        public DecoderException(string Message) : base(Message) { }
    }
}
