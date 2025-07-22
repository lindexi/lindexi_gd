// See https://aka.ms/new-console-template for more information
using ImageMagick;

foreach (var file in Directory.EnumerateFiles(@"C:\lindexi\wmf公式\","*.wmf"))
{
    var fileName = Path.GetFileNameWithoutExtension(file);

    if (fileName != "sample")
    {
        continue;
    }

    var outputFile = Path.GetFullPath($"{fileName}.png");

    using var image = new MagickImage(file,new MagickReadSettings()
    {
        BackgroundColor = MagickColors.Transparent,
    });
    image.Write(new FileInfo(outputFile), MagickFormat.Png32);
}

Console.WriteLine("Hello, World!");
