using System.Text.Json.Serialization;

namespace MiniMaxSdk.Images.Internal.Payloads;

internal sealed record ImageDataPayload(
    [property: JsonPropertyName("image_urls")] IReadOnlyList<string>? ImageUrls,
    [property: JsonPropertyName("image_base64")] IReadOnlyList<string>? ImageBase64);