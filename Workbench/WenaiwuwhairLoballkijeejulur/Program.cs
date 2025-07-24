// See https://aka.ms/new-console-template for more information
using ImageMagick;

if (args.Length == 0)
{
    return;
}

var file = args[0];
var fileName = Path.GetFileNameWithoutExtension(file);

var outputFile = Path.GetFullPath($"{fileName}.png");

using var image = new MagickImage(file, new MagickReadSettings()
{
    BackgroundColor = MagickColors.Transparent,
    Format = MagickFormat.Wmf,
});
image.Write(new FileInfo(outputFile), MagickFormat.Png32);

Console.WriteLine($"Finish convert. OutputFile={outputFile}");