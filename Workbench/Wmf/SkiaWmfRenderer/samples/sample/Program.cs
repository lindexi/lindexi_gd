// See https://aka.ms/new-console-template for more information

using Oxage.Wmf;
using Oxage.Wmf.Records;

using SkiaSharp;

using SkiaWmfRenderer;

using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

var file = @"C:\lindexi\wmf公式\image17.wmf";

SkiaWmfRenderHelper.TryConvertToPng(new FileInfo(file), new FileInfo("foo.png"));

Console.WriteLine("Hello, World!");
