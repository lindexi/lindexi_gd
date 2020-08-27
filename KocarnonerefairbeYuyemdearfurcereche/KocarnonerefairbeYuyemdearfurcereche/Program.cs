using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using dotnetCampus.FileDownloader;

namespace KocarnonerefairbeYuyemdearfurcereche
{
    class Program
    {
        static void Main(string[] args)
        {
            var url = "http://172.20.115.34:5721/image1.emf";

            var downloadFile = new FileInfo(Path.GetTempFileName());

            var segmentFileDownloader = new SegmentFileDownloader(url, downloadFile);
            
            segmentFileDownloader.DownloadFileAsync().Wait();

            downloadFile.Refresh();
            Console.WriteLine(downloadFile.Length);


            var file = "image1.emf";
            using var image = Image.FromFile(file);

            int width = 1920;
            int height = 1080;

            var resized = new Bitmap(width, height);
            using (var graphics = Graphics.FromImage(resized))
            {
                graphics.CompositingQuality = CompositingQuality.HighSpeed;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.DrawImage(image, 0, 0, width, height);
                resized.Save($"resized-{file}", ImageFormat.Png);
                Console.WriteLine($"Saving resized-{file} thumbnail");
            }
        }
    }
}
