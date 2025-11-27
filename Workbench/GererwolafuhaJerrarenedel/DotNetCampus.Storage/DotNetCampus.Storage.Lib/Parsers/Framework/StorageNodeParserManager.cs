using DotNetCampus.Storage.Parsers.NodeParsers;
using DotNetCampus.Storage.Standard;

namespace DotNetCampus.Storage.Parsers;

/// <summary>
/// 存储转换管理器
/// </summary>
/// SaveInfo （或 SaveInfo 里面的子属性） -> StorageNode 
public class StorageNodeParserManager
{
    public StorageNodeParserManager()
    {
        // 基础类型
        Register(new BoolNodeParser());
        Register(new DoubleNodeParser());
        Register(new Int32NodeParser());
        Register(new Int64NodeParser());
        Register(new StringNodeParser());

        // 链接类型
        Register(new UriNodeParser<StorageUri>());
        Register(new UriNodeParser<FileUri>());
        Register(new UriNodeParser<IdUri>());
        Register(new HttpUriNodeParser());
    }

    public void Register(NodeParser nodeParser)
    {
        _typeNodeParserDictionary[nodeParser.TargetType] = nodeParser;
        if (nodeParser.TargetStorageName is not null)
        {
            _nameNodeParserDictionary[nodeParser.TargetStorageName] = nodeParser;
        }
    }

    /// <summary>
    /// 只针对列表的转换器
    /// </summary>
    public IStorageNodeListParser StorageNodeListParser { get; init; } = new DoubleLayerStorageNodeListParser();

    private readonly Dictionary<Type, NodeParser> _typeNodeParserDictionary = [];
    private readonly Dictionary<string/*TargetStorageName*/, NodeParser> _nameNodeParserDictionary = [];

    public NodeParser GetNodeParser(Type targetType) =>
        _typeNodeParserDictionary[targetType];

    internal NodeParser? TryGetNodeParser(Type targetType) =>
        _typeNodeParserDictionary.GetValueOrDefault(targetType);

    public NodeParser? GetNodeParser(string targetStorageName) =>
        _nameNodeParserDictionary.GetValueOrDefault(targetStorageName);
}