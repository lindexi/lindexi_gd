using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Platform;

namespace LightTextEditorPlus.Document;

internal class SkiaPlatformRunPropertyCreator : IPlatformRunPropertyCreator
{
    public SkiaPlatformRunPropertyCreator(SkiaPlatformFontManager skiaPlatformFontManager)
    {
        _skiaPlatformFontManager = skiaPlatformFontManager;
    }

    private readonly SkiaPlatformFontManager _skiaPlatformFontManager;


    public IReadOnlyRunProperty GetDefaultRunProperty()
    {
        return new SkiaTextRunProperty(_skiaPlatformFontManager);
    }
}