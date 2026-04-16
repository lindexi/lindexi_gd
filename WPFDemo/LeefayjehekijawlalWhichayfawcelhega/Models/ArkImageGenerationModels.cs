using System.Text.Json.Serialization;

namespace LeefayjehekijawlalWhichayfawcelhega.Models;

internal sealed class ArkImageGenerationRequest
{
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    [JsonPropertyName("prompt")]
    public required string Prompt { get; init; }

    [JsonPropertyName("size")]
    public required string Size { get; init; }

    [JsonPropertyName("n")]
    public required int Count { get; init; }

    [JsonPropertyName("response_format")]
    public string ResponseFormat { get; init; } = "url";
}

internal sealed class ArkImageGenerationResponse
{
    [JsonPropertyName("data")]
    public List<ArkImageGenerationResponseItem> Data { get; init; } = [];
}

internal sealed class ArkImageGenerationResponseItem
{
    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("b64_json")]
    public string? Base64Content { get; init; }
}
