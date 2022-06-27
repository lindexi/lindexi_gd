using SkiaSharp;

namespace KebeninegeeWaljelluhi;

class SkiaDrawCircle : SkiaDrawBase
{
    protected override void OnDraw(SKCanvas canvas)
    {
        // create the paint for the filled circle
        var circleFill = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = SKColors.Blue
        };

        // draw the circle fill
        canvas.DrawCircle(100, 100, 40, circleFill);

        // create the paint for the circle border
        var circleBorder = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            Color = SKColors.Red,
            StrokeWidth = 5,
        };

        // draw the circle border
        canvas.DrawCircle(200, 100, 40, circleBorder);

        var circleBorderAndFill = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.StrokeAndFill,
            Color = SKColors.Red,
            StrokeWidth = 5,
        };

        canvas.DrawCircle(300, 100, 40, circleBorderAndFill);
    }
}