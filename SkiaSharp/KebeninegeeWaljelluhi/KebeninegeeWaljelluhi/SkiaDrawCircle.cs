using SkiaSharp;

namespace KebeninegeeWaljelluhi;

class SkiaDrawCircle : SkiaDrawBase
{
    protected override void OnDraw(SKCanvas canvas)
    {
        var skPaintList = SKPaintHelper.GetSKPaintList();

        var warpPanel = new WarpPanel();

        foreach (var skPaint in skPaintList)
        {
            var result = warpPanel.AddDraw(skRect =>
            {
                canvas.DrawCircle(skRect.MidX, skRect.MidY, skRect.Width / 2.3f, skPaint);
            });

            if (!result)
            {
                break;
            }
        }
    }
}