using YamlDotNet.RepresentationModel;

namespace XiaoXiIme.Dictionary;

/// <summary>
/// A native dictionary source with its YAML metadata and normalized entries.
/// </summary>
public sealed record DictionarySourceDocument<T>(DictionarySourceMetadata Metadata, IReadOnlyList<T> Entries);

/// <summary>
/// YAML source metadata. Additional fields remain available in the original mapping.
/// </summary>
public sealed record DictionarySourceMetadata(YamlMappingNode Fields)
{
    public string? Name => GetScalar("name");
    public string? Version => GetScalar("version");
    public string? Description => GetScalar("description");
    public string? Sort => GetScalar("sort");

    private string? GetScalar(string key) =>
        Fields.Children.TryGetValue(new YamlScalarNode(key), out var value)
            ? ((YamlScalarNode)value).Value
            : null;
}
