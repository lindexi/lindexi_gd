using DotNetCampus.Storage.Parsers.Contexts;
using DotNetCampus.Storage.StorageNodes;

namespace DotNetCampus.Storage.Parsers;

/// <summary>
/// 包含双层内容的列表解析器
/// 如对 string[] 进行解析时，外层是列表节点，内层是 Item 节点
/// </summary>
public class DoubleLayerStorageNodeListParser : IStorageNodeListParser
{
    public const string DefaultItemPropertyName = "Item";

    public string ItemPropertyName { get; init; } = DefaultItemPropertyName;

    public IEnumerable<TElement> ParseElementOfList<TElement>(IReadOnlyList<StorageNode>? storageNodeChildren, ParseNodeContext context)
    {
        if (storageNodeChildren is null)
        {
            yield break;
        }

        StorageNodeParserManager parserManager = context.ParserManager;
        foreach (var storageNode in storageNodeChildren)
        {
            NodeParser? nodeParser = null;

            if (!storageNode.Name.IsEmptyOrNull)
            {
                var storageNodeName = storageNode.Name.ToText();

                if (storageNodeName.Equals(ItemPropertyName, StringComparison.Ordinal))
                {
                    // 如果名称是 Item 则跳过
                    // 不能取名称对应的解析器
                }
                else
                {
                    // 可能是泛型的类型内容
                    nodeParser = parserManager.GetNodeParser(storageNodeName);
                }
            }

            if (nodeParser != null && nodeParser.TargetType.IsAssignableTo(typeof(TElement)) is false)
            {
                // 这是特殊的情况，意味着刚好有名字没有匹配到正确类型的解析器
                // 比如某些过于通用的名称，如 Item 等
                nodeParser = null;
            }

            // 不存在名称的节点，比如 List<string> 等
            nodeParser ??= parserManager.TryGetNodeParser(typeof(TElement));

            if (nodeParser is not null)
            {
                var element = nodeParser.Parse(storageNode, context);

                if (element is TElement result)
                {
                    yield return result;
                }
            }
        }
    }

    public List<StorageNode> DeparseElementOfList(IEnumerable<object> children, DeparseNodeContext context)
    {
        StorageNodeParserManager parserManager = context.ParserManager;
        var storageNodeList = new List<StorageNode>();
        foreach (var child in children)
        {
            var nodeParser = parserManager.GetNodeParser(child.GetType());
            var childNode = nodeParser.Deparse(child, context);

            if (!string.IsNullOrEmpty(ItemPropertyName)
                && childNode.Name.IsEmptyOrNull)
            {
                childNode.Name = ItemPropertyName;
            }

            storageNodeList.Add(childNode);
        }

        return storageNodeList;
    }
}