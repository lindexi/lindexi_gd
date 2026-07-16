using AgentLib.Model;
using CoursewarePptxGeneratorWpfDemo.Models;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Analyzes a loaded courseware package and produces a shared visual theme.
/// </summary>
public interface ICoursewareThemeAnalysisService
{
    /// <summary>
    /// Analyzes the specified courseware package.
    /// </summary>
    /// <param name="inputPackage">The loaded courseware package.</param>
    /// <param name="progress">The optional sink for user-facing analysis events.</param>
    /// <param name="messageProgress">The optional sink for complete Copilot messages.</param>
    /// <param name="cancellationToken">The token used to cancel the analysis.</param>
    /// <returns>The generated whole-courseware theme analysis result.</returns>
    Task<CoursewareThemeAnalysisResult> AnalyzeAsync(
        CoursewareInputPackage inputPackage,
        IProgress<CoursewareAnalysisEvent>? progress = null,
        IProgress<CopilotChatMessage>? messageProgress = null,
        CancellationToken cancellationToken = default);
}
