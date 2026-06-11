using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace PptxGenerator;

public sealed class SlideRenderResult
{
    public required string InputXml { get; init; }

    public required string OutputXml { get; init; }

    public required IReadOnlyList<string> Warnings { get; init; }

    public required IReadOnlyList<string> Errors { get; init; }

    public required BitmapSource PreviewBitmap { get; init; }
}
