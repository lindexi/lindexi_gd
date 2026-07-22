using CoursewarePptxGenerator.Core.Models;
using CoursewarePptxGeneratorWpfDemo.Models;
using PptxGenerator.Models;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Converts exported slide dimensions and theme reference coordinates into page runtime coordinates.
/// </summary>
public static class CoursewareCanvasAdapter
{
    private const double DimensionPrecisionTolerance = 0.0001;

    /// <summary>
    /// Creates the single validated canvas adaptation for one exported slide.
    /// </summary>
    /// <param name="slide">The exported slide input.</param>
    /// <returns>The immutable canvas adaptation shared by the page workflow.</returns>
    public static CoursewareSlideCanvas CreateCanvas(CoursewareSlideInput slide)
    {
        ArgumentNullException.ThrowIfNull(slide);
        return CreateCanvas(slide.Width, slide.Height);
    }

    /// <summary>
    /// Creates the single validated canvas adaptation from source dimensions.
    /// </summary>
    /// <param name="width">The source logical width.</param>
    /// <param name="height">The source logical height.</param>
    /// <returns>The immutable canvas adaptation shared by the page workflow.</returns>
    public static CoursewareSlideCanvas CreateCanvas(double width, double height)
    {
        ValidateDimension(width, nameof(width));
        ValidateDimension(height, nameof(height));

        var pixelWidth = checked((int)Math.Round(width, MidpointRounding.AwayFromZero));
        var pixelHeight = checked((int)Math.Round(height, MidpointRounding.AwayFromZero));
        var diagnostics = new List<string>(2);
        if (Math.Abs(width - pixelWidth) > DimensionPrecisionTolerance)
        {
            diagnostics.Add($"CanvasDimensionRounded: 页面逻辑宽度 {width:0.####} 已统一取整为 {pixelWidth} 像素。");
        }

        if (Math.Abs(height - pixelHeight) > DimensionPrecisionTolerance)
        {
            diagnostics.Add($"CanvasDimensionRounded: 页面逻辑高度 {height:0.####} 已统一取整为 {pixelHeight} 像素。");
        }

        return new CoursewareSlideCanvas
        {
            LogicalWidth = width,
            LogicalHeight = height,
            DocumentContext = new SlideDocumentContext(pixelWidth, pixelHeight),
            Diagnostics = diagnostics.AsReadOnly(),
        };
    }

    /// <summary>
    /// Creates a validated integer document context for one exported slide.
    /// </summary>
    /// <param name="slide">The exported slide input.</param>
    /// <returns>The validated page document context.</returns>
    public static SlideDocumentContext CreateDocumentContext(CoursewareSlideInput slide)
    {
        return CreateCanvas(slide).DocumentContext;
    }

    /// <summary>
    /// Creates a validated integer document context from source dimensions.
    /// </summary>
    /// <param name="width">The source width.</param>
    /// <param name="height">The source height.</param>
    /// <returns>The validated page document context.</returns>
    public static SlideDocumentContext CreateDocumentContext(double width, double height)
    {
        return CreateCanvas(width, height).DocumentContext;
    }

    /// <summary>
    /// Scales a theme safe area from the analysis reference canvas to the current slide canvas.
    /// </summary>
    /// <param name="safeArea">The theme safe area.</param>
    /// <param name="referenceCanvas">The analysis reference canvas.</param>
    /// <param name="slideCanvas">The current slide canvas.</param>
    /// <returns>The scaled safe area.</returns>
    public static CoursewareSafeArea ScaleSafeArea(
        CoursewareSafeArea safeArea,
        SlideDocumentContext referenceCanvas,
        SlideDocumentContext slideCanvas)
    {
        ArgumentNullException.ThrowIfNull(safeArea);
        ArgumentNullException.ThrowIfNull(referenceCanvas);
        ArgumentNullException.ThrowIfNull(slideCanvas);

        var horizontalScale = (double)slideCanvas.CanvasWidth / referenceCanvas.CanvasWidth;
        var verticalScale = (double)slideCanvas.CanvasHeight / referenceCanvas.CanvasHeight;
        return new CoursewareSafeArea
        {
            Left = safeArea.Left * horizontalScale,
            Top = safeArea.Top * verticalScale,
            Right = safeArea.Right * horizontalScale,
            Bottom = safeArea.Bottom * verticalScale,
        };
    }

    /// <summary>
    /// Scales one theme font size from the analysis reference canvas to the current slide canvas.
    /// </summary>
    /// <param name="fontSize">The reference font size.</param>
    /// <param name="referenceCanvas">The analysis reference canvas.</param>
    /// <param name="slideCanvas">The current slide canvas.</param>
    /// <returns>The scaled font size.</returns>
    public static double ScaleFontSize(
        double fontSize,
        SlideDocumentContext referenceCanvas,
        SlideDocumentContext slideCanvas)
    {
        ArgumentNullException.ThrowIfNull(referenceCanvas);
        ArgumentNullException.ThrowIfNull(slideCanvas);

        var scale = Math.Min(
            (double)slideCanvas.CanvasWidth / referenceCanvas.CanvasWidth,
            (double)slideCanvas.CanvasHeight / referenceCanvas.CanvasHeight);
        return fontSize * scale;
    }

    private static void ValidateDimension(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value <= 0 || value > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "页面画布尺寸必须是可转换为正整数的有限值。");
        }

        var rounded = Math.Round(value, MidpointRounding.AwayFromZero);
        if (rounded <= 0 || rounded > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "页面画布尺寸取整后必须位于正整数范围内。");
        }
    }
}
