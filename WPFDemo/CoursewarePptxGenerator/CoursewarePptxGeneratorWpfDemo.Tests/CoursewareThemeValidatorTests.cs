using CoursewarePptxGeneratorWpfDemo.Models;
using CoursewarePptxGeneratorWpfDemo.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoursewarePptxGeneratorWpfDemo.Tests;

[TestClass]
public sealed class CoursewareThemeValidatorTests
{
    [TestMethod(DisplayName = "完整有效的课件主题应通过校验")]
    public void ValidateShouldAcceptValidTheme()
    {
        var result = new CoursewareThemeValidator().Validate(CreateTheme(), 1280, 720);

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod(DisplayName = "非法颜色和倒置字号层级应同时返回校验错误")]
    public void ValidateShouldReturnAllColorAndTypographyErrors()
    {
        var theme = CreateTheme() with
        {
            Colors = CreateTheme().Colors with
            {
                Primary = CreateTheme().Colors.Primary with { HexValue = "blue" },
            },
            Typography = CreateTheme().Typography with
            {
                PrimaryHeading = CreateTypography("一级标题", 18),
                SecondaryHeading = CreateTypography("二级标题", 30),
            },
        };

        var result = new CoursewareThemeValidator().Validate(theme, 1280, 720);

        Assert.IsGreaterThanOrEqualTo(2, result.Errors.Count);
    }

    internal static CoursewareTheme CreateTheme()
    {
        return new CoursewareTheme
        {
            Title = "清晰课堂",
            Summary = "建立稳定清晰的课堂视觉秩序。",
            StyleKeywords = ["清晰", "克制", "现代"],
            Colors = new CoursewareColorScheme
            {
                Primary = CreateColor("主色", "教学蓝", "#2563EB"),
                Accent = CreateColor("强调色", "重点橙", "#F59E0B"),
                Background = CreateColor("背景", "雾白", "#F8FAFC"),
                PrimaryText = CreateColor("主文字", "深墨", "#0F172A"),
                SecondaryText = CreateColor("次文字", "石板灰", "#475569"),
                Rationale = "蓝色建立秩序，橙色突出结论。",
            },
            Typography = new CoursewareTypography
            {
                PrimaryHeading = CreateTypography("一级标题", 32),
                SecondaryHeading = CreateTypography("二级标题", 24),
                Body = CreateTypography("正文", 18),
                Supporting = CreateTypography("辅助文字", 14),
            },
            Fonts = new CoursewareFontRecommendation
            {
                EastAsianHeading = "微软雅黑",
                EastAsianBody = "微软雅黑",
                LatinHeading = "Arial",
                LatinBody = "Arial",
            },
            SafeArea = new CoursewareSafeArea { Left = 60, Top = 40, Right = 60, Bottom = 40 },
            LayoutPrinciples = ["稳定标题区", "突出单一焦点", "保持统一对齐"],
            PageTypes = new CoursewarePageTypeRecommendations
            {
                Cover = CreatePageType("封面页"),
                Section = CreatePageType("章节页"),
                Content = CreatePageType("内容页"),
                Ending = CreatePageType("结束页"),
            },
            ContentPresentationRules = ["公式逐步展开", "结论使用强调色"],
            GenerationPromptSummary = "使用教学蓝主色和重点橙强调，保持稳定标题区。",
        };
    }

    private static CoursewareThemeColor CreateColor(string usage, string name, string hexValue)
    {
        return new CoursewareThemeColor { Usage = usage, Name = name, HexValue = hexValue };
    }

    private static CoursewareTypographyLevel CreateTypography(string name, double size)
    {
        return new CoursewareTypographyLevel
        {
            Name = name,
            FontSize = size,
            FontWeight = "SemiBold",
            Purpose = "课堂展示",
        };
    }

    private static CoursewarePageTypeRecommendation CreatePageType(string name)
    {
        return new CoursewarePageTypeRecommendation { Name = name, Description = "保持清晰层级和适当留白。" };
    }
}