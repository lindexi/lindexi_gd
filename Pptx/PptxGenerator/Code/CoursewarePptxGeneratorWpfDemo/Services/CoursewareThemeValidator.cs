using CoursewarePptxGeneratorWpfDemo.Models;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Validates whole-courseware themes without invoking a language model.
/// </summary>
public sealed class CoursewareThemeValidator
{
    /// <summary>
    /// Validates the specified theme against the dominant slide dimensions.
    /// </summary>
    /// <param name="theme">The theme to validate.</param>
    /// <param name="slideWidth">The dominant slide width.</param>
    /// <param name="slideHeight">The dominant slide height.</param>
    /// <returns>The validation result.</returns>
    public CoursewareThemeValidationResult Validate(CoursewareTheme theme, double slideWidth, double slideHeight)
    {
        ArgumentNullException.ThrowIfNull(theme);

        var errors = new List<string>();
        ValidateRequiredText(theme, errors);
        ValidateCollections(theme, errors);
        ValidateColors(theme, errors);
        ValidateTypography(theme, errors);
        ValidateSafeArea(theme.SafeArea, slideWidth, slideHeight, errors);
        ValidatePageTypes(theme.PageTypes, errors);
        return new CoursewareThemeValidationResult { Errors = errors };
    }

    private static void ValidateRequiredText(CoursewareTheme theme, List<string> errors)
    {
        AddRequiredError(theme.Title, "Title", errors);
        AddRequiredError(theme.Summary, "Summary", errors);
        AddRequiredError(theme.GenerationPromptSummary, "GenerationPromptSummary", errors);
        AddRequiredError(theme.Colors.Rationale, "Colors.Rationale", errors);
        AddRequiredError(theme.Fonts.EastAsianHeading, "Fonts.EastAsianHeading", errors);
        AddRequiredError(theme.Fonts.EastAsianBody, "Fonts.EastAsianBody", errors);
        AddRequiredError(theme.Fonts.LatinHeading, "Fonts.LatinHeading", errors);
        AddRequiredError(theme.Fonts.LatinBody, "Fonts.LatinBody", errors);
    }

    private static void ValidateCollections(CoursewareTheme theme, List<string> errors)
    {
        if (theme.StyleKeywords.Count is < 3 or > 8)
        {
            errors.Add("StyleKeywords 必须包含 3 到 8 项。");
        }

        if (theme.LayoutPrinciples.Count is < 3 or > 8)
        {
            errors.Add("LayoutPrinciples 必须包含 3 到 8 项。");
        }

        if (theme.ContentPresentationRules.Count is < 2 or > 8)
        {
            errors.Add("ContentPresentationRules 必须包含 2 到 8 项。");
        }

        if (theme.StyleKeywords.Any(string.IsNullOrWhiteSpace))
        {
            errors.Add("StyleKeywords 不能包含空项。");
        }
    }

    private static void ValidateColors(CoursewareTheme theme, List<string> errors)
    {
        var colorRegex = new System.Text.RegularExpressions.Regex(
            "^#(?:[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        foreach (var color in theme.Colors.EnumerateColors())
        {
            AddRequiredError(color.Usage, $"Colors.{color.Name}.Usage", errors);
            AddRequiredError(color.Name, "Colors.Name", errors);
            if (!colorRegex.IsMatch(color.HexValue ?? string.Empty))
            {
                errors.Add($"颜色 {color.Name} 必须使用 #RRGGBB 或 #AARRGGBB 格式。");
            }
        }
    }

    private static void ValidateTypography(CoursewareTheme theme, List<string> errors)
    {
        var levels = theme.Typography.EnumerateLevels().ToArray();
        foreach (var level in levels)
        {
            AddRequiredError(level.Name, "Typography.Name", errors);
            AddRequiredError(level.FontWeight, $"Typography.{level.Name}.FontWeight", errors);
            AddRequiredError(level.Purpose, $"Typography.{level.Name}.Purpose", errors);
            if (level.FontSize is < 10 or > 96)
            {
                errors.Add($"字号 {level.Name} 必须在 10 到 96 之间。");
            }
        }

        if (levels[0].FontSize < levels[1].FontSize
            || levels[1].FontSize < levels[2].FontSize
            || levels[2].FontSize < levels[3].FontSize)
        {
            errors.Add("字体层级必须满足一级标题 >= 二级标题 >= 正文 >= 辅助文字。");
        }
    }

    private static void ValidateSafeArea(CoursewareSafeArea safeArea, double slideWidth, double slideHeight, List<string> errors)
    {
        if (safeArea.Left < 0 || safeArea.Top < 0 || safeArea.Right < 0 || safeArea.Bottom < 0)
        {
            errors.Add("SafeArea 的四个边距不能为负数。");
        }

        if (slideWidth <= 0 || slideHeight <= 0)
        {
            errors.Add("无法使用无效的页面尺寸校验 SafeArea。");
            return;
        }

        if (safeArea.Left + safeArea.Right >= slideWidth * 0.6)
        {
            errors.Add("SafeArea 左右边距之和不能超过页面宽度的 60%。");
        }

        if (safeArea.Top + safeArea.Bottom >= slideHeight * 0.6)
        {
            errors.Add("SafeArea 上下边距之和不能超过页面高度的 60%。");
        }
    }

    private static void ValidatePageTypes(CoursewarePageTypeRecommendations pageTypes, List<string> errors)
    {
        foreach (var recommendation in pageTypes.EnumerateRecommendations())
        {
            AddRequiredError(recommendation.Name, "PageTypes.Name", errors);
            AddRequiredError(recommendation.Description, $"PageTypes.{recommendation.Name}.Description", errors);
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