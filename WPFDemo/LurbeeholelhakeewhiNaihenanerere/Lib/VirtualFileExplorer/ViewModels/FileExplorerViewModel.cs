using System.Collections.ObjectModel;
using System.Windows.Input;
using VirtualFileExplorer.Core;

namespace VirtualFileExplorer.ViewModels;

public sealed class FileExplorerViewModel : NotifyObject
{
    private readonly RelayCommand _refreshCommand;
    private readonly RelayCommand _navigateUpCommand;
    private readonly RelayCommand _openEntryCommand;
    private readonly RelayCommand _switchToTileViewCommand;
    private readonly RelayCommand _switchToDetailsViewCommand;
    private VirtualFileManager? _fileManager;
    private VirtualFolderInfo? _currentFolder;
    private VirtualFileSystemEntry? _selectedEntry;
    private FileExplorerDisplayMode _displayMode = FileExplorerDisplayMode.Tile;
    private string _statusText = "未设置文件管理器";

    public FileExplorerViewModel()
    {
        _refreshCommand = new RelayCommand(_ => Refresh(), _ => FileManager is not null && CurrentFolder is not null);
        _navigateUpCommand = new RelayCommand(_ => NavigateUp(), _ => CurrentFolder?.UpperLevelFolder is not null);
        _openEntryCommand = new RelayCommand(OpenEntryInternal, parameter => parameter is VirtualFolderInfo || SelectedEntry is VirtualFolderInfo);
        _switchToTileViewCommand = new RelayCommand(_ => DisplayMode = FileExplorerDisplayMode.Tile, _ => DisplayMode != FileExplorerDisplayMode.Tile);
        _switchToDetailsViewCommand = new RelayCommand(_ => DisplayMode = FileExplorerDisplayMode.Details, _ => DisplayMode != FileExplorerDisplayMode.Details);
    }

    public ObservableCollection<VirtualFileSystemEntry> Entries { get; } = new();

    public ObservableCollection<RelativeFolderLink> Breadcrumbs { get; } = new();

    public ICommand RefreshCommand => _refreshCommand;

    public ICommand NavigateUpCommand => _navigateUpCommand;

    public ICommand OpenEntryCommand => _openEntryCommand;

    public ICommand SwitchToTileViewCommand => _switchToTileViewCommand;

    public ICommand SwitchToDetailsViewCommand => _switchToDetailsViewCommand;

    public VirtualFileManager? FileManager
    {
        get => _fileManager;
        private set => SetField(ref _fileManager, value);
    }

    public VirtualFolderInfo? CurrentFolder
    {
        get => _currentFolder;
        private set
        {
            if (SetField(ref _currentFolder, value))
            {
                OnPropertyChanged(nameof(CurrentFolderPath));
                RefreshCommandStates();
            }
        }
    }

    public string CurrentFolderPath => CurrentFolder?.Id ?? string.Empty;

    public VirtualFileSystemEntry? SelectedEntry
    {
        get => _selectedEntry;
        set
        {
            if (SetField(ref _selectedEntry, value))
            {
                OnPropertyChanged(nameof(CanRenameSelected));
                OnPropertyChanged(nameof(CanCopySelected));
                OnPropertyChanged(nameof(CanMoveSelected));
                OnPropertyChanged(nameof(CanDeleteSelected));
                RefreshCommandStates();
            }
        }
    }

    public FileExplorerDisplayMode DisplayMode
    {
        get => _displayMode;
        set
        {
            if (SetField(ref _displayMode, value))
            {
                OnPropertyChanged(nameof(IsTileView));
                OnPropertyChanged(nameof(IsDetailsView));
                RefreshCommandStates();
            }
        }
    }

    public bool IsTileView => DisplayMode == FileExplorerDisplayMode.Tile;

    public bool IsDetailsView => DisplayMode == FileExplorerDisplayMode.Details;

