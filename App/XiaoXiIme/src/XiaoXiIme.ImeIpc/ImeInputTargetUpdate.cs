using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeIpc;

/// <summary>Ordered IMM activation/position observation. Deactivation preserves composition and identity.</summary>
public sealed record ImeInputTargetUpdate(
    string OwnerId,
    long SequenceNumber,
    ImeSessionId SessionId,
    string ContextKey,
    bool Active,
    ImeCandidateAnchor? Anchor);

/// <summary>Reports whether the observation superseded the owner's previous observation.</summary>
public sealed record ImeInputTargetUpdateResponse(bool Accepted);
