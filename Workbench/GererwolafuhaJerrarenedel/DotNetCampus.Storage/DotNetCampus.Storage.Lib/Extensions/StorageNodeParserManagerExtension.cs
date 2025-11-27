using DotNetCampus.Storage.Parsers;
using DotNetCampus.Storage.Parsers.Contexts;
using DotNetCampus.Storage.StorageNodes;

namespace DotNetCampus.Storage;

/// <summary>
/// 存储转换管理器扩展
/// </summary>
public static class StorageNodeParserManagerExtension
{
    /// <summary>
    /// 从 <paramref name="storageNode"/> 转换为具体的类型值
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="manager"></param>
    /// <param name="storageNode"></param>
    /// <returns></returns>
    public static T ParseToValue<T>(this CompoundStorageDocumentManager manager, StorageNode storageNode)
    {
        var parserManager = manager.ParserManager;
        var nodeParser = parserManager.GetNodeParser(typeof(T));
        var value = nodeParser.Parse(storageNode, new ParseNodeContext()
        {
            DocumentManager = manager
        });
        return (T) value;
    }

    /// <summary>
    /// 从 <paramref name="value"/> 转换为存储节点
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="manager"></param>
    /// <param name="value"></param>
    /// <param name="nodeName"></param>
    /// <returns></returns>
    public static StorageNode DeparseToStorageNode<T>(this CompoundStorageDocumentManager manager, T value, string? nodeName = null)
    {
        var parserManager = manager.ParserManager;
        var nodeParser = parserManager.GetNodeParser(typeof(T));

        var storageNode = nodeParser.Deparse(value!, new DeparseNodeContext()
        {
            NodeName = nodeName,
            DocumentManager = manager,
        });
        return storageNode;
    }
}