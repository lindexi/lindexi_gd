using SkiaSharp;

namespace KebeninegeeWaljelluhi;

static class SKPaintHelper
{
    public static SKPaint[] GetSKPaintList()
    {
        return GetSKPaintListInner().ToArray();


    }

    private static IEnumerable<SKPaint> GetSKPaintListInner()
    {
        var skPaintProvider = new SKPaintProvider();
        skPaintProvider
            .Do((skPaint, value) => skPaint.IsAntialias = value, new[] { true, false })
            .Do((skPaint, value) => skPaint.Style = value, SKPaintStyle.Stroke, SKPaintStyle.Fill,
                SKPaintStyle.StrokeAndFill)
            .Do((skPaint, value) => skPaint.Color = value, SKColors.Blue, new SKColor(0xFF565656))
            //.Do((skPaint, value) => skPaint.ColorF = value, new SKColorF(0.5f, 0.6f, 0.5f),
            //    SKColorF.FromHsl(0.2f, 0.2f, 0.5f))
            .Do((skPaint, value) => skPaint.StrokeWidth = value, 2f, 5f, 5.5f)
            .Do((skPaint, value) => skPaint.StrokeCap = value, SKStrokeCap.Square, SKStrokeCap.Round, SKStrokeCap.Butt)
            .Do((skPaint, value) => skPaint.StrokeJoin = value, SKStrokeJoin.Round, SKStrokeJoin.Bevel,
                SKStrokeJoin.Miter)
            .Do((skPaint, value) => skPaint.StrokeMiter = value, 0.5f, 2f, 5f)

            ;

        foreach (var skPaint in skPaintProvider.SKPaintList)
        {
            yield return skPaint;
        }

        var skPaintList = new[]
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

        foreach (var skPaint in skPaintList)
        {
            yield return skPaint;
        }
    }
}