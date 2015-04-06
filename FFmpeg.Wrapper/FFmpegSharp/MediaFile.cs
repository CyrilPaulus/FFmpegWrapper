using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace FFmpeg.Wrapper.FFmpegSharp
{
    public unsafe class MediaFile : IDisposable
    {
        AVFormatContext* _formatContext;
        bool _disposed = false;
        private SortedList<int, DecoderStream> _stream;
        private string _filename;

        public unsafe ReadOnlyCollection<DecoderStream> Streams
        {
            get { return new ReadOnlyCollection<DecoderStream>(_stream.Values); }
        }

        public string FileName
        {
            get { return _filename; }
        }

        public long Length
        {
            get
            {
                return FFmpegInvoke.avio_size(_formatContext->pb);
            }
        }
    }
}
