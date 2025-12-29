namespace DotNetCampus.Storage.StorageFiles;

/// <summary>
/// 表示单个存储文件的信息
/// </summary>
public interface IReadOnlyStorageFileInfo
{
    /// <summary>
    /// 文件相对路径
    /// </summary>
    StorageFileRelativePath RelativePath { get; }

    /// <summary>
    /// 打开用于读取的流
    /// </summary>
    /// <returns></returns>
    Stream OpenRead();

    ///// <summary>
    ///// 所在的存储文件管理器
    ///// </summary>
    //IStorageFileManager StorageFileManager { get; }
}