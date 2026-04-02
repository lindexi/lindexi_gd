using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

using SimpleWrite.Business.FileHandlers;
using SimpleWrite.Business.FolderExplorers;

namespace SimpleWrite.ViewModels;

public class FolderExplorerViewModel : ViewModelBase
{
    public FolderExplorerViewModel(SimpleWriteMainViewModel mainViewModel)
    {
        ArgumentNullException.ThrowIfNull(mainViewModel);
        MainViewModel = mainViewModel;
    }

    private readonly FolderExplorerService _folderExplorerService = new FolderExplorerService();

    public SimpleWriteMainViewModel MainViewModel { get; }

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
        if (folderTreeItem is null || folderTreeItem.IsDirectory)
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

    private DirectoryInfo? _currentFolder;
}
