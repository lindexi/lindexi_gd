using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeIpc;

/// <summary>Submits one complete preference transaction to a specific Host instance.</summary>
public sealed record ImeSaveProductSettingsRequest
    (Guid HostInstanceId, Guid OperationId, long ExpectedRevision, ImeProductSettings Settings);