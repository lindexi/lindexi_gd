using System.Text.Json;
using System.Text.Json.Serialization;

namespace MiniMaxSdk;

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
