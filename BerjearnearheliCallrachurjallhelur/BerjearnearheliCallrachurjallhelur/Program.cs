using System;
using System.Collections.Generic;
using System.IO;

namespace BerjearnearheliCallrachurjallhelur
{
    class Program
    {
        static void Main(string[] args)
        {
            var folder = @"e:\上传\坚果云\回收站\网络课程\Enbx\";
            foreach (var file in Directory.GetFiles(folder, "*.enbx", SearchOption.AllDirectories))
            {
                Console.WriteLine(file);
            }

            Console.Read();
        }
    }
}