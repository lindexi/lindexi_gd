using System.Text.RegularExpressions;
using CoursewarePptxGeneratorWpfDemo.Models;
using PptxGenerator.Models;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Validates lightweight whole-courseware themes without invoking a language model.
/// </summary>
public sealed class CoursewareThemeValidator
{
    private static readonly Regex ColorRegex = new(
        "^#[0-9A-F]{6}(?:[0-9A-F]{2})?$",
        RegexOptions.CultureInvariant);

    private readonly ICoursewareThemeSlideMlValidator _slideMlValidator;

    /// <summary>
    /// Initializes a validator with the production SlideML validator.
    /// </summary>
    public CoursewareThemeValidator()
        : this(new CoursewareThemeSlideMlValidator())
    {
    }

    /// <summary>
    /// Initializes a validator with an injectable SlideML validator.
    /// </summary>
    /// <param name="slideMlValidator">The deep SlideML validator.</param>
    public CoursewareThemeValidator(ICoursewareThemeSlideMlValidator slideMlValidator)
    {
        ArgumentNullException.ThrowIfNull(slideMlValidator);
        _slideMlValidator = slideMlValidator;
    }

    /// <summary>
    /// Validates the specified lightweight theme.
    /// </summary>
    public CoursewareThemeValidationResult Validate(CoursewareTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        var errors = new List<string>();
        if (!string.Equals(theme.SchemaVersion, CoursewareTheme.CurrentSchemaVersion, StringComparison.Ordinal))
        {
            errors.Add($"SchemaVersion 必须为 {CoursewareTheme.CurrentSchemaVersion}。");
        }

        if (theme.ColorSuggestions is null || theme.ColorSuggestions.Count is < 3 or > 8)
        {
            errors.Add("ColorSuggestions 必须包含 3 到 8 项。");
        }

        AddRequiredError(theme.Style, "Style", errors);
        if (theme.Fonts is null)
        {
            errors.Add("Fonts 不能为空。");
        }
        else
        {
            AddRequiredError(theme.Fonts.Chinese, "Fonts.Chinese", errors);
            AddRequiredError(theme.Fonts.Western, "Fonts.Western", errors);
        }

        AddRequiredError(theme.SpacingAndVisualEffects, "SpacingAndVisualEffects", errors);
        AddRequiredError(theme.LayoutPrinciples, "LayoutPrinciples", errors);
        AddRequiredError(theme.CoverPageSlideMl, "CoverPageSlideMl", errors);
        AddRequiredError(theme.ContentPageSlideMl, "ContentPageSlideMl", errors);
        foreach (var color in theme.ColorSuggestions ?? [])
        {
            if (color is null)
            {
                errors.Add("ColorSuggestions 不能包含空项。");
                continue;
            }

            AddRequiredError(color.Name, "ColorSuggestions.Name", errors);
            AddRequiredError(color.Usage, "ColorSuggestions.Usage", errors);
            if (!ColorRegex.IsMatch(color.Hex ?? string.Empty))
            {
                errors.Add("ColorSuggestions.Hex 必须使用大写 #RRGGBB 或 #AARRGGBB 格式。");
            }
        }

        if (theme.SafeArea is null)
        {
            errors.Add("SafeArea 不能为空。");
        }
        else
        {
            ValidateRatio(theme.SafeArea.LeftRatio, "SafeArea.LeftRatio", errors);
            ValidateRatio(theme.SafeArea.TopRatio, "SafeArea.TopRatio", errors);
            ValidateRatio(theme.SafeArea.RightRatio, "SafeArea.RightRatio", errors);
            ValidateRatio(theme.SafeArea.BottomRatio, "SafeArea.BottomRatio", errors);
        }

        return new CoursewareThemeValidationResult { Errors = errors };
    }

    /// <summary>
    /// Validates theme fields and both complete SlideML documents.
    /// </summary>
    /// <param name="theme">The theme to validate.</param>
    /// <param name="documentContext">The first slide document context.</param>
    /// <param name="availableResourceIds">The resource identifiers available to the theme.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<CoursewareThemeValidationResult> ValidateAsync(
        CoursewareTheme theme,
        SlideDocumentContext documentContext,
        IReadOnlySet<string> availableResourceIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(theme);
        ArgumentNullException.ThrowIfNull(documentContext);
        ArgumentNullException.ThrowIfNull(availableResourceIds);

        var fieldResult = Validate(theme);
        var slideMlResult = await _slideMlValidator.ValidateAsync(theme, documentContext, availableResourceIds, cancellationToken).ConfigureAwait(false);
        if (fieldResult.IsValid)
        {
            return slideMlResult;
        }

        if (slideMlResult.IsValid)
        {
            return fieldResult;
        }

        var errors = new List<string>(fieldResult.Errors.Count + slideMlResult.Errors.Count);
        errors.AddRange(fieldResult.Errors);
        errors.AddRange(slideMlResult.Errors);
        return new CoursewareThemeValidationResult { Errors = errors };
    }

    private static void ValidateRatio(double value, string fieldName, List<string> errors)
    {
        if (!double.IsFinite(value) || value is < 0 or >= 0.5)
        {
            errors.Add($"{fieldName} 必须是 [0, 0.5) 范围内的有限值。");
        }
    }

    private static void AddRequiredError(string? value, string fieldName, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{fieldName} 不能为空。");
        }
    }
}