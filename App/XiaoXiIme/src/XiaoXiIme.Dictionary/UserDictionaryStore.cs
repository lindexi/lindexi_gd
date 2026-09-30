using System.Text.Json;

namespace XiaoXiIme.Dictionary;

/// <summary>
/// Loads and atomically saves the user dictionary independently from the system package.
/// </summary>
public static class UserDictionaryStore
{
    private const int FormatVersion = 1;
    private const int MaxFileLength = 16 * 1024 * 1024;
    private const int MaxEntryCount = 100_000;
    /// <summary>
    /// Loads a user dictionary file, isolating invalid files for later diagnosis.
    /// </summary>
    public static UserDictionaryLoadResult Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException(DictionaryResources.InvalidUserDictionaryPath, nameof(path));
        }

        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            return new UserDictionaryLoadResult([], null, null);
        }

        try
        {
            var fileInfo = new FileInfo(fullPath);
            if (fileInfo.Length <= 0 || fileInfo.Length > MaxFileLength)
            {
                throw new InvalidDataException(DictionaryResources.InvalidUserDictionary);
            }

            using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var document = JsonSerializer.Deserialize(stream, UserDictionaryJsonSerializerContext.Default.UserDictionaryDocument);
            if (document is null || document.FormatVersion != FormatVersion || document.Entries is null || document.Entries.Length > MaxEntryCount)
            {
                throw new InvalidDataException(DictionaryResources.InvalidUserDictionary);
            }

            if (document.Entries.Any(entry =>
                    string.IsNullOrWhiteSpace(entry.Text) ||
                    string.IsNullOrWhiteSpace(entry.Reading) ||
                    entry.SelectionCount <= 0))
            {
                throw new InvalidDataException(DictionaryResources.InvalidUserDictionary);
            }

            return new UserDictionaryLoadResult(document.Entries, null, null);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            var isolatedPath = IsolateCorruptFile(fullPath);
            return new UserDictionaryLoadResult([], exception.Message, isolatedPath);
        }
    }

    /// <summary>
    /// Atomically saves a stable snapshot without replacing the old file until the new file is flushed.
    /// </summary>
    public static void Save(string path, IReadOnlyList<UserDictionaryEntry> entries)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException(DictionaryResources.InvalidUserDictionaryPath, nameof(path));
        }

        ArgumentNullException.ThrowIfNull(entries);
        if (entries.Count > MaxEntryCount)
        {
            throw new ArgumentOutOfRangeException(nameof(entries), DictionaryResources.UserDictionaryEntryCountTooLarge);
        }

        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath) ?? throw new ArgumentException(DictionaryResources.InvalidUserDictionaryPath, nameof(path));
        Directory.CreateDirectory(directory);
        var temporaryPath = fullPath + $".{Guid.NewGuid():N}.tmp";

        try
        {
            using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(
                    stream,
                    new UserDictionaryDocument(FormatVersion, entries.ToArray()),
                    UserDictionaryJsonSerializerContext.Default.UserDictionaryDocument);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static string? IsolateCorruptFile(string path)
    {
        try
        {
            var isolatedPath = path + $".corrupt-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
            File.Move(path, isolatedPath);
            return isolatedPath;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    internal sealed record UserDictionaryDocument(int FormatVersion, UserDictionaryEntry[] Entries);
}

/// <summary>
/// Reports loaded entries and any recovery action taken for an invalid user dictionary.
/// </summary>
public sealed record UserDictionaryLoadResult(
    IReadOnlyList<UserDictionaryEntry> Entries,
    string? Error,
    string? IsolatedCorruptFilePath);
