using System.Globalization;

namespace XiaoXiIme.Dictionary;

/// <summary>
/// Represents an invalid XiaoXiIme dictionary source line.
/// </summary>
public sealed class DictionarySourceException : FormatException
{
    internal DictionarySourceException(string filePath, int lineNumber, string reason)
        : base(string.Format(
            CultureInfo.CurrentCulture,
            DictionaryResources.InvalidDictionarySourceLine,
            filePath,
            lineNumber,
            reason))
    {
        FilePath = filePath;
        LineNumber = lineNumber;
    }

    /// <summary>
    /// Gets the source file path supplied to the parser.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Gets the one-based source line number.
    /// </summary>
    public int LineNumber { get; }
}
