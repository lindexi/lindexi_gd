using System.Collections.Frozen;

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
    public static Task<T> ParseToValueAsync<T>(this CompoundStorageDocumentManager manager, StorageNode storageNode)
    {
        var parserManager = manager.ParserManager;
        var nodeParser = parserManager.GetNodeParser(typeof(T));
        var value = nodeParser.Parse(storageNode, new ParseNodeContext()
        {
            DocumentManager = manager
        });
        return Task.FromResult((T) value);
    }

    /// <summary>
    /// 从 <paramref name="value"/> 转换为存储节点
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="manager"></param>
    /// <param name="value"></param>
    /// <param name="nodeName"></param>
    /// <returns></returns>
    public static async Task<StorageNode> DeparseToStorageNodeAsync<T>(this CompoundStorageDocumentManager manager, T value, string? nodeName = null)
    {
        var parserManager = manager.ParserManager;
        var nodeParser = parserManager.GetNodeParser(typeof(T));

        var deparseNodeContext = new DeparseNodeContext()
        {
            NodeName = nodeName,
            DocumentManager = manager,
        };

        StorageNode storageNode = nodeParser.Deparse(value!, deparseNodeContext);

        var dictionary = deparseNodeContext.PostNodeParserTaskList.Where(t => t.IsDeparse).ToFrozenDictionary(t => t.StorageNode);

        if (dictionary.TryGetValue(storageNode, out PostAsyncNodeParserTaskInfo info))
        {
            var newStorageNode = await RunTaskInfo(info);
            return newStorageNode;
        }

        await RecursiveRunPostDeparser(storageNode.Children);

        return storageNode;

        async Task<StorageNode> RunTaskInfo(PostAsyncNodeParserTaskInfo taskInfo)
        {
            var parser = taskInfo.Parser;
            var newStorageNode = await parser.PostDeparseAsync(taskInfo.Value, taskInfo.StorageNode, deparseNodeContext);
            return newStorageNode;
        }

        async Task RecursiveRunPostDeparser(List<StorageNode>? storageNodeChildren)
        {
            // 递归执行解析器
            if (storageNodeChildren is null)
            {
                return;
            }

            for (var i = 0; i < storageNodeChildren.Count; i++)
            {
                var oldStorageNode = storageNodeChildren[i];
                if (dictionary.TryGetValue(oldStorageNode, out PostAsyncNodeParserTaskInfo taskInfo))
                {
                    var newStorageNode = await RunTaskInfo(taskInfo);
                    storageNodeChildren[i] = newStorageNode;
                }

                await RecursiveRunPostDeparser(storageNodeChildren[i].Children);
            }
        }
    }
}