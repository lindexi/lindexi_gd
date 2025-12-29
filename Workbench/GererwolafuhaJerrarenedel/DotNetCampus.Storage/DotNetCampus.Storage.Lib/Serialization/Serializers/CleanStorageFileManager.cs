using DotNetCampus.Storage.StorageFiles;

namespace DotNetCampus.Storage.Serialization;

/// <summary>
/// 一个干净的存放文件信息的管理器，只存放被添加进去的文件信息
/// </summary>
internal class CleanStorageFileManager : IReadOnlyStorageFileManager
{
    public CleanStorageFileManager(IStorageFileManager backStorageFileManager)
    {
        BackStorageFileManager = backStorageFileManager;
    }

    public IStorageFileInfo CreateFile(StorageFileRelativePath relativePath)
    {
        var fileInfo = BackStorageFileManager.CreateFile(relativePath);
        AddFile(fileInfo);
        return fileInfo;
    }

    private List<IReadOnlyStorageFileInfo> FileList { get; } = [];

    /// <summary>
    /// 后备的文件存储管理器。实际的文件存储管理器
    /// </summary>
    public IStorageFileManager BackStorageFileManager { get; }

    IReadOnlyCollection<IReadOnlyStorageFileInfo> IReadOnlyStorageFileManager.FileList => FileList;

    public void AddFile(IReadOnlyStorageFileInfo fileInfo)
    {
        FileList.Add(fileInfo);
    } 
}