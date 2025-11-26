using DotNetCampus.Storage.StorageFiles;

namespace DotNetCampus.Storage.Documents.StorageDocuments;

public interface IReferencedManager
{
    /// <summary>
    /// 关联当前引用文件的文件管理器
    /// </summary>
    IStorageFileManager StorageFileManager { get; }

    void Reset(IReadOnlyCollection<ReferenceInfo>? references = null);

    IReadOnlyCollection<ReferenceInfo> References { get; }

    ReferenceInfo? GetReferenceInfo(StorageReferenceId referenceId);

    void AddReference(ReferenceInfo referenceInfo);

    ReferenceInfo AddReferenceFile(StorageReferenceId referenceId, IReadOnlyStorageFileInfo fileInfo);

    /// <summary>
    /// 加上引用计数
    /// </summary>
    /// <param name="referenceId"></param>
    void AddReferenceCount(StorageReferenceId referenceId);

    /// <summary>
    /// 减去引用计数
    /// </summary>
    /// <param name="referenceId"></param>
    void SubtractReferenceCount(StorageReferenceId referenceId);

    /// <summary>
    /// 将引用计数为 0 的资源进行清理
    /// </summary>
    void PruneReference();
}