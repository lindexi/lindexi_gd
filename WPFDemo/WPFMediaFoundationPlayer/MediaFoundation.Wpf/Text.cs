using System.Globalization;
using System.Resources;

namespace MediaFoundation.Controls;

internal static class Text
{
    private static readonly ResourceManager Manager = new("MediaFoundation.Controls.Strings", typeof(Text).Assembly);
    internal static string Get(string key) => Manager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
}
