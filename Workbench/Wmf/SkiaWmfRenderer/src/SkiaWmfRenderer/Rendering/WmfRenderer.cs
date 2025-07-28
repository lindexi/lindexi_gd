using System.Diagnostics.CodeAnalysis;
using System.Text;
using Oxage.Wmf;
using SkiaSharp;

namespace SkiaWmfRenderer;

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

        skBitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

        using SKCanvas canvas = new SKCanvas(skBitmap);

        try
        {
            var result = TryRenderInner(canvas);

            if (result)
            {
                return true;
            }
            else
            {
                skBitmap.Dispose();
                return false;
            }
        }
        catch (Exception e)
        {
            skBitmap.Dispose();
            return false;
        }
    }

    private bool TryRenderInner(SKCanvas canvas)
    {
        return true;
    }
}