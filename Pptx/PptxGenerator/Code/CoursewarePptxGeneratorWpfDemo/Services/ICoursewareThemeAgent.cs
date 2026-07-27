using AgentLib.Model;
using CoursewarePptxGeneratorWpfDemo.Models;
using PptxGenerator.Models;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Executes the language-model portion of whole-courseware theme analysis.
/// </summary>
public interface ICoursewareThemeAgent
{
    /// <summary>
    /// Generates a lightweight courseware theme from the prepared prompt.
    /// </summary>
    Task<CoursewareTheme> AnalyzeAsync(
        string prompt,
        SlideDocumentContext validationCanvas,
        IReadOnlySet<string> availableResourceIds,
        IProgress<CoursewareAnalysisEvent>? progress = null,
        IProgress<CopilotChatMessage>? messageProgress = null,
        CancellationToken cancellationToken = default);
}