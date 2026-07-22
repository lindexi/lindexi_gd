using CoursewarePptxGenerator.Core.Models;

namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Holds the immutable courseware facts and one prepared privacy-safe analysis view used by page prompts.
/// </summary>
public sealed class CoursewareSlidePromptSource
{
    internal CoursewareSlidePromptSource(
        CoursewareInputPackage inputPackage,
        CoursewareThemeAnalysisResult analysisResult,
        CoursewareAnalysisEnvelope analysisEnvelope)
    {
        ArgumentNullException.ThrowIfNull(inputPackage);
        ArgumentNullException.ThrowIfNull(analysisResult);
        ArgumentNullException.ThrowIfNull(analysisEnvelope);
        InputPackage = inputPackage;
        AnalysisResult = analysisResult;
        AnalysisEnvelope = analysisEnvelope;
    }

    /// <summary>
    /// Gets the loaded courseware input package.
    /// </summary>
    public CoursewareInputPackage InputPackage { get; }

    /// <summary>
    /// Gets the validated theme analysis result.
    /// </summary>
    public CoursewareThemeAnalysisResult AnalysisResult { get; }

    internal CoursewareAnalysisEnvelope AnalysisEnvelope { get; }
}
