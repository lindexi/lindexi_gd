using DotNetCampus.Storage.Lib.Parsers.Contexts;
using DotNetCampus.Storage.Lib.SaveInfos;
using DotNetCampus.Storage.Lib.StorageNodes;

namespace DotNetCampus.Storage.Lib.Parsers.NodeParsers;

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
}