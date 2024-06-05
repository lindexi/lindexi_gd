#if HAS_UNO
using Microsoft.UI.Composition;

namespace UnoHacker;

public class SkiaVisual : Visual
{
    public SkiaVisual(Compositor compositor) : base(compositor)
    {
    }


}
#endif
