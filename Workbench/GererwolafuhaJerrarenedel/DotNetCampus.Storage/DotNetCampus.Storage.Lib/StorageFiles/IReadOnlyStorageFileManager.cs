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