    public bool HasEntries => Entries.Count > 0;

    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }

    public bool CanCreateFolder => FileManager is not null && CurrentFolder is not null;

    public bool CanRenameSelected => SelectedEntry?.CanRename == true;

    public bool CanCopySelected => SelectedEntry?.CanCopy == true;

    public bool CanMoveSelected => SelectedEntry?.CanMove == true;

    public bool CanDeleteSelected => SelectedEntry?.CanDelete == true;

    public void SetFileManager(VirtualFileManager? manager)
    {
        FileManager = manager;
        SelectedEntry = null;
        Entries.Clear();
        Breadcrumbs.Clear();

        if (manager is null)
        {
            CurrentFolder = null;
            UpdateEntriesChangedState();
            StatusText = "未设置文件管理器";
            return;
        }

        NavigateToFolder(manager.RootFolder);
    }

    public void NavigateToFolder(VirtualFolderInfo folder)
    {
        ArgumentNullException.ThrowIfNull(folder);
        CurrentFolder = folder;
        Refresh();
    }

    public void Refresh()
    {
        if (FileManager is null)
        {
            SetFileManager(null);
            return;
        }

        var folder = CurrentFolder ?? FileManager.RootFolder;
        CurrentFolder = folder;

        var entries = FileManager.GetEntries(folder)
            .OrderBy(entry => entry.EntryType)
            .ThenBy(entry => entry.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        Entries.Clear();
        foreach (var entry in entries)
        {
            Entries.Add(entry);
        }

        Breadcrumbs.Clear();
        foreach (var breadcrumb in folder.GetBreadcrumbs())
        {
            Breadcrumbs.Add(breadcrumb);
        }

        UpdateEntriesChangedState();
        UpdateStatus(entries);
    }

    public void NavigateUp()
    {
        if (CurrentFolder?.UpperLevelFolder is null)
        {
            return;
        }

        NavigateToFolder(CurrentFolder.UpperLevelFolder);
    }

    public void OpenEntry(VirtualFileSystemEntry? entry)
    {
        if (entry is VirtualFolderInfo folder)
        {
            NavigateToFolder(folder);
        }
    }

    public void CreateFolder(string folderName)
    {
        if (FileManager is null || CurrentFolder is null)
        {
            throw new InvalidOperationException("当前没有可用的目录上下文。");
        }

        var createdFolder = FileManager.CreateFolder(CurrentFolder, folderName);
        Refresh();
        RestoreSelection(createdFolder.Id);
    }

    public void RenameSelected(string newName)
    {
        var entry = SelectedEntry ?? throw new InvalidOperationException("请先选择要重命名的项目。");
        entry.Rename(newName);
        Refresh();
        RestoreSelection(entry.Id);
    }

    public void CopySelected(string targetFolderPath)
    {
        var entry = SelectedEntry ?? throw new InvalidOperationException("请先选择要复制的项目。");
        if (FileManager is null)
        {
            throw new InvalidOperationException("未设置文件管理器。");
        }

        var targetFolder = FileManager.ResolveFolder(targetFolderPath, CurrentFolder);
        var copiedEntry = entry.CopyTo(targetFolder);
        Refresh();
        if (CurrentFolder is not null && string.Equals(CurrentFolder.Id, targetFolder.Id, StringComparison.OrdinalIgnoreCase))
        {
            RestoreSelection(copiedEntry.Id);
        }
    }

    public void MoveSelected(string targetFolderPath)
    {
        var entry = SelectedEntry ?? throw new InvalidOperationException("请先选择要移动的项目。");
        if (FileManager is null)
        {
            throw new InvalidOperationException("未设置文件管理器。");
        }

        var targetFolder = FileManager.ResolveFolder(targetFolderPath, CurrentFolder);
        var movedEntry = entry.MoveTo(targetFolder);
        Refresh();
        if (CurrentFolder is not null && string.Equals(CurrentFolder.Id, targetFolder.Id, StringComparison.OrdinalIgnoreCase))
        {
            RestoreSelection(movedEntry.Id);
        }
    }

    public void DeleteSelected()
    {
        var entry = SelectedEntry ?? throw new InvalidOperationException("请先选择要删除的项目。");
        entry.Delete();
        SelectedEntry = null;
        Refresh();
    }

    private void OpenEntryInternal(object? parameter)
    {
        OpenEntry(parameter as VirtualFileSystemEntry ?? SelectedEntry);
    }

    private void RestoreSelection(string entryId)
    {
        SelectedEntry = Entries.FirstOrDefault(entry => string.Equals(entry.Id, entryId, StringComparison.OrdinalIgnoreCase));
    }

    private void UpdateStatus(IReadOnlyCollection<VirtualFileSystemEntry> entries)
    {
        var folderCount = entries.Count(entry => entry is VirtualFolderInfo);
        var fileCount = entries.Count - folderCount;
        StatusText = $"当前目录：{CurrentFolderPath}    文件夹：{folderCount}    文件：{fileCount}";
        RefreshCommandStates();
    }

    private void UpdateEntriesChangedState()
    {
        OnPropertyChanged(nameof(HasEntries));
        OnPropertyChanged(nameof(CanCreateFolder));
        OnPropertyChanged(nameof(CanRenameSelected));
        OnPropertyChanged(nameof(CanCopySelected));
        OnPropertyChanged(nameof(CanMoveSelected));
        OnPropertyChanged(nameof(CanDeleteSelected));
    }

    private void RefreshCommandStates()
    {
        _refreshCommand.RaiseCanExecuteChanged();
        _navigateUpCommand.RaiseCanExecuteChanged();
        _openEntryCommand.RaiseCanExecuteChanged();
        _switchToTileViewCommand.RaiseCanExecuteChanged();
        _switchToDetailsViewCommand.RaiseCanExecuteChanged();
        OnPropertyChanged(nameof(CanCreateFolder));
    }
}
