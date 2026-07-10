using System.Collections.Generic;

namespace PptxGenerator.Rendering;

internal sealed record McpSlideMlRenderResult
{
    public required string OutputXml { get; init; }

    public required IReadOnlyList<string> Warnings { get; init; }

    public required IReadOnlyList<string> Errors { get; init; }

    public required string PreviewImageFilePath { get; init; }
}