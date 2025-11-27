namespace DotNetCampus.Storage.StorageFiles;

/// <summary>
/// 本地存储文件的信息
/// </summary>
public class LocalStorageFileInfo : IStorageFileInfo
{
    /// <summary>
    /// 相对路径
    /// </summary>
    public required StorageFileRelativePath RelativePath { get; init; }

    /// <summary>
    /// 实际文件
    /// </summary>
    public required FileInfo FileInfo { get; init; }

    internal HashCache? HashCache { get; set; }

    public Stream OpenRead()
    {
        return FileInfo.OpenRead();
    }

    public Stream OpenWrite()
    {
        if (!File.Exists(FileInfo.FullName))
        {
            FileInfo.Directory?.Create();
        }

        return FileInfo.OpenWrite();
    }
}