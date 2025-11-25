namespace DotNetCampus.Storage.StorageFiles;

/// <summary>
/// 本地文件的存储文件管理器
/// </summary>
public class LocalStorageFileManager : IStorageFileManager
{
    public LocalStorageFileManager(DirectoryInfo? workingDirectoryInfo = null)
    {
        WorkingDirectoryInfo = workingDirectoryInfo ?? new DirectoryInfo(Path.Join(Path.GetTempPath(), Path.GetRandomFileName()));
    }

    /// <summary>
    /// 工作路径。每个文档实例必须使用不同的工作路径
    /// </summary>
    public DirectoryInfo WorkingDirectoryInfo { get; init; }
}