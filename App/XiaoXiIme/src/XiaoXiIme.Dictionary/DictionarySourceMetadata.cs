using YamlDotNet.RepresentationModel;

namespace XiaoXiIme.Dictionary;

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