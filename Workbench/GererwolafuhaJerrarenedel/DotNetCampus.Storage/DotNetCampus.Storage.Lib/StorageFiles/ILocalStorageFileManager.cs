namespace DotNetCampus.Storage.StorageFiles;

public interface ILocalStorageFileManager : IStorageFileManager
{
    /// <summary>
    /// 工作路径。每个文档实例必须使用不同的工作路径
    /// </summary>
    DirectoryInfo WorkingDirectoryInfo { get; }

    /// <summary>
    /// 清理已经不包含的文件
    /// </summary>
    void Prune();

    /// <summary>
    /// 尝试根据本地文件信息获取存储文件信息
    /// </summary>
    /// <param name="fileInfo"></param>
    /// <returns></returns>
    LocalStorageFileInfo? GetFile(FileInfo fileInfo);
}