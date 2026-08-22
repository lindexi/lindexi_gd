using System.Globalization;
using System.Resources;

namespace XiaoXiIme.Dictionary;

internal static class DictionaryResources
{
    private static readonly ResourceManager ResourceManager = new(
        "XiaoXiIme.Dictionary.DictionaryResources",
        typeof(DictionaryResources).Assembly);

    internal static string EmptyReading => GetString(nameof(EmptyReading));
    internal static string EmptyText => GetString(nameof(EmptyText));
    internal static string InvalidColumnCount => GetString(nameof(InvalidColumnCount));
    internal static string InvalidFrequency => GetString(nameof(InvalidFrequency));
    internal static string InvalidPhoneticSourceLine => GetString(nameof(InvalidPhoneticSourceLine));
    internal static string InvalidReading => GetString(nameof(InvalidReading));
    internal static string LineTooLong => GetString(nameof(LineTooLong));
    internal static string ReadingTooLong => GetString(nameof(ReadingTooLong));
    internal static string TextTooLong => GetString(nameof(TextTooLong));

    private static string GetString(string name)
    {
        return ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? name;
    }
}
