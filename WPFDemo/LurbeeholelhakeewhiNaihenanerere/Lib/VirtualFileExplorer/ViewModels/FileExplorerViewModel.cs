using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;
using VirtualFileExplorer.Core;

namespace VirtualFileExplorer.ViewModels;

public sealed class FileExplorerViewModel : NotifyObject
{
    private readonly AsyncRelayCommand _refreshCommand;
    private readonly AsyncRelayCommand _navigateUpCommand;
    private readonly AsyncRelayCommand _openEntryCommand;
    private readonly AsyncRelayCommand _navigateBreadcrumbCommand;
    private readonly RelayCommand _switchToTileViewCommand;
    private readonly RelayCommand _switchToDetailsViewCommand;
    private readonly List<VirtualFileSystemEntry> _sourceEntries = new();
    private VirtualFileManager? _fileManager;
    private VirtualFileManagerAsyncAdapter? _fileManagerAsync;
    private VirtualFolderInfo? _currentFolder;
    private VirtualFileSystemEntry? _selectedEntry;
    private FileExplorerDisplayMode _displayMode = FileExplorerDisplayMode.Tile;
    private string _statusText = "未设置文件管理器";
    private string _searchText = string.Empty;
    private FileExplorerEntryFilter _entryFilter = FileExplorerEntryFilter.All;
    private FileExplorerSortField _sortField = FileExplorerSortField.Name;
    private ListSortDirection _sortDirection = ListSortDirection.Ascending;
    private SelectionMode _selectionMode = SelectionMode.Extended;
    private bool _isBusy;

    public FileExplorerViewModel()
    {
        _refreshCommand = new AsyncRelayCommand(_ => RefreshAsync(), _ => FileManager is not null && CurrentFolder is not null);
        _navigateUpCommand = new AsyncRelayCommand(_ => NavigateUpAsync(), _ => CurrentFolder?.UpperLevelFolder is not null && !IsBusy);
        _openEntryCommand = new AsyncRelayCommand(OpenEntryInternalAsync, parameter => parameter is VirtualFolderInfo || SelectedEntry is VirtualFolderInfo);
        _navigateBreadcrumbCommand = new AsyncRelayCommand(NavigateBreadcrumbAsync, parameter => parameter is RelativeFolderLink && !IsBusy);
        _switchToTileViewCommand = new RelayCommand(_ => DisplayMode = FileExplorerDisplayMode.Tile, _ => DisplayMode != FileExplorerDisplayMode.Tile);
        _switchToDetailsViewCommand = new RelayCommand(_ => DisplayMode = FileExplorerDisplayMode.Details, _ => DisplayMode != FileExplorerDisplayMode.Details);

        FilterOptions = new ReadOnlyCollection<ExplorerOption<FileExplorerEntryFilter>>(
            new[]
            {
                new ExplorerOption<FileExplorerEntryFilter>(FileExplorerEntryFilter.All, "全部"),
                new ExplorerOption<FileExplorerEntryFilter>(FileExplorerEntryFilter.FoldersOnly, "仅文件夹"),
                new ExplorerOption<FileExplorerEntryFilter>(FileExplorerEntryFilter.FilesOnly, "仅文件"),
            });

        SortOptions = new ReadOnlyCollection<ExplorerOption<FileExplorerSortField>>(
            new[]
            {
                new ExplorerOption<FileExplorerSortField>(FileExplorerSortField.Name, "按名称"),
                new ExplorerOption<FileExplorerSortField>(FileExplorerSortField.Type, "按类型"),
                new ExplorerOption<FileExplorerSortField>(FileExplorerSortField.Size, "按大小"),
                new ExplorerOption<FileExplorerSortField>(FileExplorerSortField.ModifiedTime, "按修改时间"),
                new ExplorerOption<FileExplorerSortField>(FileExplorerSortField.CreatedTime, "按创建时间"),
            });
    }

    public ObservableCollection<VirtualFileSystemEntry> Entries { get; } = new();

    public ObservableCollection<VirtualFileSystemEntry> SelectedEntries { get; } = new();

    public ObservableCollection<RelativeFolderLink> Breadcrumbs { get; } = new();

    public IReadOnlyList<ExplorerOption<FileExplorerEntryFilter>> FilterOptions { get; }

    public IReadOnlyList<ExplorerOption<FileExplorerSortField>> SortOptions { get; }

    public ICommand RefreshCommand => _refreshCommand;

