using LightTextEditorPlus.Core.Platform;

namespace LightTextEditorPlus.Core.Tests;

public class FixCharSizePlatformProvider : TestPlatformProvider
{
    public override ICharInfoMeasurer? GetCharInfoMeasurer() => new FixedCharSizeCharInfoMeasurer();
}