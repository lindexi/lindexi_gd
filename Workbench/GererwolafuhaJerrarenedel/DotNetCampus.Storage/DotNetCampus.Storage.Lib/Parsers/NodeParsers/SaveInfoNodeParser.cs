using DotNetCampus.Storage.Parsers.Contexts;
using DotNetCampus.Storage.SaveInfos;
using DotNetCampus.Storage.StorageNodes;

namespace DotNetCampus.Storage.Parsers.NodeParsers;

public abstract class SaveInfoNodeParser<T> : NodeParser<T>
{
    public abstract SaveInfoContractAttribute ContractAttribute { get; }

    public override string TargetStorageName => ContractAttribute.Name;

    protected void FillExtensionAndUnknownProperties(IReadOnlyList<StorageNode> list, SaveInfo saveInfo, in ParseNodeContext context)
    {
        var parserManager = context.ParserManager;
        List<StorageNode>? unknownProperties = null;

        foreach (var storageNode in list)
        {
            if (storageNode.StorageNodeType == StorageNodeType.Property)
            {
                // 如果明确标明是属性，那就是未知属性
                AddUnknownProperty(storageNode);
                continue;
            }

            var storageName = storageNode.Name.ToText();

            var extensionParser = parserManager.GetNodeParser(storageName);
            if (extensionParser is not null)
            {
                var extension = extensionParser.Parse(storageNode, in context);
                if (extension is SaveInfo extensionSaveInfo)
                {
                    saveInfo.Extensions.Add(extensionSaveInfo);
                }
                else
                {
                    // 那就是未知属性
                    AddUnknownProperty(storageNode);
                }
            }
            else
            {
                // 没有找到对应的解析器，那就是未知属性
                AddUnknownProperty(storageNode);
            }
        }

        if (unknownProperties is not null)
        {
            saveInfo.UnknownProperties = unknownProperties;
        }

        void AddUnknownProperty(StorageNode storageNode)
        {
            unknownProperties ??= new List<StorageNode>();
            unknownProperties.Add(storageNode);
        }
    }

    /// <summary>
    /// 是否能够匹配别名
    /// </summary>
    /// <param name="currentStorageName"></param>
    /// <param name="aliases"></param>
    /// <returns></returns>
    protected static bool IsMatchAliases(ReadOnlySpan<char> currentStorageName, string[]? aliases)
    {
        if (aliases is null)
        {
            return false;
        }

        foreach (var alias in aliases)
        {
            if (currentStorageName.Equals(alias, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 处理 List 等内部的元素的情况，支持泛型
    /// </summary>
    /// <param name="storageNodeChildren"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    protected IEnumerable<TElement> ParseElementOfList<TElement>(List<StorageNode>? storageNodeChildren, ParseNodeContext context)
    {
        if (storageNodeChildren is null)
        {
            yield break;
        }

        StorageNodeParserManager parserManager = context.ParserManager;
        foreach (var storageNode in storageNodeChildren)
        {
            NodeParser? nodeParser = null;

            if (storageNode.Name.IsEmptyOrNull)
            {
                // 不存在名称的节点，比如 List<string> 等
                nodeParser = parserManager.GetNodeParser(typeof(TElement));
            }
            else
            {
                var storageNodeName = storageNode.Name.ToText();
                nodeParser = parserManager.GetNodeParser(storageNodeName);
            }

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

    protected void AppendExtensionAndUnknownProperties(StorageNode storageNode, SaveInfo saveInfo, in DeparseNodeContext context)
    {
        var parserManager = context.ParserManager;
        foreach (var saveInfoExtension in saveInfo.Extensions)
        {
            var nodeParser = parserManager.GetNodeParser(saveInfoExtension.GetType());
            var extensionNode = nodeParser.Deparse(saveInfoExtension, context);
            storageNode.Children ??= new List<StorageNode>();
            storageNode.Children.Add(extensionNode);
        }

        if (saveInfo.UnknownProperties is {} unknownProperties)
        {
            foreach (var unknownProperty in unknownProperties)
            {
                storageNode.Children ??= new List<StorageNode>();
                storageNode.Children.Add(unknownProperty.Clone());
            }
        }
    }

    /// <summary>
    /// 处理列表元素的情况
    /// </summary>
    /// <param name="children"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    protected List<StorageNode> DeparseElementOfList(IEnumerable<object> children, DeparseNodeContext context)
    {
        StorageNodeParserManager parserManager = context.ParserManager;
        var storageNodeList = new List<StorageNode>();
        foreach (var child in children)
        {
            var nodeParser = parserManager.GetNodeParser(child.GetType());
            var childNode = nodeParser.Deparse(child, context);
            storageNodeList.Add(childNode);
        }

        return storageNodeList;
    }
}