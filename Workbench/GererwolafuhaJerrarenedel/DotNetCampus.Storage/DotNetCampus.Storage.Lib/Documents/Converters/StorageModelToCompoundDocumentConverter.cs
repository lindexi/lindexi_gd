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

    public abstract Task<StorageModel> ToStorageModel(CompoundStorageDocument document);

    protected async Task<T?> ReadRootSaveInfoPropertyAsync<T>(CompoundStorageDocument document, string relativePath)
    {
        if (document.StorageItemList.FirstOrDefault(t => t.RelativePath.Equals(relativePath)) is StorageNodeItem storageNodeItem)
        {
            return await Manager.ParseToValueAsync<T>(storageNodeItem.RootStorageNode);
        }

        return default;
    }

    public async Task<List<T>> ReadRootSaveInfoPropertyListAsync<T>(CompoundStorageDocument document,
        Predicate<StorageFileRelativePath> relativePathPredicate)
    {
        var list = new List<T>();
        foreach (var storageNodeItem in document.StorageItemList.OfType<StorageNodeItem>())
        {
            if (relativePathPredicate(storageNodeItem.RelativePath))
            {
                var value = await Manager.ParseToValueAsync<T>(storageNodeItem.RootStorageNode);
                list.Add(value);
            }
        }

        return list;
    }

    public abstract Task<CompoundStorageDocument> ToCompoundDocument(StorageModel model);
}