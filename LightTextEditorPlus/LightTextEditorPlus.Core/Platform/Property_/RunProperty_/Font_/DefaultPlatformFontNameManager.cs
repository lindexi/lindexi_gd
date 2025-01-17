namespace LightTextEditorPlus.Core.Platform;

class DefaultPlatformFontNameManager : IPlatformFontNameManager
{
    public string GetFallbackDefaultFontName() => "Arial";
    public bool CheckFontFamilyInstalled(string fontName) => true;
}
