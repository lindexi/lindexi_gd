using Avalonia.Media.Imaging;

using System.Collections.Generic;

namespace PptxGenerator;

public sealed class SlideRenderResult
{
    public required string InputXml { get; init; }

    public required string OutputXml { get; init; }

    public required IReadOnlyList<string> Warnings { get; init; }

    public required Bitmap PreviewBitmap { get; init; }
}
