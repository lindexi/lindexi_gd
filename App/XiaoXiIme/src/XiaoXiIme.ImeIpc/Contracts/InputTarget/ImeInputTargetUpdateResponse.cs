namespace XiaoXiIme.ImeIpc;

/// <summary>Reports whether the observation superseded the owner's previous observation.</summary>
public sealed record ImeInputTargetUpdateResponse(bool Accepted);