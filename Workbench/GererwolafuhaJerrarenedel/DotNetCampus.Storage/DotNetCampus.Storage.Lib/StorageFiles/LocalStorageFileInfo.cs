namespace DotNetCampus.Storage.StorageFiles;

/// <summary>
/// 本地存储文件的信息
/// </summary>
public class LocalStorageFileInfo : IStorageFileInfo
{
    /// <summary>
    /// 相对路径
    /// </summary>
    public required string RelativePath { get; init; }

    /// <summary>
    /// 实际文件
    /// </summary>
    public required FileInfo FileInfo { get; init; }

    public Stream OpenRead()
    {
        return FileInfo.OpenRead();
    }

    public Stream OpenWrite()
    {
        return FileInfo.OpenWrite();
    }
}