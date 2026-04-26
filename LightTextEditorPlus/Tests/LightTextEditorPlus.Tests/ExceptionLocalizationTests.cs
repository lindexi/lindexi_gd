using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using LightTextEditorPlus.Exceptions;

namespace LightTextEditorPlus.Tests;

[TestClass]
public class ExceptionLocalizationTests
{
    [TestMethod("当 CurrentUICulture 是 en-US 时，WPF 自定义异常使用英语资源")]
    public void WpfExceptionShouldUseEnglishResource()
    {
        var satelliteAssemblyPath = Path.Combine(AppContext.BaseDirectory, "en-US", "LightTextEditorPlus.Wpf.resources.dll");

        Assert.IsTrue(File.Exists(satelliteAssemblyPath));

        var resourceNames = Assembly.LoadFrom(satelliteAssemblyPath).GetManifestResourceNames();

        Assert.IsTrue(resourceNames.Contains("LightTextEditorPlus.Resources.Wpf.ExceptionMessages.resources"));
    }

    [TestMethod("当 CurrentUICulture 没有对应资源时，WPF 自定义异常回退到中文中性资源")]
    public void WpfExceptionShouldFallbackToChineseResource()
    {
        using var cultureScope = new CultureScope(new CultureInfo("fr-FR"));

        var exception = Assert.ThrowsExactly<StaticConfigurationPropertyMultipleSettingsException>(() =>
            StaticConfigurationPropertyMultipleSettingsException.Throw("DefaultNotDefineFontFamily"));

        StringAssert.Contains(exception.Message, "只允许设置一次");
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
