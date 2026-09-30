using System.Globalization;
using System.Resources;

namespace XiaoXiIme.ImeUi.Avalonia;

internal static class DebugResources
{
    private static readonly ResourceManager Resources = new("XiaoXiIme.ImeUi.Avalonia.DebugResources", typeof(DebugResources).Assembly);
    internal static string OutputLabel => Resources.GetString(nameof(OutputLabel), CultureInfo.CurrentUICulture)!;
    internal static string LogLabel => Resources.GetString(nameof(LogLabel), CultureInfo.CurrentUICulture)!;
    internal static string PackagePath => Resources.GetString(nameof(PackagePath), CultureInfo.CurrentUICulture)!;
    internal static string LoadSucceeded => Resources.GetString(nameof(LoadSucceeded), CultureInfo.CurrentUICulture)!;
    internal static string PackageHint => Resources.GetString(nameof(PackageHint), CultureInfo.CurrentUICulture)!;
    internal static string WindowTitle => Resources.GetString(nameof(WindowTitle), CultureInfo.CurrentUICulture)!;
    internal static string LoadingTitle => Resources.GetString(nameof(LoadingTitle), CultureInfo.CurrentUICulture)!;
    internal static string LoadFailedTitle => Resources.GetString(nameof(LoadFailedTitle), CultureInfo.CurrentUICulture)!;
}
