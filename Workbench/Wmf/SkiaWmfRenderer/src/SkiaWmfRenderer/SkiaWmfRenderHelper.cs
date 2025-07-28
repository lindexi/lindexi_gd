using System.Diagnostics.CodeAnalysis;
using System.Text;
using Oxage.Wmf;

using SkiaSharp;
using SkiaWmfRenderer.Rendering;

namespace SkiaWmfRenderer;

public static class SkiaWmfRenderHelper
{
    public static bool TryConvertToPng(FileInfo wmfFile, FileInfo outputPngFile)
    {
        if (!TryRender(wmfFile, 0, 0, out var skBitmap))
        {
            return false;
        }

        using var outputStream = outputPngFile.OpenWrite();
        skBitmap.Encode(outputStream, SKEncodedImageFormat.Png, 100);

        return true;
    }

    public static bool TryRender(FileInfo wmfFile, int requestWidth, int requestHeight, [NotNullWhen(true)] out SKBitmap? skBitmap)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        skBitmap = null;

        using var fileStream = wmfFile.OpenRead();
        var wmfDocument = new WmfDocument();

        try
        {
            wmfDocument.Load(fileStream);
        }
        catch (WmfException e)
        {
            Console.WriteLine($"[SkiaWmfRenderHelper] TryRender Fail. {e}");
            return false;
        }

        var wmfRenderer = new WmfRenderer()
        {
            WmfDocument = wmfDocument,
            RequestWidth = requestWidth,
            RequestHeight = requestHeight,
        };

        return wmfRenderer.TryRender(out skBitmap);
    }
}