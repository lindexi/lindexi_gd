using CoursewarePptxGenerator.Core.Models;

namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Holds the immutable courseware input and validated theme result used by page prompts.
/// </summary>
public sealed class CoursewareSlidePromptSource
{
    internal CoursewareSlidePromptSource(
        CoursewareInputPackage inputPackage,
        CoursewareTheme theme)
    {
        ArgumentNullException.ThrowIfNull(inputPackage);
        ArgumentNullException.ThrowIfNull(theme);
        InputPackage = inputPackage;
        Theme = theme;
    }

    /// <summary>
    /// Gets the loaded courseware input package.
    /// </summary>
    public CoursewareInputPackage InputPackage { get; }

    /// <summary>
    /// Gets the complete original Theme 2.0 result.
    /// </summary>
    public CoursewareTheme Theme { get; }
}