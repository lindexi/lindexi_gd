using System.Text.Json.Serialization;

namespace MiniMaxSdk;

internal sealed record ImageDataPayload(
    [property: JsonPropertyName("image_urls")] IReadOnlyList<string>? ImageUrls,
    [property: JsonPropertyName("image_base64")] IReadOnlyList<string>? ImageBase64);