using System.Globalization;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Tests.Exceptions;

[TestClass]
public class ExceptionLocalizationTest
{
    [TestMethod("Core 异常在缺少对应区域资源时应回退到英文中性资源")]
    public void NotFoundMatchCharacterFontShouldFallbackToNeutralEnglish()
    {
        using var _ = TestCultureScope.Use("fr-FR");

        var exception = new NotFoundMatchCharacterFontException(new Utf32CodePoint('A'));

        Assert.AreEqual(
            "No font that can render character 'A' was found. The current device might not have any suitable font installed.",
            exception.Message);
    }

    [TestMethod("Core 异常应从 zh-Hans 卫星程序集读取中文资源")]
    public void NotFoundMatchCharacterFontShouldUseZhHansSatelliteAssembly()
    {
        using var _ = TestCultureScope.Use("zh-Hans");

        var exception = new NotFoundMatchCharacterFontException(new Utf32CodePoint('A'));

        Assert.AreEqual("无法找到 'A' 字符的可渲染字体，可能当前设备未安装任何一款字体。", exception.Message);
    }

    private sealed class TestCultureScope(CultureInfo originalCulture, CultureInfo originalUICulture) : IDisposable
    {
        public static TestCultureScope Use(string cultureName)
        {
            CultureInfo culture = CultureInfo.GetCultureInfo(cultureName);
            TestCultureScope scope = new(CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            return scope;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUICulture;
        }
    }
}
