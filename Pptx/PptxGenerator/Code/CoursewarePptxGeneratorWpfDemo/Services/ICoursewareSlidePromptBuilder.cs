using CoursewarePptxGenerator.Core.Models;
using CoursewarePptxGeneratorWpfDemo.Models;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Builds privacy-safe natural-language prompts for real courseware slides.
/// </summary>
public interface ICoursewareSlidePromptBuilder
{
    /// <summary>
    /// Prepares the immutable workspace source reused by page prompts.
    /// </summary>
    CoursewareSlidePromptSource PrepareSource(
        CoursewareInputPackage inputPackage,
        CoursewareThemeAnalysisResult analysisResult,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds the initial natural-language page-generation prompt.
    /// </summary>
    string BuildInitialPrompt(
        CoursewareSlidePromptSource source,
        int slideIndex,
        CoursewareSlideCanvas canvas,
        string userInstruction,
        CancellationToken cancellationToken = default);
}
