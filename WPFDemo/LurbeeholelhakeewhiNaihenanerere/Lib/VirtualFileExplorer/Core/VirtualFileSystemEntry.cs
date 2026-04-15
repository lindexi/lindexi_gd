using System.Runtime.Serialization;

namespace VirtualFileExplorer.Core;

public abstract class VirtualFileSystemEntry : NotifyObject
{
    private string _id;
    private string _name;
    private VirtualFolderInfo? _ownerFolder;
    private DateTimeOffset? _createdTime;
    private DateTimeOffset? _lastWriteTime;
    private DateTimeOffset? _lastAccessTime;
    private string _iconGlyph = string.Empty;
    private bool _canRename = true;
    private bool _canMove = true;
    private bool _canCopy = true;
    private bool _canDelete = true;

    protected VirtualFileSystemEntry(string id, string name, VirtualFolderInfo? ownerFolder)
    {
        _id = id;
        _name = name;
        _ownerFolder = ownerFolder;
    }

    public abstract VirtualFileSystemEntryType EntryType { get; }

    public VirtualFileManager? Manager { get; internal set; }

    [DataMember(Name = "id")]
    public string Id
    {
        get => _id;
        internal set => SetField(ref _id, value);
    }

    [DataMember(Name = "name")]
    public string Name
    {
        get => _name;
        internal set => SetField(ref _name, value);
    }

    public VirtualFolderInfo? OwnerFolder
    {
        get => _ownerFolder;
        internal set => SetField(ref _ownerFolder, value);
    }

    public DateTimeOffset? CreatedTime
    {
        get => _createdTime;
        internal set => SetField(ref _createdTime, value);
    }

    public DateTimeOffset? LastWriteTime
    {
        get => _lastWriteTime;
        internal set
        {
            if (SetField(ref _lastWriteTime, value))
            {
                OnPropertyChanged(nameof(DisplayModifiedTime));
            }
        }
    }

    public DateTimeOffset? LastAccessTime
    {
        get => _lastAccessTime;
        internal set => SetField(ref _lastAccessTime, value);
    }

    public bool CanRename
    {
        get => _canRename;
        internal set => SetField(ref _canRename, value);
    }

    public bool CanMove
    {
        get => _canMove;
        internal set => SetField(ref _canMove, value);
    }

    public bool CanCopy
    {
        get => _canCopy;
        internal set => SetField(ref _canCopy, value);
    }

    public bool CanDelete
    {
        get => _canDelete;
        internal set => SetField(ref _canDelete, value);
    }

    public string IconGlyph
    {
        get => _iconGlyph;
        internal set => SetField(ref _iconGlyph, value);
    }

    public virtual string DisplayType => EntryType == VirtualFileSystemEntryType.Folder ? "文件夹" : "文件";

    public virtual string DisplaySize => string.Empty;

    public string DisplayModifiedTime => LastWriteTime?.ToString("yyyy-MM-dd HH:mm") ?? string.Empty;

    public void Rename(string newName)
    {
        if (!CanRename)
        {
            throw new InvalidOperationException($"{Name} 不支持重命名。");
        }

        if (string.IsNullOrWhiteSpace(newName))
        {
            throw new ArgumentException("新名称不能为空。", nameof(newName));
        }

        EnsureManager().RenameEntry(this, newName);
    }

    public void Delete()
    {
        if (!CanDelete)
        {
            throw new InvalidOperationException($"{Name} 不支持删除。");
        }

        EnsureManager().DeleteEntry(this);
    }

    public VirtualFileSystemEntry CopyTo(VirtualFolderInfo targetFolder)
    {
        ArgumentNullException.ThrowIfNull(targetFolder);

        if (!CanCopy)
        {
            throw new InvalidOperationException($"{Name} 不支持复制。");
        }

        return EnsureManager().CopyEntry(this, targetFolder);
    }

    public VirtualFileSystemEntry MoveTo(VirtualFolderInfo targetFolder)
    {
        ArgumentNullException.ThrowIfNull(targetFolder);

        if (!CanMove)
        {
            throw new InvalidOperationException($"{Name} 不支持移动。");
        }

        return EnsureManager().MoveEntry(this, targetFolder);
    }

    private VirtualFileManager EnsureManager()
    {
        return Manager ?? throw new InvalidOperationException($"{Name} 未关联文件管理器。");
    }
}
