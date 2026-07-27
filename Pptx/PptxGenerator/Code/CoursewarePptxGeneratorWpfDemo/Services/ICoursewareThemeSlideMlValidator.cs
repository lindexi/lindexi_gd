using CoursewarePptxGeneratorWpfDemo.Models;
using PptxGenerator.Models;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Provides optional deep validation for the reference SlideML documents.
/// </summary>
public interface ICoursewareThemeSlideMlValidator
{
    /// <summary>
    /// Validates the SlideML documents contained in the specified theme.
    /// </summary>
    /// <param name="theme">The theme containing the cover and content SlideML documents.</param>
    /// <param name="documentContext">The first slide document context used for layout and rendering.</param>
    /// <param name="availableResourceIds">The logical resource identifiers available to the theme.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<CoursewareThemeValidationResult> ValidateAsync(
        CoursewareTheme theme,
        SlideDocumentContext documentContext,
        IReadOnlySet<string> availableResourceIds,
        CancellationToken cancellationToken = default);
}
