using System.Globalization;
using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Tests;

[TestClass]
public class ExceptionLocalizationTest
{
    [TestMethod]
    public void EnterEditModeShouldFallbackToNeutralEnglish()
    {
        "WPF 异常在缺少对应区域资源时应回退到英文中性资源".Test(() =>
        {
            using var _ = TestCultureScope.Use("fr-FR");
            TextEditor textEditor = new()
            {
                IsEditable = false
            };

            InvalidOperationException exception = Assert.ThrowsExactly<InvalidOperationException>(textEditor.EnterEditMode);

            Assert.AreEqual(
                "The current text editor is not editable. IsEditable=false, so edit mode cannot be entered.",
                exception.Message);
        });
    }

    [TestMethod]
    public void EnterEditModeShouldUseZhHansSatelliteAssembly()
    {
        "WPF 异常应从 zh-Hans 卫星程序集读取中文资源".Test(() =>
        {
            using var _ = TestCultureScope.Use("zh-Hans");
            TextEditor textEditor = new()
            {
                IsEditable = false
            };

            InvalidOperationException exception = Assert.ThrowsExactly<InvalidOperationException>(textEditor.EnterEditMode);

            Assert.AreEqual("当前文本不可编辑 IsEditable=false 不能进入编辑模式。", exception.Message);
        });
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
