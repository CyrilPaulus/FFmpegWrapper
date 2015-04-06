using FFmpeg.Wrapper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting...");
            var audio = new AudioWrapper();
            var stream = new MemoryStream();
            audio.Decode(@"E:\UltraStar\songs\Acdc - Highway To Hell\Acdc - Highway To Hell.mp3", stream);
            Console.WriteLine(audio.Channels);
            Console.WriteLine(audio.Frequency);
            using(var fs = new FileStream("out", FileMode.Create)) {
                stream.WriteTo(fs);
            }
            Console.ReadLine();
        }
    }
}
