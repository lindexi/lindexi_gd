namespace VirtualFileExplorer.Core;

/// <summary>
/// 为同步的 <see cref="VirtualFileManager"/> 提供异步包装能力。
/// </summary>
public sealed class VirtualFileManagerAsyncAdapter
{
    private readonly VirtualFileManager _fileManager;

    public VirtualFileManagerAsyncAdapter(VirtualFileManager fileManager)
    {
        ArgumentNullException.ThrowIfNull(fileManager);
        _fileManager = fileManager;
    }

    /// <summary>
    /// 获取根目录。
    /// </summary>
    public Task<VirtualFolderInfo> GetRootFolderAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_fileManager.RootFolder);
    }

    /// <summary>
    /// 获取指定目录下的条目。
    /// </summary>
    public Task<IReadOnlyList<VirtualFileSystemEntry>> GetEntriesAsync(VirtualFolderInfo folder, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(folder);
        return RunAsync(() => _fileManager.GetEntries(folder), cancellationToken);
    }

    /// <summary>
    /// 获取指定目录下的文件。
    /// </summary>
    public Task<IReadOnlyList<VirtualFileInfo>> GetFilesAsync(VirtualFolderInfo folder, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(folder);
        return RunAsync(() => _fileManager.GetFiles(folder), cancellationToken);
    }

    /// <summary>
    /// 获取指定目录下的文件夹。
    /// </summary>
    public Task<IReadOnlyList<VirtualFolderInfo>> GetFoldersAsync(VirtualFolderInfo folder, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(folder);
        return RunAsync(() => _fileManager.GetFolders(folder), cancellationToken);
    }

    /// <summary>
    /// 解析目标目录。
    /// </summary>
    public Task<VirtualFolderInfo> ResolveFolderAsync(string folderPath, VirtualFolderInfo? currentFolder = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            throw new ArgumentException("目标文件夹路径不能为空。", nameof(folderPath));
        }

        return RunAsync(() => _fileManager.ResolveFolder(folderPath, currentFolder), cancellationToken);
    }

    /// <summary>
    /// 重命名条目。
    /// </summary>
    public Task RenameEntryAsync(VirtualFileSystemEntry entry, string newName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (string.IsNullOrWhiteSpace(newName))
        {
            throw new ArgumentException("新名称不能为空。", nameof(newName));
        }

        return RunAsync(() => _fileManager.RenameEntry(entry, newName), cancellationToken);
    }

    /// <summary>
    /// 删除条目。
    /// </summary>
    public Task DeleteEntryAsync(VirtualFileSystemEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return RunAsync(() => _fileManager.DeleteEntry(entry), cancellationToken);
    }

    /// <summary>
    /// 复制条目到目标目录。
    /// </summary>
    public Task<VirtualFileSystemEntry> CopyEntryAsync(VirtualFileSystemEntry entry, VirtualFolderInfo targetFolder, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(targetFolder);
        return RunAsync(() => _fileManager.CopyEntry(entry, targetFolder), cancellationToken);
    }

    /// <summary>
    /// 移动条目到目标目录。
    /// </summary>
    public Task<VirtualFileSystemEntry> MoveEntryAsync(VirtualFileSystemEntry entry, VirtualFolderInfo targetFolder, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(targetFolder);
        return RunAsync(() => _fileManager.MoveEntry(entry, targetFolder), cancellationToken);
    }

    /// <summary>
    /// 在目标目录下创建文件夹。
    /// </summary>
    public Task<VirtualFolderInfo> CreateFolderAsync(VirtualFolderInfo parentFolder, string folderName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parentFolder);
        if (string.IsNullOrWhiteSpace(folderName))
        {
            throw new ArgumentException("文件夹名称不能为空。", nameof(folderName));
        }

        return RunAsync(() => _fileManager.CreateFolder(parentFolder, folderName), cancellationToken);
    }

    private static Task RunAsync(Action action, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            action();
        }, cancellationToken);
    }

    private static Task<T> RunAsync<T>(Func<T> action, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return action();
        }, cancellationToken);
    }
}
