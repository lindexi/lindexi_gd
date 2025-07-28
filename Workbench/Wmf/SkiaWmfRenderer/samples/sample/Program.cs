// See https://aka.ms/new-console-template for more information

using Oxage.Wmf;
using Oxage.Wmf.Records;

using SkiaSharp;

using SkiaWmfRenderer;

using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

var markdownText = new StringBuilder();
var outputFolder = Path.Join(AppContext.BaseDirectory, $"Output_{Path.GetRandomFileName()}");
Directory.CreateDirectory(outputFolder);

var testFile = @"C:\lindexi\wmf公式\image64.wmf";
ConvertImageFile(testFile);

//var folder = @"C:\lindexi\wmf公式\";

//foreach (var file in Directory.EnumerateFiles(folder, "*.wmf"))
//{
//    ConvertImageFile(file);
//}

var markdownFile = Path.Join(outputFolder, "README.md");
File.WriteAllText(markdownFile, markdownText.ToString());

Console.WriteLine("Hello, World!");

void ConvertImageFile(string file)
{
    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
    var gdiFileName = $"GDI_{fileNameWithoutExtension}.png";
    var gdiFile = Path.Join(outputFolder, gdiFileName);

    if (OperatingSystem.IsWindowsVersionAtLeast(6, 1))
    {
        var image = Image.FromFile(file);
        image.Save(gdiFile, ImageFormat.Png);
    }

    var wmfFileName = $"WMF_{fileNameWithoutExtension}.png";
    var testOutputFile = Path.Join(outputFolder, wmfFileName);
    var success = SkiaWmfRenderHelper.TryConvertToPng(new FileInfo(file), new FileInfo(testOutputFile));

    markdownText.AppendLine
    (
        $"""
         ## {fileNameWithoutExtension}

         **GDI:**

         ![](./{gdiFileName})

         **WMF:**

         """
    );

    if (success)
    {
        markdownText.AppendLine($"![](./{wmfFileName})");
    }
    else
    {
        markdownText.AppendLine("Rendering failed.");
    }

    markdownText.AppendLine("");
}