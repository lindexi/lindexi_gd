using SkiaSharp;

namespace KebeninegeeWaljelluhi;

static class SKPaintHelper
{
    public static SKPaint[] GetSKPaintList()
    {
        return new[]
        {
            new SKPaint
            {
                IsAntialias = false,
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Blue,
                StrokeWidth = 5,
            },
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
            new SKPaint
            {
                IsAntialias = true, // 抗锯齿
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Blue,
                StrokeWidth = 5,
                StrokeCap = SKStrokeCap.Square,
                StrokeMiter = 20,
            },
            new SKPaint
            {
                IsAntialias = true, // 抗锯齿
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Blue,
                StrokeWidth = 5,
                StrokeCap = SKStrokeCap.Square,
                StrokeMiter = 20,
                PathEffect = SKPathEffect.CreateDash(new float[] { 10, 5, 2, 5 }, 10),
            },
            new SKPaint
            {
                IsAntialias = true, // 抗锯齿
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Blue,
                StrokeWidth = 5,
                StrokeCap = SKStrokeCap.Square,
                PathEffect = SKPathEffect.CreateDash(new float[] { 10, 10 }, 20),
            },
            new SKPaint
            {
                IsAntialias = true, // 抗锯齿
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Blue,
                StrokeWidth = 5,
                StrokeCap = SKStrokeCap.Square,
                PathEffect = SKPathEffect.CreateDash(new float[] { 10, 10, 30, 10 }, 20),
            },
        };
    }
}