using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;

var file = @"C:\lindexi\Image\Image.png";
using Image image = Image.Load(new DecoderOptions()
{
    Configuration = Configuration.Default,
    SkipMetadata = false,
}, file);

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");
