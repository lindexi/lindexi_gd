using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;

using SkiaSharp;

using Uno.UI.Composition;

namespace Uno.Skia;

public class SkiaVisual : Visual
{
    public static SkiaVisual CreateAndInsertTo(UIElement element)
    {
        var visual = element.Visual;
        var skiaVisual = new SkiaVisual(visual.Compositor);
        visual.Children.InsertAtBottom(skiaVisual);
        return skiaVisual;
    }
    
    private SkiaVisual(Compositor compositor) : base(compositor)
    {
    }
    
    internal override void Draw(in DrawingSession session)
    {
        OnDraw?.Invoke(this, session.Surface);
    }
    
    public event EventHandler<SKSurface>? OnDraw;
}
