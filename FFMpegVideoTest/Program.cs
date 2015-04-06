using FFmpeg.Wrapper;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFMpegVideoTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var _videoWrapper = new VideoWrapper(@"E:\UltraStar\songs\Acdc - Highway To Hell\Acdc - Highway To Hell [VD#0].avi");
           _videoWrapper.Open();
           var data = new Byte[_videoWrapper.Height * _videoWrapper.Width * 3];
           int count = 0;

           for (int i = 0; i < _videoWrapper.FrameCount; i++)
           {
               if (!_videoWrapper.ReadFrame(data))
                   Console.WriteLine("couldn't read frame" + count);

               if (data != null)
                   count++;
           }

           Console.WriteLine(count);
        }
    }
}
