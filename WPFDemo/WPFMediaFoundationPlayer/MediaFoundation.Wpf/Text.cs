using System.Globalization;
using System.Resources;

namespace MediaFoundation.Wpf;

internal static class Text
{
    private static readonly ResourceManager Manager = new("MediaFoundation.Wpf.Strings", typeof(Text).Assembly);
    internal static string Get(string key) => Manager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
}
