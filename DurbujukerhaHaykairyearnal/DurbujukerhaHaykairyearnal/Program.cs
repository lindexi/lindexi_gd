using System;
using System.IO;
using K4os.Compression.LZ4.Streams;

namespace DurbujukerhaHaykairyearnal
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var stream = LZ4Stream.Encode(File.Create("1.lz4")))
            {
                using (var sw = new StreamWriter(stream))
                {
                    sw.WriteLine("林德熙是逗比");
                }
            }

            using (var stream = new StreamReader(LZ4Stream.Decode(File.Open("1.lz4", FileMode.Open))))
            {
                Console.WriteLine(stream.ReadLine());
            }
        }
    }
}