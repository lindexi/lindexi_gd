namespace DotNetCampus.Storage.StorageFiles;

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

    IStorageFileInfo CreateFile(StorageFileRelativePath relativePath);

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="relativePath"></param>
    void RemoveFile(StorageFileRelativePath relativePath);
}