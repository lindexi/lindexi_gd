using System.Text.Json;
using System.Text.Json.Serialization;
using MiniMaxSdk.Images.Internal.Payloads;

namespace MiniMaxSdk.Images.Internal.Serialization;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ImageGenerationRequestPayload))]
[JsonSerializable(typeof(ImageGenerationResponsePayload))]
[JsonSerializable(typeof(ImageDataPayload))]
[JsonSerializable(typeof(MetadataPayload))]
[JsonSerializable(typeof(BaseResponsePayload))]
[JsonSerializable(typeof(StylePayload))]
internal sealed partial class MiniMaxImageJsonSerializerContext : JsonSerializerContext
{
}
