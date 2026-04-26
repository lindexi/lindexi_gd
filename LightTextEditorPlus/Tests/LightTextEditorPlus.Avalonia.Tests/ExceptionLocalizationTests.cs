using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using LightTextEditorPlus.FontManagers;
using LightTextEditorPlus.Platform;

namespace LightTextEditorPlus.Avalonia.Tests;

[TestClass]
public class ExceptionLocalizationTests
{
    [TestMethod("当 CurrentUICulture 是 en-US 时，Avalonia 平台的字体资源异常使用英语资源")]
    public void AvaloniaFontExceptionShouldUseEnglishResource()
    {
        var satelliteAssemblyPath = Path.Combine(AppContext.BaseDirectory, "en-US", "LightTextEditorPlus.Avalonia.resources.dll");

        Assert.IsTrue(File.Exists(satelliteAssemblyPath));

        var resourceNames = Assembly.LoadFrom(satelliteAssemblyPath).GetManifestResourceNames();

        Assert.IsTrue(resourceNames.Contains("LightTextEditorPlus.Resources.Avalonia.ExceptionMessages.resources"));
    }

    [TestMethod("当 CurrentUICulture 是 en-US 时，Skia 平台提供器复用异常使用英语资源")]
    public void SkiaExceptionShouldUseEnglishResource()
    {
        var satelliteAssemblyPath = Path.Combine(AppContext.BaseDirectory, "en-US", "LightTextEditorPlus.Skia.resources.dll");

        Assert.IsTrue(File.Exists(satelliteAssemblyPath));

        var resourceNames = Assembly.LoadFrom(satelliteAssemblyPath).GetManifestResourceNames();

        Assert.IsTrue(resourceNames.Contains("LightTextEditorPlus.Resources.Skia.ExceptionMessages.resources"));
    }

    [TestMethod("当 CurrentUICulture 没有对应资源时，Avalonia 平台的字体资源异常回退到中文中性资源")]
    public void AvaloniaFontExceptionShouldFallbackToChineseResource()
    {
        using var cultureScope = new CultureScope(new CultureInfo("fr-FR"));
        var fontName = Guid.NewGuid().ToString("N");
        var fontFile = new FileInfo(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ttf"));

        var exception = Assert.ThrowsExactly<FileNotFoundException>(() => TextEditorFontResourceManager.TryRegisterFontNameToResource(fontName, fontFile));

        StringAssert.Contains(exception.Message, "未找到字体文件：");
    }

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _originalCulture;
        private readonly CultureInfo _originalUICulture;

        public CultureScope(CultureInfo culture)
        {
            _originalCulture = CultureInfo.CurrentCulture;
            _originalUICulture = CultureInfo.CurrentUICulture;

            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _originalCulture;
            CultureInfo.CurrentUICulture = _originalUICulture;
        }
    }
}
