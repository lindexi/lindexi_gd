using System.Globalization;
using System.Resources;

namespace CoursewarePptxGeneratorWpfDemo.Resources;

internal static class CoursewareUiStrings
{
    private static readonly ResourceManager ResourceManager = new(
        "CoursewarePptxGeneratorWpfDemo.Resources.CoursewareUiStrings",
        typeof(CoursewareUiStrings).Assembly);

    internal static string AnalysisCompletedTitle => GetString(nameof(AnalysisCompletedTitle));

    internal static string AnalysisCompletedMessageFormat => GetString(nameof(AnalysisCompletedMessageFormat));

    internal static string AnalysisReadyStatus => GetString(nameof(AnalysisReadyStatus));

    internal static string AnalysisReadyCaptionFormat => GetString(nameof(AnalysisReadyCaptionFormat));

    internal static string CapabilityPassed => GetString(nameof(CapabilityPassed));

    internal static string CapabilityNotRequested => GetString(nameof(CapabilityNotRequested));

    internal static string CapabilityNotSupported => GetString(nameof(CapabilityNotSupported));

    internal static string CapabilityFailed => GetString(nameof(CapabilityFailed));

    internal static string UnknownCapabilityStatus => GetString(nameof(UnknownCapabilityStatus));

    internal static string PrototypeRenderingLog => GetString(nameof(PrototypeRenderingLog));

    internal static string PrototypeSlideSummaryFormat => GetString(nameof(PrototypeSlideSummaryFormat));

    private static string GetString(string name)
    {
        return ResourceManager.GetString(name, CultureInfo.CurrentUICulture)
            ?? throw new MissingManifestResourceException($"缺少用户界面字符串资源：{name}");
    }
}
