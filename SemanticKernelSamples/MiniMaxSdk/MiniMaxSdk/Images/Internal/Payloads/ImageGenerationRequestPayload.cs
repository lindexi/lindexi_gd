using System.Text.Json.Serialization;
using MiniMaxSdk.Images.Models;

namespace MiniMaxSdk.Images.Internal.Payloads;

internal sealed record ImageGenerationRequestPayload(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("prompt")] string Prompt,
    [property: JsonPropertyName("subject_reference")] IReadOnlyList<ImageSubjectReferencePayload>? SubjectReference,
    [property: JsonPropertyName("style")] StylePayload? Style,
    [property: JsonPropertyName("aspect_ratio")] string? AspectRatio,
    [property: JsonPropertyName("width")] int? Width,
    [property: JsonPropertyName("height")] int? Height,
    [property: JsonPropertyName("response_format")] string ResponseFormat,
    [property: JsonPropertyName("seed")] long? Seed,
    [property: JsonPropertyName("n")] int Count,
    [property: JsonPropertyName("prompt_optimizer")] bool PromptOptimizer,
    [property: JsonPropertyName("aigc_watermark")] bool? AigcWatermark)
{
    public static ImageGenerationRequestPayload FromRequest(MiniMaxImageGenerationRequest request)
    {
        return new ImageGenerationRequestPayload(
            request.Model,
            request.Prompt,
            null,
            request.Style is null ? null : new StylePayload(request.Style.StyleType, request.Style.StyleWeight),
            request.AspectRatio,
            request.Width,
            request.Height,
            request.ResponseFormat,
            request.Seed,
            request.Count,
            request.PromptOptimizer,
            request.AigcWatermark);
    }

    public static ImageGenerationRequestPayload FromRequest(MiniMaxImageToImageGenerationRequest request)
    {
        return new ImageGenerationRequestPayload(
            request.Model,
            request.Prompt,
            request.SubjectReferences.Select(static subjectReference => new ImageSubjectReferencePayload(subjectReference.Type, subjectReference.ImageFile)).ToArray(),
            request.Style is null ? null : new StylePayload(request.Style.StyleType, request.Style.StyleWeight),
            request.AspectRatio,
            request.Width,
            request.Height,
            request.ResponseFormat,
            request.Seed,
            request.Count,
            request.PromptOptimizer,
            request.AigcWatermark);
    }
}