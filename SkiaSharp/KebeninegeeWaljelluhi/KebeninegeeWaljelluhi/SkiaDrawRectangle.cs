using SkiaSharp;

namespace KebeninegeeWaljelluhi;

class SkiaDrawRectangle : SkiaDrawBase
{
    protected override void OnDraw(SKCanvas canvas)
    {
        var skPaintList = SKPaintHelper.GetSKPaintList();

        var warpPanel = new WarpPanel();

        for (var i = 0; i < int.MaxValue; i++)
        {
            var result =
                warpPanel.AddDraw((skRect) => { canvas.DrawRect(skRect, skPaintList[i % skPaintList.Length]); });

            if (!result)
            {
                return;
            }
        }
    }
}