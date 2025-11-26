using DotNetCampus.Storage.CompoundStorageDocumentManagers;
using DotNetCampus.Storage.Documents.StorageDocuments;
using DotNetCampus.Storage.Documents.StorageDocuments.StorageItems;
using DotNetCampus.Storage.Documents.StorageModels;
using DotNetCampus.Storage.Parsers.Contexts;
using DotNetCampus.Storage.StorageNodes;

namespace DotNetCampus.Storage.Documents.Converters;

/// <summary>
/// 存储模型与复合文档转换器基类
/// </summary>
public abstract class StorageModelToCompoundDocumentConverter : IStorageModelToCompoundDocumentConverter
{
    protected StorageModelToCompoundDocumentConverter(CompoundStorageDocumentManagerProvider provider)
    {
        _provider = provider;
    }

    private readonly CompoundStorageDocumentManagerProvider _provider;

    public CompoundStorageDocumentManager Manager => _provider.GetManager();

    public abstract StorageModel ToStorageModel(CompoundStorageDocument document);

    protected T ReadAsPropertyValue<T>(StorageNode storageNode)
    {
        var parserManager = Manager.ParserManager;
        var nodeParser = parserManager.GetNodeParser(typeof(T));
        var value = nodeParser.Parse(storageNode, new ParseNodeContext()
        {
            DocumentManager = Manager
        });
        return (T) value;
    }

    public abstract CompoundStorageDocument ToCompoundDocument(StorageModel model);

    protected StorageFileItem ToStorageFileItem<T>(T propertyValue, string relativePath, string? nodeName = null)
      where T : notnull
    {
        var parserManager = Manager.ParserManager;
        var nodeParser = parserManager.GetNodeParser(typeof(T));
        var storageNode = nodeParser.Deparse(propertyValue, new DeparseNodeContext()
        {
            NodeName = nodeName,
            DocumentManager = Manager,
        });

        return new StorageFileItem()
        {
            RootStorageNode = storageNode,
            RelativePath = relativePath
        };
    }
}