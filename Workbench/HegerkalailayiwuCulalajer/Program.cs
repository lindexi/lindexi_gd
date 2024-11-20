using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Quantization;

var file = @"C:\lindexi\Image\Image.png";

using var fileStream = File.OpenRead(file);

var decode = JpegDecoder.Instance.Decode(new JpegDecoderOptions(),fileStream);

var imageInfo = Image.Identify(fileStream);

var decodeImage = PngDecoder.Instance.Decode(new PngDecoderOptions()
{
    PngCrcChunkHandling = PngCrcChunkHandling.IgnoreAll,
}, fileStream);

var decodeSize = decodeImage.Size;
var pixelType = decodeImage.PixelType;

decodeImage.Mutate(context =>
{
    context.Resize(100, 100);
});

var pngEncoder = new PngEncoder()
{
    ColorType = PngColorType.RgbWithAlpha,
    BitDepth = PngBitDepth.Bit8,
    CompressionLevel = PngCompressionLevel.DefaultCompression,
};

var outputFile = Path.GetFullPath("1.png");

decodeImage.SaveAsPng(outputFile, pngEncoder);

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");
