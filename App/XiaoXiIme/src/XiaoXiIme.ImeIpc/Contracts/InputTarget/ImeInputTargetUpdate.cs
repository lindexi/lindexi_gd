using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeIpc;

/// <summary>Ordered IMM activation/position observation. Deactivation preserves composition and identity.</summary>
public sealed record ImeInputTargetUpdate
(
    string OwnerId,
    long SequenceNumber,
    ImeSessionId SessionId,
    string ContextKey,
    bool Active,
    ImeCandidateAnchor? Anchor
);