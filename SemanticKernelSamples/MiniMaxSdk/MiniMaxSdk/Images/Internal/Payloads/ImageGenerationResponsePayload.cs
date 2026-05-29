using System.Text.Json.Serialization;

namespace MiniMaxSdk.Images.Internal.Payloads;

internal sealed record ImageGenerationResponsePayload(
    [property: JsonPropertyName("data")] ImageDataPayload? Data,
    [property: JsonPropertyName("metadata")] MetadataPayload? Metadata,
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("base_resp")] BaseResponsePayload? BaseResp);