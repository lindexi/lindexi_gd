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

    public IReadOnlyCollection<IReadOnlyStorageFileInfo> FileList => FileDictionary.Values;

    private Dictionary<string /*RelativePath*/, IReadOnlyStorageFileInfo> FileDictionary { get; } =
        new Dictionary<string, IReadOnlyStorageFileInfo>([], StringComparer.OrdinalIgnoreCase);

    public async Task<LocalStorageFileInfo> ToLocalStorageFileInfoAsync(IReadOnlyStorageFileInfo fileInfo)
    {
        if (fileInfo is LocalStorageFileInfo localStorageFileInfo)
        {
            // 先不管是否在当前空间管理内
            return localStorageFileInfo;
        }

        await using var readStream = fileInfo.OpenRead();

        var relativePath = fileInfo.RelativePath.AsSpan();

        var localFileInfo = CreateLocalFileInfo(relativePath);

        await using var fileStream = localFileInfo.Create();
        await readStream.CopyToAsync(fileStream);

        var result = new LocalStorageFileInfo
        {
            RelativePath = fileInfo.RelativePath,
            FileInfo = localFileInfo,
            //StorageFileManager = this,
        };

        FileDictionary[result.RelativePath.RelativePath] = result;
        return result;
    }

    public IReadOnlyStorageFileInfo? GetFile(StorageFileRelativePath relativePath)
    {
        return FileDictionary.GetValueOrDefault(relativePath.RelativePath);
    }

    public void AddFile(IReadOnlyStorageFileInfo fileInfo)
    {
        FileDictionary[fileInfo.RelativePath.RelativePath] = fileInfo;
    }

    public IStorageFileInfo CreateFile(StorageFileRelativePath relativePath)
    {
        var fileInfo = CreateLocalFileInfo(relativePath.AsSpan());

        var localStorageFileInfo = new LocalStorageFileInfo()
        {
            RelativePath = relativePath,
            FileInfo = fileInfo,
        };
        AddFile(localStorageFileInfo);
        return localStorageFileInfo;
    }

    private FileInfo CreateLocalFileInfo(ReadOnlySpan<char> relativePath)
    {
        var fileName = Path.GetFileName(relativePath);
        var localFileDirectory = Path.GetDirectoryName(relativePath);
        var extension = Path.GetExtension(fileName);
        var localFileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

        var localFileName = $"{localFileNameWithoutExtension}_{Path.GetRandomFileName()}{extension}";

        var localFilePath = Path.Join(WorkingDirectoryInfo.FullName, localFileDirectory, localFileName);
        var localFileInfo = new FileInfo(localFilePath);
        return localFileInfo;
    }

    public void RemoveFile(StorageFileRelativePath relativePath)
    {
        FileDictionary.Remove(relativePath.RelativePath);
    }

    /// <summary>
    /// 清理已经不包含的文件
    /// </summary>
    public void Prune()
    {
        var allFiles = WorkingDirectoryInfo.GetFiles("*",SearchOption.AllDirectories);
        foreach (var fileInfo in allFiles)
        {
            var relativePath = Path.GetRelativePath(WorkingDirectoryInfo.FullName, fileInfo.FullName);
            if (!FileDictionary.ContainsKey(relativePath))
            {
                fileInfo.Delete();
            }
        }
    }
}