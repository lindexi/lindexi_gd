using LightTextEditorPlus.Core.Platform;

namespace LightTextEditorPlus.Core.TestsFramework;

public class FixCharSizePlatformProvider : TestPlatformProvider
{
    public FixCharSizePlatformProvider()
    {
        CharInfoMeasurer = new FixedCharSizeCharInfoMeasurer();
    }
}