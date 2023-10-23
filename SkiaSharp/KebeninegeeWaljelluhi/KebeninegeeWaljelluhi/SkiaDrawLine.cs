using SkiaSharp;

namespace KebeninegeeWaljelluhi;

class SkiaDrawLine : SkiaDrawBase
{
    protected override void OnDraw(SKCanvas canvas)
    {
        var fillPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill, // 无论是 Fill 还是 Stroke 效果都是相同
            Color = SKColors.Blue,
            StrokeWidth = 10,
        };

        canvas.Translate(300,100);

        canvas.DrawLine(10, 10, 100, 10, fillPaint);

        var strokePaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            Color = SKColors.Blue,
            StrokeWidth = 10,
        };

        canvas.DrawLine(10, 30, 100, 30, strokePaint);

        canvas.DrawLine(new SKPoint(10, 50), new SKPoint(100, 50), strokePaint);

        var paintList = SKPaintHelper.GetSKPaintList();

        for (var i = 0; i < paintList.Length; i++)
        {
            canvas.DrawLine(new SKPoint(10, 50 + i * 20 + 10), new SKPoint(200, 50 + i * 20 + 10), paintList[i]);
        }

        for (var i = 0; i < paintList.Length; i++)
        {
            canvas.DrawLine(new SKPoint(10, 50 + paintList.Length * 20 + 20 + i * 20), new SKPoint(200, 50 + paintList.Length * 20 + 20 + i * 20 + 10), paintList[i]);
        }
    }
}