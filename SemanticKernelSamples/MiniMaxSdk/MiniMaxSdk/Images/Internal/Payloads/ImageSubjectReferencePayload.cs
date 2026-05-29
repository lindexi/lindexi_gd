using System.Text.Json.Serialization;

namespace MiniMaxSdk.Images.Internal.Payloads;

internal sealed record ImageSubjectReferencePayload(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("image_file")] string ImageFile);