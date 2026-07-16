using AgentLib.Model;
using CoursewarePptxGeneratorWpfDemo.Models;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Executes the language-model portion of whole-courseware theme analysis.
/// </summary>
public interface ICoursewareThemeAgent
{
    /// <summary>
    /// Generates and validates a theme from the bounded courseware input.
    /// </summary>
    /// <param name="analysisInput">The bounded courseware input.</param>
    /// <param name="slideWidth">The dominant slide width.</param>
    /// <param name="slideHeight">The dominant slide height.</param>
    /// <param name="progress">The optional analysis progress sink.</param>
    /// <param name="messageProgress">The optional sink for complete Copilot messages.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The validated theme.</returns>
    Task<CoursewareTheme> AnalyzeAsync(
        CoursewareAnalysisInput analysisInput,
        double slideWidth,
        double slideHeight,
        IProgress<CoursewareAnalysisEvent>? progress = null,
        IProgress<CopilotChatMessage>? messageProgress = null,
        CancellationToken cancellationToken = default);
}