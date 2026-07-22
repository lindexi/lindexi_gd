using AgentLib.Model;
using CoursewarePptxGenerator.Core.Models;
using CoursewarePptxGeneratorWpfDemo.Models;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Executes the language-model portion of whole-courseware theme analysis.
/// </summary>
public interface ICoursewareThemeAgent
{
    /// <summary>
    /// Generates and validates an executable design system from the bounded courseware input.
    /// </summary>
    /// <param name="analysisInput">The bounded courseware input.</param>
    /// <param name="inputPackage">The local package used only to load selected screenshot attachments.</param>
    /// <param name="structuredFacts">Deterministic facts extracted from the complete Markdown.</param>
    /// <param name="visualSamples">The bounded path-free visual sample manifest.</param>
    /// <param name="progress">The optional analysis progress sink.</param>
    /// <param name="messageProgress">The optional sink for complete Copilot messages.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The validated executable design-system result.</returns>
    Task<CoursewareDesignSystemAgentResult> AnalyzeAsync(
        CoursewareAnalysisInput analysisInput,
        CoursewareInputPackage inputPackage,
        CoursewareStructuredFactReport structuredFacts,
        IReadOnlyList<CoursewareVisualSample> visualSamples,
        IProgress<CoursewareAnalysisEvent>? progress = null,
        IProgress<CopilotChatMessage>? messageProgress = null,
        CancellationToken cancellationToken = default);
}