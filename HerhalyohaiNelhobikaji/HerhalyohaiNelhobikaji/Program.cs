using System;
using System.IO;
using System.Threading.Tasks;

namespace HerhalyohaiNelhobikaji
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using (var fileStream = new FileStream("File.txt", FileMode.Open))
            {
                var buffer = new byte[1024];
                var task = Task<int>.Factory.FromAsync(fileStream.BeginRead, fileStream.EndRead, buffer, 0, 1024, null);
                var n = await task;
            }
        }
    }
}
