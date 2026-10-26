using System.Globalization;
using System.Resources;

namespace MediaFoundation.Controls;

internal static class Text
{
    private static readonly ResourceManager s_manager = new("MediaFoundation.Controls.Strings", typeof(Text).Assembly);
    internal static string Get(string key) => s_manager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
}