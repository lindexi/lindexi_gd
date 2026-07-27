using CoursewarePptxGeneratorWpfDemo.Models;
using CoursewarePptxGeneratorWpfDemo.Services;
using PptxGenerator.Models;

namespace CoursewarePptxGeneratorWpfDemo.Tests.Fakes;

internal sealed class FakeCoursewareThemeSlideMlValidator : ICoursewareThemeSlideMlValidator
{
    public CoursewareThemeValidationResult Result { get; init; } = new();
    public int CallCount { get; private set; }

    public Task<CoursewareThemeValidationResult> ValidateAsync(
        CoursewareTheme theme,
        SlideDocumentContext documentContext,
        IReadOnlySet<string> availableResourceIds,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CallCount++;
        return Task.FromResult(Result);
    }
}