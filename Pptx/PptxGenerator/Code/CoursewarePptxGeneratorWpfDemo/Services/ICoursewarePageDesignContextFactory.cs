using CoursewarePptxGeneratorWpfDemo.Models;
using PptxGenerator.Models;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Creates the executable page design context consumed by slide-generation prompts.
/// </summary>
public interface ICoursewarePageDesignContextFactory
{
    /// <summary>
    /// Adapts the current whole-courseware analysis result to one page canvas.
    /// </summary>
    CoursewarePageDesignContext Create(
        CoursewareThemeAnalysisResult analysisResult,
        SlideDocumentContext slideCanvas);
}
