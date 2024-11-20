using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

var file = @"C:\lindexi\Image\Image.png";

using var fileStream = File.OpenRead(file);

var decode = PngDecoder.Instance.Decode(new PngDecoderOptions()
{
    PngCrcChunkHandling = PngCrcChunkHandling.IgnoreAll,
}, fileStream);

var decodeSize = decode.Size;
var pixelType = decode.PixelType;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");
