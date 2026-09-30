using System.Text;

namespace XiaoXiIme.Dictionary;

/// <summary>
/// First-party XiaoXiIme phonetic terms kept as a versioned TSV independent of SeWZC sources.
/// </summary>
public static class XiaoXiImeProjectTerms
{
    /// <summary>
    /// Gets the native source path written by conversion and required by default package compilation.
    /// </summary>
    public const string OutputRelativePath = "phonetic/xiaoxiime-project-terms.phonetic.tsv";

    internal const string ResourceName = "XiaoXiIme.Dictionary.xiaoxiime-project-terms.phonetic.tsv";

    /// <summary>
    /// Parses the canonical first-party term TSV.
    /// </summary>
    public static IReadOnlyList<PhoneticDictionaryEntry> Parse()
    {
        using var reader = new StreamReader(Open(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return PhoneticDictionarySourceParser.Parse(reader, OutputRelativePath);
    }

    /// <summary>
    /// Writes the canonical first-party term TSV to a native dictionary source path.
    /// </summary>
    public static void WriteTo(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("A project-term output path is required.", nameof(path));
        }

        var outputPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var source = Open();
        using var target = File.Create(outputPath);
        source.CopyTo(target);
    }

    private static Stream Open()
    {
        return typeof(XiaoXiImeProjectTerms).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded XiaoXiIme project-term source was not found: {ResourceName}");
    }
}
