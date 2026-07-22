using CoursewarePptxGeneratorWpfDemo.Models;
using PptxGenerator.Models;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Adapts a validated whole-courseware theme to the coordinate system of one slide.
/// </summary>
public sealed class CoursewareThemePageDesignAdapter : ICoursewarePageDesignContextFactory
{
    private const double AspectRatioFallbackThreshold = 0.05;

    /// <summary>
    /// Creates the structured page design context for one slide canvas.
    /// </summary>
    /// <param name="analysisResult">The validated whole-courseware theme analysis result.</param>
    /// <param name="slideCanvas">The current slide document context.</param>
    /// <returns>The page design context scaled to the current canvas.</returns>
    public CoursewarePageDesignContext Adapt(
        CoursewareThemeAnalysisResult analysisResult,
        SlideDocumentContext slideCanvas)
    {
        ArgumentNullException.ThrowIfNull(analysisResult);
        ArgumentNullException.ThrowIfNull(slideCanvas);

        var theme = analysisResult.Theme;
        var diagnostics = CreateCanvasDiagnostics(analysisResult.ReferenceCanvas, slideCanvas);
        return new CoursewarePageDesignContext
        {
            Capability = "ThemeSuggestion",
            ThemeTitle = theme.Title,
            ThemeSummary = theme.Summary,
            StyleKeywords = theme.StyleKeywords,
            Colors = theme.Colors,
            Typography = new CoursewareTypography
            {
                PrimaryHeading = ScaleTypographyLevel(theme.Typography.PrimaryHeading, analysisResult.ReferenceCanvas, slideCanvas),
                SecondaryHeading = ScaleTypographyLevel(theme.Typography.SecondaryHeading, analysisResult.ReferenceCanvas, slideCanvas),
                Body = ScaleTypographyLevel(theme.Typography.Body, analysisResult.ReferenceCanvas, slideCanvas),
                Supporting = ScaleTypographyLevel(theme.Typography.Supporting, analysisResult.ReferenceCanvas, slideCanvas),
            },
            Fonts = theme.Fonts,
            SafeArea = CoursewareCanvasAdapter.ScaleSafeArea(
                theme.SafeArea,
                analysisResult.ReferenceCanvas,
                slideCanvas),
            LayoutPrinciples = theme.LayoutPrinciples,
            PageTypes = theme.PageTypes,
            ContentPresentationRules = theme.ContentPresentationRules,
            GenerationPromptSummary = theme.GenerationPromptSummary,
            ReferenceCanvasWidth = analysisResult.ReferenceCanvas.CanvasWidth,
            ReferenceCanvasHeight = analysisResult.ReferenceCanvas.CanvasHeight,
            CanvasWidth = slideCanvas.CanvasWidth,
            CanvasHeight = slideCanvas.CanvasHeight,
            Diagnostics = diagnostics,
        };
    }

    /// <inheritdoc />
    public CoursewarePageDesignContext Create(
        CoursewareThemeAnalysisResult analysisResult,
        SlideDocumentContext slideCanvas)
    {
        return Adapt(analysisResult, slideCanvas);
    }

    private static IReadOnlyList<string> CreateCanvasDiagnostics(
        SlideDocumentContext referenceCanvas,
        SlideDocumentContext slideCanvas)
    {
        var referenceAspectRatio = (double)referenceCanvas.CanvasWidth / referenceCanvas.CanvasHeight;
        var slideAspectRatio = (double)slideCanvas.CanvasWidth / slideCanvas.CanvasHeight;
        var relativeDifference = Math.Abs(slideAspectRatio / referenceAspectRatio - 1);
        if (relativeDifference <= AspectRatioFallbackThreshold)
        {
            return [];
        }

        return
        [
            $"CanvasProfileFallback: 当前页面比例 {slideCanvas.CanvasWidth}×{slideCanvas.CanvasHeight} 与主题参考画布 "
            + $"{referenceCanvas.CanvasWidth}×{referenceCanvas.CanvasHeight} 差异显著；应基于当前页面尺寸重新排版，不得静默套用参考画布版式。",
        ];
    }

    private static CoursewareTypographyLevel ScaleTypographyLevel(
        CoursewareTypographyLevel level,
        SlideDocumentContext referenceCanvas,
        SlideDocumentContext slideCanvas)
    {
        return level with
        {
            FontSize = CoursewareCanvasAdapter.ScaleFontSize(
                level.FontSize,
                referenceCanvas,
                slideCanvas),
        };
    }
}
