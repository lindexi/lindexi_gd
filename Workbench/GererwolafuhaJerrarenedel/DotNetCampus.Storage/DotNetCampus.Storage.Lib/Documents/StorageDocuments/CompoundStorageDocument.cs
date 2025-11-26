using DotNetCampus.Storage.Documents.StorageDocuments.StorageItems;
using DotNetCampus.Storage.StorageFiles;

namespace DotNetCampus.Storage.Documents.StorageDocuments;

/// <summary>
/// 复合的存储文档，表示一个暂时的状态
/// </summary>
/// 文档里面存放了多个 <see cref="IStorageItem"/> 内容
/// 其中 <see cref="StorageFileItem"/> 提供了具体存储信息
/// 可以将其转换为具体的存储模型
public class CompoundStorageDocument
{
    public CompoundStorageDocument(List<IStorageItem> storageItemList, CompoundStorageDocumentManager manager)
    {
        Manager = manager;
        _storageItemList = storageItemList;
    }

    public CompoundStorageDocumentManager Manager { get; }

    public IReadOnlyList<IStorageItem> StorageItemList => _storageItemList;
    private readonly List<IStorageItem> _storageItemList;

    public IReferencedFileManager ReferencedFileManager => Manager.ReferencedFileManager;

    public IStorageFileManager StorageFileManager => Manager.StorageFileManager;

    //public CompoundDocument ToCompoundDocument(StorageModel storageModel,
    //    CompoundDocumentStorageLogger? logger = null)
    //{
    //}
}