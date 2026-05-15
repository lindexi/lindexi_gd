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

public sealed class SlideGenerationIteration
{
    public required int Attempt { get; init; }

    public required string RequestPrompt { get; init; }

    public required string ModelResponse { get; init; }

    public required string SlideXml { get; init; }

    public required SlideRenderResult RenderResult { get; init; }
}

public sealed class SlideGenerationSessionResult
{
    public required string UserPrompt { get; init; }

    public required string FinalSlideXml { get; init; }

    public required SlideRenderResult FinalRenderResult { get; init; }

    public required IReadOnlyList<SlideGenerationIteration> Iterations { get; init; }

    public required IReadOnlyList<string> ConversationMessages { get; init; }
}
