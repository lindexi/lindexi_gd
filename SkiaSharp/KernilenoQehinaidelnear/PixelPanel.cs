using Microsoft.Maui.Graphics;

namespace KernilenoQehinaidelnear;

class PixelPanel(SkiaCanvasContext context)
{
    private SkiaCanvasContext Context => context;

    public event EventHandler<Rect>? RenderBoundsChanged;
}