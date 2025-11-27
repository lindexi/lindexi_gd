using DotNetCampus.Storage.StorageFiles;

namespace DotNetCampus.Storage.Documents.StorageDocuments;

/// <summary>
/// 引用资源管理器
/// </summary>
public interface IReferencedManager
{
    /// <summary>
    /// 资源文件夹的相对路径。默认应该是 "Resources" 文件夹。当添加本地文件，调用 <see cref="AddLocalFile"/> 时，将会将其相对文件夹存放在此路径下
    /// </summary>
    StorageFileRelativePath ResourceFolderRelativePath => new StorageFileRelativePath("Resources");

    /// <summary>
    /// 关联当前引用文件的文件管理器
    /// </summary>
    IStorageFileManager StorageFileManager { get; }

    /// <summary>
    /// 重置清空引用。使用传入的引用集合进行初始化
    /// </summary>
    /// <param name="references"></param>
    void Reset(IReadOnlyCollection<ReferenceInfo>? references = null);

    /// <summary>
    /// 当前存在的引用列表
    /// </summary>
    IReadOnlyList<ReferenceInfo> References { get; }

    /// <summary>
    /// 获取引用信息
    /// </summary>
    /// <param name="referenceId"></param>
    /// <returns></returns>
    ReferenceInfo? GetReferenceInfo(StorageReferenceId referenceId);

    ///// <summary>
    ///// 添加引用
    ///// </summary>
    ///// <param name="referenceInfo"></param>
    //void AddReference(ReferenceInfo referenceInfo);

    /// <summary>
    /// 添加本地文件作为引用文件
    /// </summary>
    /// <param name="localFile"></param>
    /// <returns></returns>
    ReferenceInfo AddLocalFile(FileInfo localFile);

    /// <summary>
    /// 添加引用文件
    /// </summary>
    /// <param name="referenceId"></param>
    /// <param name="fileInfo"></param>
    /// <returns></returns>
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