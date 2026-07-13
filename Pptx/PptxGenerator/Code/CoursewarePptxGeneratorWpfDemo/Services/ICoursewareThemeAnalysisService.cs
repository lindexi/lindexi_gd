using CoursewarePptxGeneratorWpfDemo.Models;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Analyzes a loaded courseware package and produces a validated global theme.
/// </summary>
public interface ICoursewareThemeAnalysisService
{
    /// <summary>
    /// Analyzes the package and reports user-facing workflow stages.
    /// </summary>
    /// <param name="package">The loaded courseware package.</param>
    /// <param name="progress">The optional progress receiver.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validated global theme.</returns>
    Task<CoursewareThemeAnalysisResult> AnalyzeAsync(
        CoursewareInputPackage package,
        IProgress<CoursewareAnalysisMessage>? progress,
        CancellationToken cancellationToken);
}
