using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using dotnetCampus.FileDownloader;

namespace KocarnonerefairbeYuyemdearfurcereche
{
    class Program
    {
        static void Main(string[] args)
        {
            var port = 8080;

            var server = $"http://172.30.162.24:{port}";
            var url = server + $"/ImageTest/";
            var httpClient = new HttpClient();

            var imageConvertTaskRequest = new ImageConvertTaskRequest()
            {
                ImageUrl = "http://172.20.115.34:5721/image1.emf"
            };

            var response = httpClient.PostAsJsonAsync(url, imageConvertTaskRequest).Result;
            var resizedFile = $"{server}{response.Content.ReadAsStringAsync().Result}";
            Console.WriteLine(resizedFile);

            var downloadFile = new FileInfo(Path.GetTempFileName()+".png");

            var segmentFileDownloader = new SegmentFileDownloader(resizedFile, downloadFile);

            segmentFileDownloader.DownloadFileAsync().Wait();

            downloadFile.Refresh();
            Console.WriteLine(downloadFile.Length);
            Process.Start("explorer", downloadFile.FullName);

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

    public class ImageConvertTaskRequest
    {
        public string ImageUrl { set; get; } = null!;
    }


    /// <summary>
    /// 图片优化
    /// </summary>
    public static class ImageOptimization
    {

    }
}
