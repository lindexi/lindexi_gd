using SkiaSharp;

namespace KebeninegeeWaljelluhi;

class SkiaDrawDissolve : SkiaDrawBase
{
    protected override void OnDraw(SKCanvas canvas)
    {
        var file1 = new FileInfo("SkiaDrawRectangle.png");
        var file2 = new FileInfo("SkiaDrawCircle.png");

        using var fileStream1 = file1.OpenRead();
        using var fileStream2 = file2.OpenRead();

        using var resourceBitmap1 = SKBitmap.Decode(fileStream1);
        using var resourceBitmap2 = SKBitmap.Decode(fileStream2);
        const float width = 1920;
        const float height = 1080;

        var progress = 0.3;
        var skPaint = new SKPaint();
        skPaint.Color = skPaint.Color.WithAlpha((byte) (0xFF * (1 - progress)));

        canvas.DrawBitmap(resourceBitmap1, dest: new SKRect(0, 0, width, height), skPaint);
        canvas.DrawBitmap(resourceBitmap2, dest: new SKRect(0, 0, width, height), skPaint);
    }
}