using SkiaSharp;

namespace KebeninegeeWaljelluhi;

class SkiaDrawRoundRectangle : SkiaDrawBase
{
    protected override void OnDraw(SKCanvas canvas)
    {
        var skPaintList = SKPaintHelper.GetSKPaintList();

        var warpPanel = new WarpPanel();

        foreach (var skPaint in skPaintList)
        {
            var result =
                warpPanel.AddDraw(skRect => { canvas.DrawRoundRect(skRect, 2, 2, skPaint); });

            if (!result)
            {
                return;
            }
        }
    }
}