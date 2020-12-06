using System;
using System.IO;
using System.Threading.Tasks;

namespace KebejeeballNeejayurwe
{
    class Program
    {
        static void Main(string[] args)
        {
            var logFile = @"f:\temp\build\build.txt";
            PrintLog(logFile);
        }

        private static void PrintLog(string logFile)
        {
            DateTimeOffset? lastTime = null;

            var allLogTextList = File.ReadAllLines(logFile);
            foreach (var logText in allLogTextList)
            {
                // 读取第一个空格
                var text = logText;
                var spaceIndex = logText.IndexOf(' ');
                if (spaceIndex > 0)
                {
                    var span = logText.AsSpan();
                    var timeText = span.Slice(0, spaceIndex);
                    if (DateTimeOffset.TryParse(timeText, out var currentTime))
                    {
                        if (lastTime != null)
                        {
                            var offset = currentTime - lastTime.Value;

                            if (offset > TimeSpan.FromMilliseconds(10) && offset < TimeSpan.FromMinutes(1))
                            {
                                Task.Delay(offset).Wait();
                            }
                        }

                        lastTime = currentTime;

                        text = logText.Substring(spaceIndex);
                    }
                }

                Console.WriteLine(DateTimeOffset.Now.ToString("O") + " " + text);
            }
        }
    }
}
