using CoursewarePptxGenerator.Core.Models;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Builds the natural-language input for whole-courseware theme analysis.
/// </summary>
public interface ICoursewareThemeAnalysisPromptBuilder
{
    /// <summary>
    /// Builds a prompt from lightweight style references and the original slide Markdown.
    /// </summary>
    string Build(CoursewareInputPackage inputPackage, CoursewareStyleUsageSummary styleUsageSummary);
}
