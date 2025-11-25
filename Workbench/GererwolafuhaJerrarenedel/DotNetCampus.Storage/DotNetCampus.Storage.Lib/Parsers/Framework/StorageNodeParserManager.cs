using DotNetCampus.Storage.Parsers.NodeParsers;

namespace DotNetCampus.Storage.Parsers;

/// <summary>
/// 存储转换管理器
/// </summary>
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
    }

    public void Register(NodeParser nodeParser)
    {
        _typeNodeParserDictionary[nodeParser.TargetType] = nodeParser;
        if (nodeParser.TargetStorageName is not null)
        {
            _nameNodeParserDictionary[nodeParser.TargetStorageName] = nodeParser;
        }
    }

    private readonly Dictionary<Type, NodeParser> _typeNodeParserDictionary = [];
    private readonly Dictionary<string/*TargetStorageName*/, NodeParser> _nameNodeParserDictionary = [];

    public NodeParser GetNodeParser(Type targetType) =>
        _typeNodeParserDictionary[targetType];
    public NodeParser? GetNodeParser(string targetStorageName) =>
        _nameNodeParserDictionary.GetValueOrDefault(targetStorageName);
}