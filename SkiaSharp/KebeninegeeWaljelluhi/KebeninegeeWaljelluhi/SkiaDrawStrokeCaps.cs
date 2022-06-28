using SkiaSharp;

namespace KebeninegeeWaljelluhi;

class SkiaDrawStrokeCaps : SkiaDrawBase
{
    protected override void OnDraw(SKCanvas canvas)
    {
        var skPaintProvider = new SKPaintProvider(new SKPaint()
        {
            Color = SKColors.Blue,
            StrokeWidth = 20,
            Style = SKPaintStyle.Stroke
        });

        skPaintProvider.Do((paint, value) => paint.StrokeCap = value, SKStrokeCap.Butt, SKStrokeCap.Round, SKStrokeCap.Square);

        for (var i = 1; i < skPaintProvider.SKPaintList.Count + 1; i++)
        {
            canvas.DrawLine(100, 50 * i, 200, 50 * i, skPaintProvider.SKPaintList[i - 1]);

            canvas.DrawLine(100, 50 * i, 200, 50 * i, new SKPaint()
            {
                Color = SKColors.Gray,
                StrokeWidth = 2,
                Style = SKPaintStyle.Stroke
            });
        }
    }
}