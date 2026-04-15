namespace VirtualFileExplorer.Core;

public abstract class VirtualFileManager
{
    public abstract VirtualFolderInfo RootFolder { get; }

    /// <summary>
    /// 创建当前管理器的异步包装器。
    /// </summary>
    public VirtualFileManagerAsyncAdapter AsAsync()
    {
        return new VirtualFileManagerAsyncAdapter(this);
    }

    public abstract IReadOnlyList<VirtualFileInfo> GetFiles(VirtualFolderInfo folder);

    public abstract IReadOnlyList<VirtualFolderInfo> GetFolders(VirtualFolderInfo folder);

    public virtual IReadOnlyList<VirtualFileSystemEntry> GetEntries(VirtualFolderInfo folder)
    {
        ArgumentNullException.ThrowIfNull(folder);

        var entries = new List<VirtualFileSystemEntry>();
        entries.AddRange(GetFolders(folder));
        entries.AddRange(GetFiles(folder));
        return entries;
    }

    public abstract VirtualFolderInfo ResolveFolder(string folderPath, VirtualFolderInfo? currentFolder = null);

    public abstract void RenameEntry(VirtualFileSystemEntry entry, string newName);

    public abstract void DeleteEntry(VirtualFileSystemEntry entry);

    public abstract VirtualFileSystemEntry CopyEntry(VirtualFileSystemEntry entry, VirtualFolderInfo targetFolder);

    public abstract VirtualFileSystemEntry MoveEntry(VirtualFileSystemEntry entry, VirtualFolderInfo targetFolder);

    public abstract VirtualFolderInfo CreateFolder(VirtualFolderInfo parentFolder, string folderName);

    protected T AttachEntry<T>(T entry) where T : VirtualFileSystemEntry
    {
        ArgumentNullException.ThrowIfNull(entry);
        entry.Manager = this;
        return entry;
    }
}