using CoursewarePptxGenerator.Core.Models;
using CoursewarePptxGeneratorWpfDemo.Models;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Produces the legacy read-only theme projection used by the current summary UI.
/// </summary>
public static class CoursewareDesignSystemThemeAdapter
{
    /// <summary>Creates a compatibility theme projection from a frozen design system.</summary>
    public static CoursewareTheme CreateTheme(CoursewareDesignSystem designSystem)
    {
        ArgumentNullException.ThrowIfNull(designSystem);
        var typography = designSystem.Typography.Tokens.OrderByDescending(token => token.FontSize).ToArray();
        var colors = designSystem.Colors.Tokens;
        var primaryCanvas = designSystem.CanvasProfiles.First(canvas => canvas.IsPrimary);
        return new CoursewareTheme
        {
            Title = designSystem.DesignIntent.Name,
            Summary = designSystem.DesignIntent.Summary,
            StyleKeywords = designSystem.DesignIntent.StyleKeywords,
            Colors = new CoursewareColorScheme
            {
                Primary = CreateColor(colors, ["primary", "brand", "accent"], "主色", "#2563EB"),
                Accent = CreateColor(colors, ["accent", "secondary", "highlight"], "强调色", "#F59E0B"),
                Background = CreateColor(colors, ["background", "page", "surface"], "背景色", "#FFFFFF"),
                PrimaryText = CreateColor(colors, ["text-primary", "title", "heading"], "主要文字", "#0F172A"),
                SecondaryText = CreateColor(colors, ["text-secondary", "muted", "caption"], "辅助文字", "#475569"),
                Rationale = designSystem.DesignIntent.Rationale,
            },
            Typography = new CoursewareTypography
            {
                PrimaryHeading = CreateTypography(typography, 0, "一级标题"),
                SecondaryHeading = CreateTypography(typography, 1, "二级标题"),
                Body = CreateTypography(typography, Math.Min(2, typography.Length - 1), "正文"),
                Supporting = CreateTypography(typography, typography.Length - 1, "辅助文字"),
            },
            Fonts = new CoursewareFontRecommendation
            {
                EastAsianHeading = typography.First().EastAsianFontStack.First(),
                EastAsianBody = typography.Last().EastAsianFontStack.First(),
                LatinHeading = typography.First().LatinFontStack.First(),
                LatinBody = typography.Last().LatinFontStack.First(),
            },
            SafeArea = new CoursewareSafeArea
            {
                Left = designSystem.Grid.SafeLeft,
                Top = designSystem.Grid.SafeTop,
                Right = designSystem.Grid.SafeRight,
                Bottom = designSystem.Grid.SafeBottom,
            },
            LayoutPrinciples = designSystem.Consistency.Invariants,
            PageTypes = CreatePageTypes(designSystem.PageTypes),
            ContentPresentationRules = designSystem.Accessibility.Rules,
            GenerationPromptSummary = $"执行设计系统 {designSystem.DesignSystemId}，主画布 {primaryCanvas.Width:0}×{primaryCanvas.Height:0}；页面生成必须消费强类型令牌、页面类型和模板。",
        };
    }

    private static CoursewareThemeColor CreateColor(
        IReadOnlyList<CoursewareColorToken> colors,
        IReadOnlyList<string> candidates,
        string usage,
        string fallback)
    {
        var color = colors.FirstOrDefault(token => candidates.Any(candidate => token.TokenId.Contains(candidate, StringComparison.OrdinalIgnoreCase)))
            ?? colors.FirstOrDefault();
        return new CoursewareThemeColor
        {
            Usage = usage,
            Name = color?.TokenId ?? usage,
            HexValue = color?.HexValue ?? fallback,
        };
    }

    private static CoursewareTypographyLevel CreateTypography(
        IReadOnlyList<CoursewareTypographyToken> typography,
        int index,
        string fallbackName)
    {
        var token = typography[Math.Clamp(index, 0, typography.Count - 1)];
        return new CoursewareTypographyLevel
        {
            Name = token.TokenId.Length == 0 ? fallbackName : token.TokenId,
            FontSize = token.FontSize,
            FontWeight = token.FontWeight,
            Purpose = token.Purpose,
        };
    }

    private static CoursewarePageTypeRecommendations CreatePageTypes(IReadOnlyList<CoursewarePageTypeContract> pageTypes)
    {
        return new CoursewarePageTypeRecommendations
        {
            Cover = CreatePageType(pageTypes, ["cover", "封面"], "封面"),
            Section = CreatePageType(pageTypes, ["section", "章节"], "章节页"),
            Content = CreatePageType(pageTypes, ["content", "内容", "正文"], "内容页"),
            Ending = CreatePageType(pageTypes, ["ending", "end", "结束"], "结束页"),
        };
    }

    private static CoursewarePageTypeRecommendation CreatePageType(
        IReadOnlyList<CoursewarePageTypeContract> pageTypes,
        IReadOnlyList<string> candidates,
        string fallbackName)
    {
        var pageType = pageTypes.FirstOrDefault(item => candidates.Any(candidate => item.PageTypeId.Contains(candidate, StringComparison.OrdinalIgnoreCase) || item.Name.Contains(candidate, StringComparison.OrdinalIgnoreCase)))
            ?? pageTypes.First();
        return new CoursewarePageTypeRecommendation { Name = pageType.Name.Length == 0 ? fallbackName : pageType.Name, Description = pageType.Purpose };
    }
}
