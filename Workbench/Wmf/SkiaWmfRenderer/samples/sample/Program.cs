// See https://aka.ms/new-console-template for more information

using DocSharp.Markdown;

using Markdig;

using Oxage.Wmf;
using Oxage.Wmf.Records;

using SkiaSharp;

using SkiaWmfRenderer;

using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using DocSharp;
using Markdig.Extensions.Figures;
using Markdig.Syntax;

var markdownText = new StringBuilder();
var outputFolder = Path.Join(AppContext.BaseDirectory, $"Output_{Path.GetRandomFileName()}");
Directory.CreateDirectory(outputFolder);

//var testFile = @"C:\lindexi\wmf公式\sample.wmf";
//ConvertImageFile(testFile);

var folder = @"C:\lindexi\wmf公式\";

foreach (var file in Directory.EnumerateFiles(folder, "*.wmf"))
{
    ConvertImageFile(file);
}

var markdownFile = Path.Join(outputFolder, "README.md");
var markdown = markdownText.ToString();
File.WriteAllText(markdownFile, markdown);

var docxFile = Path.Join(outputFolder, "README.docx");
var markdownConverter = new MarkdownConverter
{
    ImagesBaseUri = outputFolder
};

MarkdownSource markdownSource = MarkdownSource.FromMarkdownString(markdown);
markdownConverter.ToDocx(markdownSource, docxFile);

Console.WriteLine("Hello, World!");

void ConvertImageFile(string file)
{
    Console.WriteLine($"Start convert '{file}'");

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
    var stopwatch = Stopwatch.StartNew();

    var success = SkiaWmfRenderHelper.TryConvertToPng(new FileInfo(file), new FileInfo(testOutputFile));
    stopwatch.Stop();

    Console.WriteLine($"SkiaWmfRenderHelper.TryConvertToPng success={success}");

    markdownText.AppendLine
    (
        $$"""
          ## {{fileNameWithoutExtension}}

          **GDI:**

          ![](./{{gdiFileName}}){width=250 height=120}

          **WMF:**

          """
    );

    if (success)
    {
        markdownText.AppendLine($"![](./{wmfFileName})"+ "{width=250 height=120}");
    }
    else
    {
        markdownText.AppendLine("Rendering failed.");
    }

    markdownText.AppendLine();
    markdownText.AppendLine($"Rendering time: {stopwatch.ElapsedMilliseconds} ms");
}
