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

        var paintList = new[]
        {
            new SKPaint
            {
                IsAntialias = false,
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Blue,
                StrokeWidth = 5,
            },
            //new SKPaint
            //{
            //    IsAntialias = false,
            //    Style = SKPaintStyle.Stroke,
            //    Color = SKColors.Blue,
            //    StrokeWidth = 5,
            //    BlendMode = SKBlendMode.ColorBurn,
            //},
            new SKPaint
            {
                IsAntialias = false,
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Blue,
                StrokeWidth = 5,
                StrokeCap = SKStrokeCap.Butt,
            },
            new SKPaint
            {
                IsAntialias = false,
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Blue,
                StrokeWidth = 5,
                StrokeCap = SKStrokeCap.Round,
            },
            new SKPaint
            {
                IsAntialias = false,
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Blue,
                StrokeWidth = 5,
                StrokeCap = SKStrokeCap.Round,
                StrokeJoin = SKStrokeJoin.Bevel,
            },
            new SKPaint
            {
                IsAntialias = false,
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Blue,
                StrokeWidth = 6,
                StrokeCap = SKStrokeCap.Round,
                StrokeJoin = SKStrokeJoin.Bevel,
                StrokeMiter = 3,
            },
            new SKPaint
            {
                IsAntialias = false,
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Blue,
                StrokeWidth = 5,
                StrokeCap = SKStrokeCap.Square,
            },
            new SKPaint
            {
                IsAntialias = false,
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Blue,
                StrokeWidth = 5,
                StrokeCap = SKStrokeCap.Square,
                StrokeMiter = 10,
            },
            new SKPaint
            {
                IsAntialias = false,
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Blue,
                StrokeWidth = 5,
                StrokeCap = SKStrokeCap.Square,
                StrokeMiter = 20,
            },
        };

        for (var i = 0; i < paintList.Length; i++)
        {
            canvas.DrawLine(new SKPoint(10, 50 + i * 10 + 10), new SKPoint(200, 50 + i * 10 + 10), paintList[i]);
        }
    }
}