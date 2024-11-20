using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Quantization;

var file = @"C:\lindexi\Image\Image.png";

using var fileStream = File.OpenRead(file);

var decode = PngDecoder.Instance.Decode(new PngDecoderOptions()
{
    PngCrcChunkHandling = PngCrcChunkHandling.IgnoreAll,
}, fileStream);

var decodeSize = decode.Size;
var pixelType = decode.PixelType;

var pngEncoder = new PngEncoder()
{
    ColorType = PngColorType.RgbWithAlpha,
    BitDepth = PngBitDepth.Bit8,
    CompressionLevel = PngCompressionLevel.DefaultCompression,
};

var outputFile = Path.GetFullPath("1.png");

decode.SaveAsPng(outputFile, pngEncoder);

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");
