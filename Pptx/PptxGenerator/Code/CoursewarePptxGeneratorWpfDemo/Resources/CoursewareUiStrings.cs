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

    internal static string SlideInitialRenderingLog => GetString(nameof(SlideInitialRenderingLog));

    internal static string SlideRuntimeNotCreated => GetString(nameof(SlideRuntimeNotCreated));

    internal static string SlideRuntimeCreating => GetString(nameof(SlideRuntimeCreating));

    internal static string SlideRuntimeReady => GetString(nameof(SlideRuntimeReady));

    internal static string SlideRuntimeModelUnavailable => GetString(nameof(SlideRuntimeModelUnavailable));

    internal static string SlideRuntimeFailed => GetString(nameof(SlideRuntimeFailed));

    internal static string SlideRuntimeCanceled => GetString(nameof(SlideRuntimeCanceled));

    internal static string SlideStatusNotStarted => GetString(nameof(SlideStatusNotStarted));

    internal static string SlideStatusInitializing => GetString(nameof(SlideStatusInitializing));

    internal static string SlideStatusReady => GetString(nameof(SlideStatusReady));

    internal static string SlideStatusGenerating => GetString(nameof(SlideStatusGenerating));

    internal static string SlideStatusRendering => GetString(nameof(SlideStatusRendering));

    internal static string SlideStatusCompleted => GetString(nameof(SlideStatusCompleted));

    internal static string SlideStatusFailed => GetString(nameof(SlideStatusFailed));

    internal static string SlideStatusCanceled => GetString(nameof(SlideStatusCanceled));

    internal static string ScreenshotNotPrepared => GetString(nameof(ScreenshotNotPrepared));

    internal static string ScreenshotAttached => GetString(nameof(ScreenshotAttached));

    internal static string ScreenshotFileMissing => GetString(nameof(ScreenshotFileMissing));

    internal static string ScreenshotSendFailed => GetString(nameof(ScreenshotSendFailed));

    internal static string SlideGenerationCompleted => GetString(nameof(SlideGenerationCompleted));

    internal static string SlideGenerationCanceled => GetString(nameof(SlideGenerationCanceled));

    internal static string SlideRerenderCompleted => GetString(nameof(SlideRerenderCompleted));

    internal static string SlideRerenderCanceled => GetString(nameof(SlideRerenderCanceled));

    private static string GetString(string name)
    {
        return ResourceManager.GetString(name, CultureInfo.CurrentUICulture)
            ?? throw new MissingManifestResourceException($"缺少用户界面字符串资源：{name}");
    }
}
