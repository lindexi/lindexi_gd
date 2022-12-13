using LightTextEditorPlus.Core.Platform;

namespace LightTextEditorPlus.Core.TestsFramework;

public class FixCharSizePlatformProvider : TestPlatformProvider
{
    public override ICharInfoMeasurer? GetCharInfoMeasurer() => new FixedCharSizeCharInfoMeasurer();
}