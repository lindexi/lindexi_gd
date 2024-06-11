// See https://aka.ms/new-console-template for more information

using System.Diagnostics;

var folder = @"C:\lindexi\Gif\";

foreach (var file in Directory.EnumerateFiles(folder,"*.gif"))
{
    Process.Start(new ProcessStartInfo(@"C:\lindexi\Gif\ffmpeg\ffmpeg.exe",
        $"-i {file} -movflags faststart -pix_fmt yuv420p -vf \"scale=trunc(iw/2)*2:trunc(ih/2)*2\" {Path.GetFileNameWithoutExtension(file)}.mp4")
    {
        WorkingDirectory = folder
    });
}

Console.WriteLine("Hello, World!");
