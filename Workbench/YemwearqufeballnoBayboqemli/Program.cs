// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Text;

var codePage = Console.OutputEncoding.CodePage; // The code page will be 0 when the OutputType is WinExe.

if (args.Length > 0)
{
    Console.WriteLine($"CodePage={codePage} Text=\u6797");
}
else
{
    var self = Path.Join(AppContext.BaseDirectory, "YemwearqufeballnoBayboqemli.exe");
    var processStartInfo = new ProcessStartInfo(self, "foo");
    processStartInfo.RedirectStandardOutput = true;
    processStartInfo.StandardOutputEncoding = Encoding.UTF8;
    var process = Process.Start(processStartInfo)!;
    var text = process.StandardOutput.ReadToEnd();
    // You can find the output text is "CodePage=0 Text=��"
    _ = text;
}
