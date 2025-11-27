using DotNetCampus.Storage.Documents.StorageDocuments;
using DotNetCampus.Storage.Documents.StorageDocuments.StorageItems;
using DotNetCampus.Storage.Documents.StorageModels;
using DotNetCampus.Storage.StorageFiles;

namespace DotNetCampus.Storage.Documents.Converters;

/// <summary>
/// 存储模型与复合文档转换器基类
/// </summary>
public abstract class StorageModelToCompoundDocumentConverter : IStorageModelToCompoundDocumentConverter
{
    /// <summary>
    /// 创建存储模型与复合文档转换器基类
    /// </summary>
    protected StorageModelToCompoundDocumentConverter(CompoundStorageDocumentManager manager)
    {
        Manager = manager;
    }

    public CompoundStorageDocumentManager Manager { get; }

    public abstract StorageModel ToStorageModel(CompoundStorageDocument document);

    protected T? ReadRootSaveInfoProperty<T>(CompoundStorageDocument document, string relativePath)
    {
        if (document.StorageItemList.FirstOrDefault(t => t.RelativePath.Equals(relativePath)) is StorageNodeItem storageNodeItem)
        {
            return Manager.ParseToValue<T>(storageNodeItem.RootStorageNode);
        }

        return default;
    }

    public IEnumerable<T> ReadRootSaveInfoPropertyList<T>(CompoundStorageDocument document,
        Predicate<StorageFileRelativePath> relativePathPredicate)
    {
        foreach (var storageNodeItem in document.StorageItemList.OfType<StorageNodeItem>())
        {
            if (relativePathPredicate(storageNodeItem.RelativePath))
            {
                yield return Manager.ParseToValue<T>(storageNodeItem.RootStorageNode);
            }
        }
    }

    public abstract CompoundStorageDocument ToCompoundDocument(StorageModel model);
}