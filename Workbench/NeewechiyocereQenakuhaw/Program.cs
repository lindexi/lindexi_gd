// See https://aka.ms/new-console-template for more information
using ImageMagick;

var file = @"C:\lindexi\sample.wmf";
var outputFile = Path.GetFullPath(@"sample.png");

using var image = new MagickImage(file);
image.Write(new FileInfo(outputFile), MagickFormat.Png32);

Console.WriteLine("Hello, World!");
