using System.Globalization;
using System.Resources;

namespace CakawuhealehaneJairijelanefer;

internal static class UiStrings
{
    private static readonly ResourceManager ResourceManagerInstance = new(
        "CakawuhealehaneJairijelanefer.Resources.UiStrings",
        typeof(UiStrings).Assembly);

    internal static string WindowTitle => GetString(nameof(WindowTitle));

    internal static string Heading => GetString(nameof(Heading));

    internal static string Description => GetString(nameof(Description));

    internal static string OutputDirectoryLabel => GetString(nameof(OutputDirectoryLabel));

    internal static string OutputFolderHint => GetString(nameof(OutputFolderHint));

    internal static string FormatsLabel => GetString(nameof(FormatsLabel));

    internal static string FormatsValue => GetString(nameof(FormatsValue));

    internal static string ExportButtonText => GetString(nameof(ExportButtonText));

    internal static string ReadyStatus => GetString(nameof(ReadyStatus));

    internal static string ExportingStatus => GetString(nameof(ExportingStatus));

    internal static string FormatExportSucceeded(int pngCount)
    {
        return string.Format(CultureInfo.CurrentCulture, GetString("ExportSucceeded"), pngCount);
    }

    internal static string FormatExportFailed(string errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
        return string.Format(CultureInfo.CurrentCulture, GetString("ExportFailed"), errorMessage);
    }

    private static string GetString(string name)
    {
        return ResourceManagerInstance.GetString(name, CultureInfo.CurrentUICulture)
            ?? throw new InvalidOperationException($"Missing UI resource '{name}'.");
    }
}
