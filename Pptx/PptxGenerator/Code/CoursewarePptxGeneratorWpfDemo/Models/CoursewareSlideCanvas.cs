using PptxGenerator.Models;

namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Represents the single validated conversion from exported logical dimensions to rendering pixels.
/// </summary>
public sealed record CoursewareSlideCanvas
{
    /// <summary>
    /// Gets the original logical width from the courseware export.
    /// </summary>
    public required double LogicalWidth { get; init; }

    /// <summary>
    /// Gets the original logical height from the courseware export.
    /// </summary>
    public required double LogicalHeight { get; init; }

    /// <summary>
    /// Gets the immutable document context shared by prompting, layout, rendering, and UI.
    /// </summary>
    public required SlideDocumentContext DocumentContext { get; init; }

    /// <summary>
    /// Gets deterministic diagnostics produced while adapting the dimensions.
    /// </summary>
    public IReadOnlyList<string> Diagnostics { get; init; } = [];

    /// <summary>
    /// Gets the rendering width in pixels.
    /// </summary>
    public int PixelWidth => DocumentContext.CanvasWidth;

    /// <summary>
    /// Gets the rendering height in pixels.
    /// </summary>
    public int PixelHeight => DocumentContext.CanvasHeight;
}
