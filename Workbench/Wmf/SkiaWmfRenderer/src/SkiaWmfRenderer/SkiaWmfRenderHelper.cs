using Oxage.Wmf;

using SkiaSharp;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

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
        skBitmap = null;

        using var fileStream = wmfFile.OpenRead();
        var wmfDocument = new WmfDocument();
        try
        {
            wmfDocument.Load(fileStream);
        }
        catch (WmfException e)
        {
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

class WmfRenderer
{
    public required WmfDocument WmfDocument { get; init; }
    public required int RequestWidth { get; init; }
    public required int RequestHeight { get; init; }

    public bool TryRender([NotNullWhen(true)] out SKBitmap? skBitmap)
    {
        skBitmap = null;

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var format = WmfDocument.Format;

        var x = Math.Min(format.Left, format.Right);
        var y = Math.Min(format.Top, format.Bottom);

        var width = Math.Abs(format.Right - format.Left);
        var height = Math.Abs(format.Bottom - format.Top);

        return skBitmap == null;
    }
}