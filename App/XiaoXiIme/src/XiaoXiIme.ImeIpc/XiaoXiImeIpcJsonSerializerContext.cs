using System.Text.Json.Serialization;
using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeIpc;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    Converters = [typeof(ImeSessionIdJsonConverter), typeof(JsonCamelCaseStringEnumConverter)])]
[JsonSerializable(typeof(ImeCandidate))]
[JsonSerializable(typeof(ImeCandidate[]))]
[JsonSerializable(typeof(ImeCandidateWindowState))]
[JsonSerializable(typeof(ImeGuideline))]
[JsonSerializable(typeof(ImeGuidelineLevel))]
[JsonSerializable(typeof(CompositionText))]
[JsonSerializable(typeof(ImeSessionSnapshot))]
[JsonSerializable(typeof(ImeUiState))]
[JsonSerializable(typeof(ImeAboutState))]
[JsonSerializable(typeof(ImeKey))]
[JsonSerializable(typeof(ImeKeyKind))]
[JsonSerializable(typeof(ImeSessionId))]
[JsonSerializable(typeof(ImeProcessResult))]
[JsonSerializable(typeof(ImeHostStatus))]
[JsonSerializable(typeof(ImeProcessKeyRequest))]
[JsonSerializable(typeof(ImeProcessKeyResponse))]
[JsonSerializable(typeof(ImeSnapshotRequest))]
[JsonSerializable(typeof(ImeSnapshotResponse))]
[JsonSerializable(typeof(ImeUiStateRequest))]
[JsonSerializable(typeof(ImeUiStateResponse))]
[JsonSerializable(typeof(ImeHostStatusRequest))]
[JsonSerializable(typeof(ImeHostStatusResponse))]
[JsonSerializable(typeof(ImeResetSessionRequest))]
[JsonSerializable(typeof(ImeResetSessionResponse))]
[JsonSerializable(typeof(ImeSetCompositionRequest))]
[JsonSerializable(typeof(ImeSetCompositionResponse))]
[JsonSerializable(typeof(ImeConversionListRequest))]
[JsonSerializable(typeof(ImeConversionListResponse))]
[JsonSerializable(typeof(ImeConversionListKind))]
[JsonSerializable(typeof(ImeRegisterWordRequest))]
[JsonSerializable(typeof(ImeRegisterWordResponse))]
[JsonSerializable(typeof(ImeRegisterWordEntry))]
[JsonSerializable(typeof(ImeRegisterWordEntry[]))]
[JsonSerializable(typeof(ImeRegisterWordAction))]
[JsonSerializable(typeof(ImeSnapshotChangedNotification))]
[JsonSerializable(typeof(object))]
public sealed partial class XiaoXiImeIpcJsonSerializerContext : JsonSerializerContext
{

}
