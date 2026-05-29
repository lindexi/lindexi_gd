using System.Text.Json;
using System.Text.Json.Serialization;
using MiniMaxSdk.Images.Internal.Payloads;
using MiniMaxSdk.Music.Internal.Payloads;

namespace MiniMaxSdk.Internal.Serialization;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ImageGenerationRequestPayload))]
[JsonSerializable(typeof(ImageGenerationResponsePayload))]
[JsonSerializable(typeof(ImageDataPayload))]
[JsonSerializable(typeof(MetadataPayload))]
[JsonSerializable(typeof(MiniMaxSdk.Images.Internal.Payloads.BaseResponsePayload))]
[JsonSerializable(typeof(StylePayload))]
[JsonSerializable(typeof(ImageSubjectReferencePayload))]
[JsonSerializable(typeof(MusicGenerationRequestPayload))]
[JsonSerializable(typeof(MusicGenerationResponsePayload))]
[JsonSerializable(typeof(MusicDataPayload))]
[JsonSerializable(typeof(MusicAudioSettingPayload))]
[JsonSerializable(typeof(LyricsGenerationRequestPayload))]
[JsonSerializable(typeof(LyricsGenerationResponsePayload))]
[JsonSerializable(typeof(CoverPreprocessRequestPayload))]
[JsonSerializable(typeof(CoverPreprocessResponsePayload))]
[JsonSerializable(typeof(MiniMaxSdk.Music.Internal.Payloads.BaseResponsePayload))]
internal sealed partial class MiniMaxJsonSerializerContext : JsonSerializerContext
{
}