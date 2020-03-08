using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace LearballeleGayfunaka
{
    class Program
    {
        static void Main(string[] args)
        {
            var processStartInfo = new ProcessStartInfo(@"..\..\..\..\NijemgewaQeewhachonalllelwhohar\bin\Debug\net45\NijemgewaQeewhachonalllelwhohar.exe")
            {
                RedirectStandardOutput = true
            };
            var process = Process.Start(processStartInfo);

            while (true)
            {
                var temp = process.StandardOutput.BaseStream.ReadByte();
                Task.Delay(100).Wait();
                if (temp == -1)
                {
                    break;
                }
            }

            process.WaitForExit();
            Console.WriteLine(DateTime.Now);
            Console.WriteLine(process.ExitTime);
        }
    }
}
