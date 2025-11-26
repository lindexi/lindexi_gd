namespace DotNetCampus.Storage.StorageFiles;

/// <summary>
/// 只读存储文件管理器
/// </summary>
public interface IReadOnlyStorageFileManager
{
    /// <summary>
    /// 当前包含的所有文件列表
    /// </summary>
    IReadOnlyCollection<IReadOnlyStorageFileInfo> FileList { get; }

    /// <summary>
    /// 获取文件
    /// </summary>
    /// <param name="relativePath"></param>
    /// <returns></returns>
    IReadOnlyStorageFileInfo? GetFile(StorageFileRelativePath relativePath)
    {
        return FileList.FirstOrDefault(t => t.RelativePath == relativePath);
    }
}

/// <summary>
/// 存储文件管理器
/// </summary>
/// 由于文件可能是完全虚拟的，如在线的，此时就应该考虑有文件管理器的存在
public interface IStorageFileManager : IReadOnlyStorageFileManager
{
    /// <summary>
    /// 转换为本地存储文件
    /// </summary>
    /// <param name="fileInfo"></param>
    /// <returns></returns>
    Task<LocalStorageFileInfo> ToLocalStorageFileInfoAsync(IReadOnlyStorageFileInfo fileInfo);

    /// <summary>
    /// 添加文件
    /// </summary>
    /// <param name="fileInfo"></param>
    void AddFile(IReadOnlyStorageFileInfo fileInfo);

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="relativePath"></param>
    void RemoveFile(StorageFileRelativePath relativePath);
}