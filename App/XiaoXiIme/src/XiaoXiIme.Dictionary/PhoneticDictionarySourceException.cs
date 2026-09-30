using System.Globalization;

namespace XiaoXiIme.Dictionary;

/// <summary>
/// Represents an invalid phonetic dictionary source line.
/// </summary>
public sealed class PhoneticDictionarySourceException : FormatException
{
    internal PhoneticDictionarySourceException(string filePath, int lineNumber, string reason)
        : base(string.Format(
            CultureInfo.CurrentCulture,
            DictionaryResources.InvalidPhoneticSourceLine,
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
