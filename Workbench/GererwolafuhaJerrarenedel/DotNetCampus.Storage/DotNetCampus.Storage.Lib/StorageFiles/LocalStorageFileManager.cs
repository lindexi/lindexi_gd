namespace DotNetCampus.Storage.StorageFiles;

/// <summary>
/// 本地文件的存储文件管理器
/// </summary>
public class LocalStorageFileManager : ILocalStorageFileManager
{
    public LocalStorageFileManager(DirectoryInfo? workingDirectoryInfo = null)
    {
        WorkingDirectoryInfo = workingDirectoryInfo ?? new DirectoryInfo(Path.Join(Path.GetTempPath(), Path.GetRandomFileName()));

        StringComparer stringComparer;
        if (OperatingSystem.IsWindows())
        {
            stringComparer = StringComparer.OrdinalIgnoreCase;
        }
        else
        {
            stringComparer = StringComparer.Ordinal;
        }

        AbsolutePathFileDictionary = new Dictionary<string, LocalStorageFileInfo>([], stringComparer);
    }

    /// <summary>
    /// 工作路径。每个文档实例必须使用不同的工作路径
    /// </summary>
    public DirectoryInfo WorkingDirectoryInfo { get; init; }

    public IReadOnlyCollection<IReadOnlyStorageFileInfo> FileList => FileDictionary.Values;

    private Dictionary<string /*RelativePath*/, IReadOnlyStorageFileInfo> FileDictionary { get; } =
        new Dictionary<string, IReadOnlyStorageFileInfo>([], StringComparer.OrdinalIgnoreCase);

    private Dictionary<string /*FullName*/, LocalStorageFileInfo> AbsolutePathFileDictionary { get; }


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
        AbsolutePathFileDictionary[localFileInfo.FullName] = result;
        return result;
    }

    public IReadOnlyStorageFileInfo? GetFile(StorageFileRelativePath relativePath)
    {
        return FileDictionary.GetValueOrDefault(relativePath.RelativePath);
    }

    public LocalStorageFileInfo? GetFile(FileInfo fileInfo)
    {
        return AbsolutePathFileDictionary.GetValueOrDefault(fileInfo.FullName);
    }

    public void AddFile(IReadOnlyStorageFileInfo fileInfo)
    {
        FileDictionary[fileInfo.RelativePath.RelativePath] = fileInfo;
        if (fileInfo is LocalStorageFileInfo localStorageFileInfo)
        {
            AbsolutePathFileDictionary[localStorageFileInfo.FileInfo.FullName] = localStorageFileInfo;
        }
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

        var folder = Path.Join(WorkingDirectoryInfo.FullName, localFileDirectory);
        Directory.CreateDirectory(folder);
        var localFilePath = Path.Join(folder, localFileName);
        var localFileInfo = new FileInfo(localFilePath);
        return localFileInfo;
    }

    public void RemoveFile(StorageFileRelativePath relativePath)
    {
        if (FileDictionary.Remove(relativePath.RelativePath, out IReadOnlyStorageFileInfo? fileInfo))
        {
            if (fileInfo is LocalStorageFileInfo localStorageFileInfo)
            {
                AbsolutePathFileDictionary.Remove(localStorageFileInfo.FileInfo.FullName);
            }
        }
    }

    /// <summary>
    /// 清理已经不包含的文件
    /// </summary>
    public void Prune()
    {
        var allFiles = WorkingDirectoryInfo.GetFiles("*", SearchOption.AllDirectories);
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