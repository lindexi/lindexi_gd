using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace KocarnonerefairbeYuyemdearfurcereche
{
    class Program
    {
        static void Main(string[] args)
        {
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
