using DotNetCampus.Storage.Documents.StorageDocuments.StorageItems;
using DotNetCampus.Storage.StorageFiles;

namespace DotNetCampus.Storage.Documents.StorageDocuments;

/// <summary>
/// 复合的存储文档，表示一个暂时的状态
/// </summary>
/// <remarks>
/// 这是一个暂时的状态，可能来不及消费的时候，引用资源或文件资源将会被变更，导致文档无效
/// </remarks>
/// 文档里面存放了多个 <see cref="IStorageItem"/> 内容
/// 其中 <see cref="StorageNodeItem"/> 提供了具体存储信息
/// 可以将其转换为具体的存储模型
public class CompoundStorageDocument
{
    public CompoundStorageDocument(IReadOnlyList<IStorageItem> storageItemList, IReferencedManager referencedManager)
    {
        StorageItemList = storageItemList;
        ReferencedManager = referencedManager;
    }

    public IReferencedManager ReferencedManager { get; }
    public IStorageFileManager StorageFileManager => ReferencedManager.StorageFileManager;

    /// <summary>
    /// 这份文档包含的所有存储项。包含存储内容和资源内容
    /// </summary>
    public IReadOnlyList<IStorageItem> StorageItemList { get; }
}