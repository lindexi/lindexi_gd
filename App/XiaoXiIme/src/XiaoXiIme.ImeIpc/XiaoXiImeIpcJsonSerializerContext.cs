using System.Text.Json.Serialization;
using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeIpc;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ImeCandidate))]
[JsonSerializable(typeof(ImeCandidate[]))]
[JsonSerializable(typeof(ImeCandidateWindowState))]
[JsonSerializable(typeof(ImeGuideline))]
[JsonSerializable(typeof(ImeGuidelineLevel))]
[JsonSerializable(typeof(CompositionText))]
[JsonSerializable(typeof(ImeSessionSnapshot))]
[JsonSerializable(typeof(ImeUiState))]
[JsonSerializable(typeof(ImeKey))]
[JsonSerializable(typeof(ImeKeyKind))]
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
[JsonSerializable(typeof(ImeSnapshotChangedNotification))]
[JsonSerializable(typeof(object))]
public sealed partial class XiaoXiImeIpcJsonSerializerContext : JsonSerializerContext
{

}
