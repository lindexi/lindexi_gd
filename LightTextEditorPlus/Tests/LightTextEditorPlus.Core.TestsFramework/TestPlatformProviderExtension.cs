using LightTextEditorPlus.Core.Platform;

namespace LightTextEditorPlus.Core.TestsFramework;

public static class TestPlatformProviderExtension
{
    /// <summary>
    /// 固定字符尺寸，方便调试布局
    /// </summary>
    /// <param name="provider"></param>
    /// <returns></returns>
    public static TestPlatformProvider UsingFixedCharSizeCharInfoMeasurer(this TestPlatformProvider provider)
    {
        provider.CharInfoMeasurer = new FixedCharSizeCharInfoMeasurer();
        return provider;
    }

    public static TestPlatformProvider UseFakeLineSpacingCalculator(this TestPlatformProvider provider, ILineSpacingCalculator? lineSpacingCalculator = null)
    {
        provider.LineSpacingCalculator = lineSpacingCalculator ?? new FakeLineSpacingCalculator();
        return provider;
    }
}