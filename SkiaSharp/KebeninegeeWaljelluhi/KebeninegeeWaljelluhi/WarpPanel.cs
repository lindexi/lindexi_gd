using SkiaSharp;

namespace KebeninegeeWaljelluhi;

class WarpPanel
{
    public WarpPanel()
    {
        OffsetX = ElementMargin;
        OffsetY = ElementMargin;
    }

    public float Width { set; get; } = 1920;
    public float Height { set; get; } = 1080;

    public float OffsetX { set; get; }
    public float OffsetY { set; get; }

    public float ElementMargin { set; get; } = 10;

    public float ElementWidth { set; get; } = 20;
    public float ElementHeight { set; get; } = 20;

    public bool AddDraw(Action<SKRect> action)
    {
        action(new SKRect(OffsetX, OffsetY, OffsetX + ElementWidth,
            OffsetY + ElementHeight));

        OffsetX += ElementWidth + ElementMargin;
        if (OffsetX + ElementWidth + ElementMargin > Width)
        {
            OffsetX = ElementMargin;

            OffsetY += ElementHeight + ElementMargin;

            if (OffsetY + ElementHeight + ElementMargin > Height)
            {
                return false;
            }
        }

        return true;
    }
}