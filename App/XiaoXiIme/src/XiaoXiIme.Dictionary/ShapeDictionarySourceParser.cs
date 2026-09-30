using System.Globalization;
using System.Text;

namespace XiaoXiIme.Dictionary;

/// <summary>
/// Parses XiaoXiIme shape TSV source files into normalized entries.
/// </summary>
public static class ShapeDictionarySourceParser
{
    public const int MaxLineLength = 16 * 1024;
    public const int MaxShapeCodeLength = 64;
    public const int MaxDecompositionLength = 256;

    /// <summary>
    /// Parses, normalizes, merges, and deterministically sorts shape source entries.
    /// </summary>
    public static IReadOnlyList<ShapeDictionaryEntry> Parse(TextReader reader, string filePath)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ValidateFilePath(filePath);

        var entries = new Dictionary<(string Text, string ShapeCode), string>();
        var lineNumber = 0;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            if (line.Length > MaxLineLength)
            {
                throw CreateException(filePath, lineNumber, DictionaryResources.LineTooLong);
            }

            if (string.IsNullOrWhiteSpace(line) || line.AsSpan().TrimStart().StartsWith(ImeDictionarySourceFormat.CommentMarker))
            {
                continue;
            }

            var columns = line.Split(ImeDictionarySourceFormat.ColumnSeparator);
            if (columns.Length != 3)
            {
                throw CreateException(filePath, lineNumber, DictionaryResources.InvalidColumnCount);
            }

            var text = columns[0].Trim();
            if (!IsSingleHanCharacter(text))
            {
                throw CreateException(filePath, lineNumber, DictionaryResources.InvalidShapeText);
            }

            var shapeCode = columns[1].Trim().ToLowerInvariant();
            if (shapeCode.Length is 0 or > MaxShapeCodeLength || !shapeCode.All(IsAsciiCodeCharacter))
            {
                throw CreateException(filePath, lineNumber, DictionaryResources.InvalidShapeCode);
            }

            var decomposition = columns[2].Trim();
            if (decomposition.Length is 0 or > MaxDecompositionLength)
            {
                throw CreateException(filePath, lineNumber, DictionaryResources.InvalidDecomposition);
            }

            entries[(text, shapeCode)] = decomposition;
        }

        return entries
            .Select(entry => new ShapeDictionaryEntry(entry.Key.Text, entry.Key.ShapeCode, entry.Value))
            .OrderBy(entry => entry.ShapeCode, StringComparer.Ordinal)
            .ThenBy(entry => entry.Text, StringComparer.Ordinal)
            .ThenBy(entry => entry.Decomposition, StringComparer.Ordinal)
            .ToArray();
    }

    private static void ValidateFilePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(nameof(filePath), nameof(filePath));
        }
    }

    private static bool IsAsciiCodeCharacter(char character)
    {
        return character is >= 'a' and <= 'z' or >= '0' and <= '9';
    }

    private static bool IsSingleHanCharacter(string text)
    {
        var runes = text.EnumerateRunes();
        if (!runes.MoveNext())
        {
            return false;
        }

        var rune = runes.Current;
        if (runes.MoveNext())
        {
            return false;
        }

        var value = rune.Value;
        return value is 0x3007
            or >= 0x3400 and <= 0x4DBF
            or >= 0x4E00 and <= 0x9FFF
            or >= 0xF900 and <= 0xFAFF
            or >= 0x20000 and <= 0x2A6DF
            or >= 0x2A700 and <= 0x2B81F
            or >= 0x2B820 and <= 0x2CEAF
            or >= 0x2CEB0 and <= 0x2EBEF
            or >= 0x2EBF0 and <= 0x2EE5F
            or >= 0x2F800 and <= 0x2FA1F
            or >= 0x30000 and <= 0x3134F
            or >= 0x31350 and <= 0x323AF;
    }

    private static DictionarySourceException CreateException(string filePath, int lineNumber, string reason)
    {
        return new DictionarySourceException(filePath, lineNumber, reason);
    }
}
