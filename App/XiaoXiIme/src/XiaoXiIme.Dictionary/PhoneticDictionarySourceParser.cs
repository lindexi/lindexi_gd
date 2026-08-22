using System.Globalization;

namespace XiaoXiIme.Dictionary;

/// <summary>
/// Parses XiaoXiIme phonetic TSV source files into normalized entries.
/// </summary>
public static class PhoneticDictionarySourceParser
{
    public const int MaxLineLength = 16 * 1024;
    public const int MaxTextLength = 256;
    public const int MaxReadingLength = 512;

    /// <summary>
    /// Parses, normalizes, merges, and deterministically sorts phonetic source entries.
    /// </summary>
    public static IReadOnlyList<PhoneticDictionaryEntry> Parse(TextReader reader, string filePath)
    {
        ArgumentNullException.ThrowIfNull(reader);

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(nameof(filePath), nameof(filePath));
        }

        var entries = new Dictionary<(string Text, string Reading), int>();
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
            if (text.Length == 0)
            {
                throw CreateException(filePath, lineNumber, DictionaryResources.EmptyText);
            }

            if (text.Length > MaxTextLength)
            {
                throw CreateException(filePath, lineNumber, DictionaryResources.TextTooLong);
            }

            var reading = NormalizeReading(columns[1]);
            if (reading.Length == 0)
            {
                throw CreateException(filePath, lineNumber, DictionaryResources.EmptyReading);
            }

            if (reading.Length > MaxReadingLength)
            {
                throw CreateException(filePath, lineNumber, DictionaryResources.ReadingTooLong);
            }

            if (!IsValidReading(reading))
            {
                throw CreateException(filePath, lineNumber, DictionaryResources.InvalidReading);
            }

            if (!int.TryParse(columns[2].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var frequency))
            {
                throw CreateException(filePath, lineNumber, DictionaryResources.InvalidFrequency);
            }

            var key = (text, reading);
            if (!entries.TryGetValue(key, out var existingFrequency) || frequency > existingFrequency)
            {
                entries[key] = frequency;
            }
        }

        return entries
            .Select(entry => new PhoneticDictionaryEntry(entry.Key.Text, entry.Key.Reading, entry.Value))
            .OrderBy(entry => entry.Reading, StringComparer.Ordinal)
            .ThenByDescending(entry => entry.Frequency)
            .ThenBy(entry => entry.Text, StringComparer.Ordinal)
            .ToArray();
    }

    private static string NormalizeReading(string reading)
    {
        return string.Join(
            ' ',
            reading
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .Select(syllable => syllable.ToLowerInvariant()));
    }

    private static bool IsValidReading(string reading)
    {
        foreach (var character in reading)
        {
            if (character != ' ' && (character < 'a' || character > 'z'))
            {
                return false;
            }
        }

        return true;
    }

    private static PhoneticDictionarySourceException CreateException(string filePath, int lineNumber, string reason)
    {
        return new PhoneticDictionarySourceException(filePath, lineNumber, reason);
    }
}
