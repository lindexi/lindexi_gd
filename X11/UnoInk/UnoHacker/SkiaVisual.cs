<<<<<<< HEAD
#if HAS_UNO
using Microsoft.UI.Composition;
using Uno.UI.Composition;

namespace UnoHacker;

public class SkiaVisual : Visual
{
    public SkiaVisual(Compositor compositor) : base(compositor)
=======
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
>>>>>>> 8bc253f1b095527a007667281960ac542224d8ad
    {
    }
    
    internal override void Draw(in DrawingSession session)
    {
<<<<<<< HEAD
        base.Draw(in session);
    }
}
#endif
=======
        OnDraw?.Invoke(this, session.Surface);
    }
    
    public event EventHandler<SKSurface>? OnDraw;
}
>>>>>>> 8bc253f1b095527a007667281960ac542224d8ad
