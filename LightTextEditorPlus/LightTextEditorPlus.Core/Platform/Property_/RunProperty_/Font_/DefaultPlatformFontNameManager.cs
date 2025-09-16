using LightTextEditorPlus.Core.Utils;

namespace LightTextEditorPlus.Core.Platform;

class DefaultPlatformFontNameManager : IPlatformFontNameManager
{
    public string GetFallbackDefaultFontName() => "Arial";

    public string GetFallbackFontName(string desiredFontName)
    {
        return TextContext.GlobalFontNameManager.GetFallbackFontName(desiredFontName, this);
    }

    public bool CheckFontFamilyInstalled(string fontName) => true;
}
