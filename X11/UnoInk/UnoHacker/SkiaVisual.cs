#if HAS_UNO
using Microsoft.UI.Composition;
using Uno.UI.Composition;

namespace UnoHacker;

public class SkiaVisual : Visual
{
    public SkiaVisual(Compositor compositor) : base(compositor)
    {
    }
    
    internal override void Draw(in DrawingSession session)
    {
        base.Draw(in session);
    }
}
#endif
