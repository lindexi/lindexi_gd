// See https://aka.ms/new-console-template for more information

using Oxage.Wmf;

using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using SkiaSharp;

var file = @"C:\lindexi\wmf公式\sample.wmf";
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var image = Image.FromFile(file);
var imageWidth = image.Width;
var imageHeight = image.Height;
var imagePhysicalDimension = image.PhysicalDimension;

using var fileStream = File.OpenRead(file);
var wmfDocument = new WmfDocument();
wmfDocument.Load(fileStream);


var format = wmfDocument.Format;
Console.WriteLine(format.Dump());

// Left: 61528
// Top: 62158
// Right: 4008
// Bottom: 3378
// Unit: 1000
// Checksum: 21749

var x = Math.Min(format.Left, format.Right);
var y = Math.Min(format.Top, format.Bottom);

var width = Math.Abs(format.Right - format.Left);
var height = Math.Abs(format.Bottom - format.Top);

var inchUnit = format.Unit;
var pixelWidth = (double)width / inchUnit * 96;
var pixelHeight = (double)height / inchUnit * 96;

var sx = (double) width / imageWidth;
var sy = (double) height / imageHeight;

var aw = 607d;
var ah = 512d;

var t = width / 1440;
var t2 = height / 1440;

var formatUnit = format.Unit;

// mnUnitsPerInch 

var inch = wmfDocument.Width / 1440;
var pixel = inch * 96;

var wmfDocumentHeader = wmfDocument.Header;

var skBitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
SKCanvas canvas = new SKCanvas(skBitmap);


foreach (var wmfDocumentRecord in wmfDocument.Records)
{

}

var outputFile = "1.png";
using (var outputStream = File.OpenWrite(outputFile))
{
    skBitmap.Encode(outputStream, SKEncodedImageFormat.Png, 100);
}

Console.WriteLine("Hello, World!");
