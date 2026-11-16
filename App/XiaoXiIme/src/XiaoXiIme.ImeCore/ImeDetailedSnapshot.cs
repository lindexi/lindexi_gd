using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeCore;

/// <summary>Exposes original character marks and the current interpretation page.</summary>
public sealed record ImeDetailedSnapshot(ImeSessionSnapshot Session, IReadOnlyList<bool> UppercaseMarks, ImeConversionPage Page, long Revision);