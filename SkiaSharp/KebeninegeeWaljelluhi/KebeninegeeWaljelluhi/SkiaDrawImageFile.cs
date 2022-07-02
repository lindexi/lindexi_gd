using SkiaSharp;

namespace KebeninegeeWaljelluhi;

class SkiaDrawImageFile : SkiaDrawBase
{
    protected override void OnDraw(SKCanvas canvas)
    {
        canvas.Clear();
        var file1 = new FileInfo("SkiaDrawLine.png");
        var file2 = new FileInfo("SkiaDrawCircle.png");

        using var fileStream1 = file1.OpenRead();
        using var fileStream2 = file2.OpenRead();

        using var resourceBitmap1 = SKBitmap.Decode(fileStream1);
        using var resourceBitmap2 = SKBitmap.Decode(fileStream2);
        const int width = 1920;
        const int height = 1080;

        // 这个解码会变糊 [SkiaSharp decode the png file be blur · Issue #2132 · mono/SkiaSharp](https://github.com/mono/SkiaSharp/issues/2132 )
        canvas.DrawBitmap(resourceBitmap1, new SKPoint(0, 0));

        //canvas.DrawBitmap(resourceBitmap1, dest: new SKRect(0, 0, width / 2, height));
        //canvas.DrawBitmap(resourceBitmap2, dest: new SKRect(width / 2, 0, width, height));
    }
}