using DotNetCampus.Storage.Lib.Logging;

namespace DotNetCampus.Storage.Lib.Documents;

/// <summary>
/// 复合的存储文档
/// </summary>
/// 文档里面存放了多个 <see cref="IStorageItem"/> 内容
/// 其中 <see cref="StorageFileItem"/> 提供了具体存储信息
/// 可以将其转换为具体的存储模型
public class CompoundStorageDocument
{
    public CompoundStorageDocument(List<IStorageItem> storageItemList, IReferencedFileManager referencedFileManager)
    {
        _storageItemList = storageItemList;
        ReferencedFileManager = referencedFileManager;
    }

    public IReadOnlyList<IStorageItem> StorageItemList => _storageItemList;
    private readonly List<IStorageItem> _storageItemList;

    public IReferencedFileManager ReferencedFileManager { get; }

    //public CompoundDocument ToCompoundDocument(StorageModel storageModel,
    //    CompoundDocumentStorageLogger? logger = null)
    //{
    //}
}