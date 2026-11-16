namespace XiaoXiIme.ImeCore;

/// <summary>Identifies a selectable semantic result within a context revision.</summary>
public readonly record struct ImeSelectionId(Guid ContextId, long Revision, int Ordinal);