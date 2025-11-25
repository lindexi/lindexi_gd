namespace DotNetCampus.Storage.StorageFiles;

/// <summary>
/// 表示单个存储文件的信息
/// </summary>
public interface IStorageFileInfo : IReadOnlyStorageFileInfo
{
    /// <summary>
    /// 打开用于写入的流
    /// </summary>
    /// <returns></returns>
    Stream OpenWrite();
}

/// <summary>
/// 表示单个存储文件的信息
/// </summary>
public interface IReadOnlyStorageFileInfo
{
    /// <summary>
    /// 文件相对路径
    /// </summary>
    string RelativePath { get; init; }

    /// <summary>
    /// 打开用于读取的流
    /// </summary>
    /// <returns></returns>
    Stream OpenRead();
}