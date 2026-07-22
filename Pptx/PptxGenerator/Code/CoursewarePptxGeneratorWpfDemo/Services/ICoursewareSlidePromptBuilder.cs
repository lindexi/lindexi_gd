using CoursewarePptxGenerator.Core.Models;
using CoursewarePptxGeneratorWpfDemo.Models;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Builds privacy-safe structured prompts for real courseware slides.
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
    /// Builds one structured page-generation request.
    /// </summary>
    CoursewareSlidePromptBuildResult Build(
        CoursewareSlidePromptSource source,
        int slideIndex,
        string userInstruction,
        bool screenshotAttached,
        CancellationToken cancellationToken = default);
}
