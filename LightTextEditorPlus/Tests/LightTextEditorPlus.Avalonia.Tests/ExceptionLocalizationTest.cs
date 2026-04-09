using System.Globalization;
using Avalonia.Threading;
using LightTextEditorPlus.Platform;

namespace LightTextEditorPlus.Avalonia.Tests;

[TestClass]
public class ExceptionLocalizationTest
{
    [TestMethod("Avalonia 顶层异常在缺少对应区域资源时应回退到英文中性资源")]
    public async Task AvaloniaTextEditorEnterEditModeShouldFallbackToNeutralEnglish()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            using var cultureScope = TestCultureScope.Use("fr-FR");
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

    [TestMethod("Avalonia 顶层异常应从 zh-Hans 卫星程序集读取中文资源")]
    public async Task AvaloniaTextEditorEnterEditModeShouldUseZhHansSatelliteAssembly()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            using var cultureScope = TestCultureScope.Use("zh-Hans");
            TextEditor textEditor = new()
            {
                IsEditable = false
            };

            InvalidOperationException exception = Assert.ThrowsExactly<InvalidOperationException>(textEditor.EnterEditMode);

            Assert.AreEqual("当前文本不可编辑 IsEditable=false 不能进入编辑模式。", exception.Message);
        });
    }

    [TestMethod("Skia 异常在缺少对应区域资源时应回退到英文中性资源")]
    public void SkiaTextEditorConstructorShouldFallbackToNeutralEnglish()
    {
        using var cultureScope = TestCultureScope.Use("fr-FR");
        SkiaTextEditorPlatformProvider platformProvider = new();
        SkiaTextEditor firstEditor = new(platformProvider);

        InvalidOperationException exception = Assert.ThrowsExactly<InvalidOperationException>(() =>
            new SkiaTextEditor(platformProvider));

        Assert.AreEqual(
            "Each SkiaTextEditorPlatformProvider can be associated with only one SkiaTextEditor. Reusing one across editors is not supported. The current provider is already bound to a text editor whose DebugName is \"\".",
            exception.Message);
    }

    [TestMethod("Skia 异常应从 zh-Hans 卫星程序集读取中文资源")]
    public void SkiaTextEditorConstructorShouldUseZhHansSatelliteAssembly()
    {
        using var cultureScope = TestCultureScope.Use("zh-Hans");
        SkiaTextEditorPlatformProvider platformProvider = new();
        SkiaTextEditor firstEditor = new(platformProvider);

        InvalidOperationException exception = Assert.ThrowsExactly<InvalidOperationException>(() =>
            new SkiaTextEditor(platformProvider));

        Assert.AreEqual(
            "每个 SkiaTextEditorPlatformProvider 只能和一个 SkiaTextEditor 关联，禁止跨文本框使用。当前传入的 SkiaTextEditorPlatformProvider 关联的文本框的 DebugName=\"\"",
            exception.Message);
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
