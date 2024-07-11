using SkiaSharp;

namespace WhaleljainaDeljayfecelchalearqe;

public class SKCanvasTest
{
    public void Test(SKCanvas skCanvas)
    {
        var skImageInfo = new SKImageInfo(500, 500, SKColorType.Bgra8888, SKAlphaType.Premul);
        var skBitmap = new SKBitmap(skImageInfo);

        var skCanvas2 = new SKCanvas(skBitmap);
        using var skPaint = new SKPaint()
        {
            StrokeWidth = 2,
            Color = SKColors.Black,
            IsStroke = true,
        };
        for (int i = 0; i < 100; i++)
        {
            skCanvas2.DrawLine(i * 5, 500, (i + 1) * 5, 500, skPaint);
        }
        skCanvas2.Flush();
        skCanvas2.Dispose();

        //skCanvas.Scale(2);

        //skCanvas.Scale(0.5f);
        skCanvas.DrawBitmap(skBitmap, 0, 0);

    }
}