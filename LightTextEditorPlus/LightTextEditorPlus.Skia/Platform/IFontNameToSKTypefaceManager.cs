using System.Diagnostics.CodeAnalysis;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Document;
using SkiaSharp;

namespace LightTextEditorPlus.Platform;

public interface IFontNameToSKTypefaceManager
{
    SKTypeface Resolve(SkiaTextRunProperty runProperty);

    bool TryFallbackRunProperty(SkiaTextRunProperty runProperty, ICharObject charObject,
        [NotNullWhen(true)] out SkiaTextRunProperty? newRunProperty);
}
