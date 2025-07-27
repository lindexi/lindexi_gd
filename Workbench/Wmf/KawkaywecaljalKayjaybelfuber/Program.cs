// See https://aka.ms/new-console-template for more information

using System.Drawing;
using System.Drawing.Imaging;
using Oxage.Wmf;

var file = @"C:\lindexi\wmf公式\sample.wmf";

var image = Image.FromFile(file);
var imageWidth = image.Width;
var imageHeight = image.Height;

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



var width = Math.Abs(format.Left - format.Right);
var height = Math.Abs(format.Top - format.Bottom);
var aw = 607d;
var ah = 512d;

var t = width / 1440;
var t2 = height / 1440;

var formatUnit = format.Unit;

// mnUnitsPerInch 

var inch = wmfDocument.Width / 1440;
var pixel = inch * 96;

var wmfDocumentHeader = wmfDocument.Header;

foreach (var wmfDocumentRecord in wmfDocument.Records)
{
    
}

Console.WriteLine("Hello, World!");
