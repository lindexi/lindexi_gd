using XiaoXiIme.Foundation;

namespace XiaoXiIme.Dictionary;

/// <summary>
/// Provides shape-code lookup and filtering independently from phonetic lookup.
/// </summary>
public interface IShapeDictionary
{
    /// <summary>
    /// Queries single-character entries whose shape code starts with the supplied code.
    /// </summary>
    IReadOnlyList<ShapeDictionaryEntry> QueryShape(string shapeCode, int maxCount = 9);

    /// <summary>
    /// Filters phonetic candidates by a shape-code prefix while preserving candidate order.
    /// </summary>
    IReadOnlyList<ImeCandidate> FilterByShape(
        IReadOnlyList<ImeCandidate> candidates,
        string shapeCode,
        int maxCount = 9);
}
