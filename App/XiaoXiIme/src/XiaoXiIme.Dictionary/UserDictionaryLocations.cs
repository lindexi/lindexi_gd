namespace XiaoXiIme.Dictionary;

/// <summary>
/// Resolves the user-data location independently from the read-only installed dictionary package.
/// </summary>
public static class UserDictionaryLocations
{
    public const string FileName = "user-dictionary.json";

    public const string ApplicationDirectoryName = "XiaoXiIme";

    /// <summary>
    /// Returns the default per-user directory that stores learned entries.
    /// </summary>
    public static string GetDefaultDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            ApplicationDirectoryName);
    }

    /// <summary>
    /// Returns the default per-user dictionary file path.
    /// </summary>
    public static string GetDefaultFilePath()
    {
        return Path.Combine(GetDefaultDirectory(), FileName);
    }

    /// <summary>
    /// Deletes the user dictionary and isolated corrupt copies. Missing files are not an error.
    /// </summary>
    public static void Purge(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException(DictionaryResources.InvalidUserDictionaryPath, nameof(path));
        }

        var fullPath = Path.GetFullPath(path);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        var directory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
        {
            return;
        }

        var prefix = Path.GetFileName(fullPath) + ".corrupt-";
        foreach (var corruptPath in Directory.EnumerateFiles(directory, Path.GetFileName(fullPath) + ".corrupt-*"))
        {
            if (Path.GetFileName(corruptPath).StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(corruptPath);
            }
        }
    }
}
