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
            
            audio.Open(@"E:\UltraStar\songs\Gaetan Roussel - Help myself\Gaetan Roussel - Help myself.mp3");
            Console.WriteLine(audio.Channels);
            Console.WriteLine(audio.Frequency);

            using(var fs = new BinaryWriter(new FileStream("out", FileMode.Create))) {
                var buffer = new float[1024];
                while (audio.Read(buffer))
                {
                    for (int i = 0; i < buffer.Length; i++)
                        fs.Write(buffer[i]);
                }               
                
            }
            
            
            Console.WriteLine("Finished");
            Console.ReadLine();
        }
    }
}
