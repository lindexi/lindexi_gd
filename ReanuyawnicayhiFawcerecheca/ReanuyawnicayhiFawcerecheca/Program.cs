using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ReanuyawnicayhiFawcerecheca
{
    class Program
    {
        static void Main(string[] args)
        {
            var httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromSeconds(3)
            };

            var ignoreList = new[] { "http://image.acmx.xyz", "http://creativecommons.org/licenses/", "http://blog.csdn.net", "0.0.0.0", "127.0.0.1", "localhost" };

            foreach (var file in Directory.GetFiles(args[0], "*.md"))
            {
                Log($"[start] {file}");

                try
                {
                    var text = File.ReadAllText(file);

                    var regex = new Regex(@"([a-zA-z]+://[^\s^:^)^""]*)");
                    foreach (Match match in regex.Matches(text))
                    {
                        var url = match.Groups[1].Value;

                        if (url.EndsWith(")"))
                        {
                            url = url.Remove(url.Length - 1);
                        }

                        if (url.EndsWith("\""))
                        {
                            url = url.Remove(url.Length - 1);
                        }

                        if (ignoreList.Any(temp => url.Contains(temp)))
                        {
                            continue;
                        }

                        try
                        {
                            var httpResponseMessage = httpClient.GetAsync(url).Result;
                            if (httpResponseMessage.StatusCode != HttpStatusCode.OK)
                            {
                                Log(
                                    $"{Path.GetFileName(file)} {url} {(int)httpResponseMessage.StatusCode}");
                            }
                        }
                        catch (Exception e)
                        {
                            if (e.InnerException is TaskCanceledException)
                            {
                                continue;
                            }

                            Log($"{Path.GetFileName(file)} {url} {e}");
                        }
                    }
                }
                catch (Exception e)
                {
                    Log(e.ToString());
                }

                Log($"[end] {file}");
            }
        }

        private static void Log(string message)
        {
            Console.WriteLine(message);
            File.AppendAllText("Log.txt", $"{DateTime.Now:yyyy-MM-dd hh:mm:ss.fff} {message}");
        }
    }
}