    public ICommand NavigateUpCommand => _navigateUpCommand;

    public ICommand OpenEntryCommand => _openEntryCommand;

    public ICommand NavigateBreadcrumbCommand => _navigateBreadcrumbCommand;

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

    public SelectionMode SelectionMode
    {
        get => _selectionMode;
        set
        {
            if (SetField(ref _selectionMode, value))
            {
                OnPropertyChanged(nameof(IsMultiSelectionEnabled));
            }
        }
    }

    public bool IsMultiSelectionEnabled => SelectionMode != SelectionMode.Single;

    public bool IsTileView => DisplayMode == FileExplorerDisplayMode.Tile;

    public bool IsDetailsView => DisplayMode == FileExplorerDisplayMode.Details;

    public bool HasEntries => Entries.Count > 0;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetField(ref _isBusy, value))
            {
                RefreshCommandStates();
            }
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetField(ref _searchText, value))
            {
                ApplyEntriesView();
            }
        }
    }

    public FileExplorerEntryFilter EntryFilter
    {
        get => _entryFilter;
        set
        {
            if (SetField(ref _entryFilter, value))
            {
                ApplyEntriesView();
            }
        }
    }

    public FileExplorerSortField SortField
    {
        get => _sortField;
        set
        {
            if (SetField(ref _sortField, value))
            {
                ApplyEntriesView();
            }
        }
    }

    public ListSortDirection SortDirection
    {
        get => _sortDirection;
        set
        {
            if (SetField(ref _sortDirection, value))
            {
                OnPropertyChanged(nameof(IsSortAscending));
                ApplyEntriesView();
            }
        }
    }

    public bool IsSortAscending => SortDirection == ListSortDirection.Ascending;

    public bool CanCreateFolder => FileManager is not null && CurrentFolder is not null && !IsBusy;

    public bool CanRenameSelected => SelectedEntries.Count == 1 && SelectedEntry?.CanRename == true && !IsBusy;

    public bool CanCopySelected => SelectedEntries.Count > 0 && SelectedEntries.All(entry => entry.CanCopy) && !IsBusy;

    public bool CanMoveSelected => SelectedEntries.Count > 0 && SelectedEntries.All(entry => entry.CanMove) && !IsBusy;

    public bool CanDeleteSelected => SelectedEntries.Count > 0 && SelectedEntries.All(entry => entry.CanDelete) && !IsBusy;

    public async Task SetFileManagerAsync(VirtualFileManager? manager, CancellationToken cancellationToken = default)
    {
        FileManager = manager;
        _fileManagerAsync = manager?.AsAsync();
        _sourceEntries.Clear();
        Entries.Clear();
        Breadcrumbs.Clear();
        UpdateSelection(Array.Empty<VirtualFileSystemEntry>(), null);

        if (manager is null)
        {
            CurrentFolder = null;
            UpdateEntriesChangedState();
            StatusText = "未设置文件管理器";
            return;
        }

        var rootFolder = await _fileManagerAsync!.GetRootFolderAsync(cancellationToken);
        await NavigateToFolderAsync(rootFolder, cancellationToken);
    }

    public async Task NavigateToFolderAsync(VirtualFolderInfo folder, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(folder);
        CurrentFolder = folder;
        await RefreshAsync(cancellationToken);
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (_fileManagerAsync is null)
        {
            await SetFileManagerAsync(null, cancellationToken);
            return;
        }

        var folder = CurrentFolder ?? FileManager!.RootFolder;
        CurrentFolder = folder;

        IsBusy = true;
        try
        {
            var entries = await _fileManagerAsync.GetEntriesAsync(folder, cancellationToken);
            _sourceEntries.Clear();
            _sourceEntries.AddRange(entries);

            Breadcrumbs.Clear();
            foreach (var breadcrumb in folder.GetBreadcrumbs())
            {
                Breadcrumbs.Add(breadcrumb);
            }

            ApplyEntriesView();
        }
        finally
        {
            IsBusy = false;
            RefreshCommandStates();
        }
    }

    public async Task NavigateUpAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentFolder?.UpperLevelFolder is null)
        {
            return;
        }

        await NavigateToFolderAsync(CurrentFolder.UpperLevelFolder, cancellationToken);
    }

    public async Task OpenEntryAsync(VirtualFileSystemEntry? entry, CancellationToken cancellationToken = default)
    {
        if (entry is VirtualFolderInfo folder)
        {
            await NavigateToFolderAsync(folder, cancellationToken);
        }
    }

    public async Task CreateFolderAsync(string folderName, CancellationToken cancellationToken = default)
    {
        if (_fileManagerAsync is null || CurrentFolder is null)
        {
            throw new InvalidOperationException("当前没有可用的目录上下文。");
        }

        var createdFolder = await _fileManagerAsync.CreateFolderAsync(CurrentFolder, folderName, cancellationToken);
        await RefreshAsync(cancellationToken);
        RestoreSelection(new[] { createdFolder.Id });
    }

    public async Task RenameSelectedAsync(string newName, CancellationToken cancellationToken = default)
    {
        var entry = GetSingleSelectedEntry("请先选择一个要重命名的项目。");
        if (_fileManagerAsync is null)
        {
            throw new InvalidOperationException("未设置文件管理器。");
        }

        await _fileManagerAsync.RenameEntryAsync(entry, newName, cancellationToken);
        await RefreshAsync(cancellationToken);
        RestoreSelection(new[] { entry.Id });
    }

    public async Task CopySelectedAsync(VirtualFolderInfo targetFolder, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(targetFolder);
        if (_fileManagerAsync is null)
        {
            throw new InvalidOperationException("未设置文件管理器。");
        }

        var targets = GetSelectedEntriesSnapshot("请先选择要复制的项目。");
        var copiedIds = new List<string>(targets.Count);
        foreach (var entry in targets)
        {
            var copiedEntry = await _fileManagerAsync.CopyEntryAsync(entry, targetFolder, cancellationToken);
            copiedIds.Add(copiedEntry.Id);
        }

        await RefreshAsync(cancellationToken);
        if (CurrentFolder is not null && string.Equals(CurrentFolder.Id, targetFolder.Id, StringComparison.OrdinalIgnoreCase))
        {
            RestoreSelection(copiedIds);
        }
    }

    public async Task MoveSelectedAsync(VirtualFolderInfo targetFolder, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(targetFolder);
        if (_fileManagerAsync is null)
        {
            throw new InvalidOperationException("未设置文件管理器。");
        }

        var targets = GetSelectedEntriesSnapshot("请先选择要移动的项目。");
        var movedIds = new List<string>(targets.Count);
        foreach (var entry in targets)
        {
            var movedEntry = await _fileManagerAsync.MoveEntryAsync(entry, targetFolder, cancellationToken);
            movedIds.Add(movedEntry.Id);
        }

        await RefreshAsync(cancellationToken);
        if (CurrentFolder is not null && string.Equals(CurrentFolder.Id, targetFolder.Id, StringComparison.OrdinalIgnoreCase))
        {
            RestoreSelection(movedIds);
        }
    }

    public async Task DeleteSelectedAsync(CancellationToken cancellationToken = default)
    {
        if (_fileManagerAsync is null)
        {
            throw new InvalidOperationException("未设置文件管理器。");
        }

        var targets = GetSelectedEntriesSnapshot("请先选择要删除的项目。");
        foreach (var entry in targets)
        {
            await _fileManagerAsync.DeleteEntryAsync(entry, cancellationToken);
        }

        await RefreshAsync(cancellationToken);
    }

    public void UpdateSelection(IReadOnlyList<VirtualFileSystemEntry> selectedEntries, VirtualFileSystemEntry? selectedEntry)
    {
        ArgumentNullException.ThrowIfNull(selectedEntries);

        SelectedEntries.Clear();
        foreach (var entry in selectedEntries.DistinctBy(entry => entry.Id, StringComparer.OrdinalIgnoreCase))
        {
            SelectedEntries.Add(entry);
        }

        SelectedEntry = selectedEntry ?? SelectedEntries.FirstOrDefault();
        UpdateEntriesChangedState();
    }

    private async Task OpenEntryInternalAsync(object? parameter)
    {
        await OpenEntryAsync(parameter as VirtualFileSystemEntry ?? SelectedEntry);
    }

    private async Task NavigateBreadcrumbAsync(object? parameter)
    {
        if (parameter is RelativeFolderLink link)
        {
            await NavigateToFolderAsync(link.Folder);
        }
    }

    private void ApplyEntriesView()
    {
        var selectionIds = SelectedEntries.Select(entry => entry.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var primarySelectionId = SelectedEntry?.Id;

        IEnumerable<VirtualFileSystemEntry> entries = _sourceEntries;
        entries = EntryFilter switch
        {
            FileExplorerEntryFilter.FoldersOnly => entries.Where(entry => entry is VirtualFolderInfo),
            FileExplorerEntryFilter.FilesOnly => entries.Where(entry => entry is VirtualFileInfo),
            _ => entries,
        };

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            entries = entries.Where(entry => MatchesSearch(entry, SearchText));
        }

        entries = ApplySort(entries);

        Entries.Clear();
        foreach (var entry in entries)
        {
            Entries.Add(entry);
        }

        var visibleSelectedEntries = Entries.Where(entry => selectionIds.Contains(entry.Id)).ToList();
        var primarySelection = primarySelectionId is null
            ? visibleSelectedEntries.FirstOrDefault()
            : Entries.FirstOrDefault(entry => string.Equals(entry.Id, primarySelectionId, StringComparison.OrdinalIgnoreCase));
        UpdateSelection(visibleSelectedEntries, primarySelection);
        UpdateStatus();
    }

    private IEnumerable<VirtualFileSystemEntry> ApplySort(IEnumerable<VirtualFileSystemEntry> entries)
    {
        IOrderedEnumerable<VirtualFileSystemEntry> orderedEntries = SortField switch
        {
            FileExplorerSortField.Type => entries.OrderBy(entry => entry.EntryType).ThenBy(entry => entry.DisplayType, StringComparer.CurrentCultureIgnoreCase).ThenBy(entry => entry.Name, StringComparer.CurrentCultureIgnoreCase),
            FileExplorerSortField.Size => entries.OrderBy(entry => entry.EntryType).ThenBy(entry => entry is VirtualFileInfo file ? file.Length ?? long.MinValue : long.MinValue).ThenBy(entry => entry.Name, StringComparer.CurrentCultureIgnoreCase),
            FileExplorerSortField.ModifiedTime => entries.OrderBy(entry => entry.EntryType).ThenBy(entry => entry.LastWriteTime).ThenBy(entry => entry.Name, StringComparer.CurrentCultureIgnoreCase),
            FileExplorerSortField.CreatedTime => entries.OrderBy(entry => entry.EntryType).ThenBy(entry => entry.CreatedTime).ThenBy(entry => entry.Name, StringComparer.CurrentCultureIgnoreCase),
            _ => entries.OrderBy(entry => entry.EntryType).ThenBy(entry => entry.Name, StringComparer.CurrentCultureIgnoreCase),
        };

        return SortDirection == ListSortDirection.Ascending ? orderedEntries : orderedEntries.Reverse();
    }

    private static bool MatchesSearch(VirtualFileSystemEntry entry, string searchText)
    {
        return entry.Name.Contains(searchText, StringComparison.CurrentCultureIgnoreCase)
               || entry.DisplayType.Contains(searchText, StringComparison.CurrentCultureIgnoreCase)
               || entry.Id.Contains(searchText, StringComparison.CurrentCultureIgnoreCase);
    }

    private void RestoreSelection(IEnumerable<string> entryIds)
    {
        var selectionIds = entryIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var selectedEntries = Entries.Where(entry => selectionIds.Contains(entry.Id)).ToList();
        UpdateSelection(selectedEntries, selectedEntries.FirstOrDefault());
    }

    private List<VirtualFileSystemEntry> GetSelectedEntriesSnapshot(string errorMessage)
    {
        if (SelectedEntries.Count == 0)
        {
            throw new InvalidOperationException(errorMessage);
        }

        return SelectedEntries.ToList();
    }

    private VirtualFileSystemEntry GetSingleSelectedEntry(string errorMessage)
    {
        var entries = GetSelectedEntriesSnapshot(errorMessage);
        if (entries.Count != 1)
        {
            throw new InvalidOperationException("当前操作仅支持单项选择。");
        }

        return entries[0];
    }

    private void UpdateStatus()
    {
        var folderCount = Entries.Count(entry => entry is VirtualFolderInfo);
        var fileCount = Entries.Count - folderCount;
        StatusText = $"当前目录：{CurrentFolderPath}    可见文件夹：{folderCount}    可见文件：{fileCount}    总条目：{_sourceEntries.Count}";
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
        _navigateBreadcrumbCommand.RaiseCanExecuteChanged();
        _switchToTileViewCommand.RaiseCanExecuteChanged();
        _switchToDetailsViewCommand.RaiseCanExecuteChanged();
        OnPropertyChanged(nameof(CanCreateFolder));
    }
}
