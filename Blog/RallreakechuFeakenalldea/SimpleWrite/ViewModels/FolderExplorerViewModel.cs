using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using SimpleWrite.Business.FileHandlers;
using SimpleWrite.Business.FolderExplorers;

namespace SimpleWrite.ViewModels;

/// <summary>
/// 文件夹管理器模型
/// </summary>
public class FolderExplorerViewModel : ViewModelBase
{
    public FolderExplorerViewModel()
        : this(null)
    {
    }

    public FolderExplorerViewModel(SimpleWriteMainViewModel? mainViewModel)
    {
        if (!Design.IsDesignMode)
        {
            ArgumentNullException.ThrowIfNull(mainViewModel);
        }

        MainViewModel = mainViewModel;

        if (Design.IsDesignMode)
        {
            AddDesignTimeData();
        }
    }

    private readonly FolderExplorerService _folderExplorerService = new FolderExplorerService();

    public SimpleWriteMainViewModel? MainViewModel { get; }

    public ObservableCollection<FolderTreeItemViewModel> RootItems { get; } = [];

    public string CurrentFolderPath => _currentFolder?.FullName ?? string.Empty;

    public bool HasOpenedFolder => _currentFolder is not null;

    public bool IsEmptyStateVisible => !HasOpenedFolder;

    internal IFilePickerHandler? FilePickerHandler { get; set; }

    public DirectoryInfo? CurrentFolder => _currentFolder;

    public event EventHandler? CurrentFolderChanged;

    public async Task PickAndOpenFolderAsync()
    {
        if (FilePickerHandler is null)
        {
            return;
        }

        var directoryInfo = await FilePickerHandler.PickOpenFolderAsync();
        if (directoryInfo is null)
        {
            return;
        }

        await OpenFolderAsync(directoryInfo);
    }

    public async Task OpenFolderAsync(DirectoryInfo directoryInfo)
    {
        ArgumentNullException.ThrowIfNull(directoryInfo);

        var rootEntry = await _folderExplorerService.BuildTreeAsync(directoryInfo);
        RootItems.Clear();
        RootItems.Add(new FolderTreeItemViewModel(rootEntry));

        _currentFolder = directoryInfo;
        NotifyFolderStateChanged();
    }

    public void ClearFolder()
    {
        RootItems.Clear();
        _currentFolder = null;
        NotifyFolderStateChanged();
    }

    public Task OpenFileAsync(FolderTreeItemViewModel? folderTreeItem)
    {
        if (MainViewModel is null || folderTreeItem is null || folderTreeItem.IsDirectory)
        {
            return Task.CompletedTask;
        }

        return MainViewModel.OpenFileAsync(new FileInfo(folderTreeItem.FullPath));
    }

    private void NotifyFolderStateChanged()
    {
        OnPropertyChanged(nameof(CurrentFolderPath));
        OnPropertyChanged(nameof(HasOpenedFolder));
        OnPropertyChanged(nameof(IsEmptyStateVisible));
        CurrentFolderChanged?.Invoke(this, EventArgs.Empty);
    }

    private void AddDesignTimeData()
    {
        if (RootItems.Count > 0)
        {
            return;
        }

        _currentFolder = new DirectoryInfo(@"C:\Projects\SimpleWrite");
        RootItems.Add(new FolderTreeItemViewModel(CreateDesignTimeRootEntry()));
        NotifyFolderStateChanged();
    }

    private static FolderTreeEntry CreateDesignTimeRootEntry()
    {
        return new FolderTreeEntry("SimpleWrite", @"C:\Projects\SimpleWrite", true,
        [
            new FolderTreeEntry("Docs", @"C:\Projects\SimpleWrite\Docs", true,
            [
                new FolderTreeEntry("README.md", @"C:\Projects\SimpleWrite\Docs\README.md", false, []),
                new FolderTreeEntry("Folder-Explorer-And-Folder-Find.md", @"C:\Projects\SimpleWrite\Docs\Knowledge\Avalonia\Folder-Explorer-And-Folder-Find.md", false, [])
            ]),
            new FolderTreeEntry("SimpleWrite", @"C:\Projects\SimpleWrite\SimpleWrite", true,
            [
                new FolderTreeEntry("Views", @"C:\Projects\SimpleWrite\SimpleWrite\Views", true,
                [
                    new FolderTreeEntry("Components", @"C:\Projects\SimpleWrite\SimpleWrite\Views\Components", true,
                    [
                        new FolderTreeEntry("SimpleWriteSideBar.axaml", @"C:\Projects\SimpleWrite\SimpleWrite\Views\Components\SimpleWriteSideBar.axaml", false, []),
                        new FolderTreeEntry("MainEditorView.axaml", @"C:\Projects\SimpleWrite\SimpleWrite\Views\Components\MainEditorView.axaml", false, [])
                    ])
                ]),
                new FolderTreeEntry("Styles", @"C:\Projects\SimpleWrite\SimpleWrite\Styles", true,
                [
                    new FolderTreeEntry("MainStyles.axaml", @"C:\Projects\SimpleWrite\SimpleWrite\Styles\MainStyles.axaml", false, []),
                    new FolderTreeEntry("Brushes.axaml", @"C:\Projects\SimpleWrite\SimpleWrite\Styles\Brushes.axaml", false, [])
                ])
            ]),
            new FolderTreeEntry("待办事项.txt", @"C:\Projects\SimpleWrite\待办事项.txt", false, [])
        ]);
    }

    private DirectoryInfo? _currentFolder;
}
