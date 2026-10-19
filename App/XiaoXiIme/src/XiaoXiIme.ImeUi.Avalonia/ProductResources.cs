using System.Globalization;
using System.Resources;

namespace XiaoXiIme.ImeUi.Avalonia;

internal static class ProductResources
{
    private static readonly ResourceManager Resources = new("XiaoXiIme.ImeUi.Avalonia.ProductResources", typeof(ProductResources).Assembly);
    internal static string Text(string key) => Resources.GetString(key, CultureInfo.CurrentUICulture) ?? key;
    internal static string Format(string key, params object[] args) => string.Format(CultureInfo.CurrentUICulture, Text(key), args);
    internal static string Scheme(string? scheme) => Text(XiaoXiIme.Dictionary.PhoneticSchemeCatalog.All.FirstOrDefault(item => item.SchemeId == scheme)?.DisplayResourceKey ?? "Unknown");
}
