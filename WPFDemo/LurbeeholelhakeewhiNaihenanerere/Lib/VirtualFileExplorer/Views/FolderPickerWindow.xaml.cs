using System.Windows;
using System.Windows.Controls;
using VirtualFileExplorer.Core;
using VirtualFileExplorer.ViewModels;

namespace VirtualFileExplorer.Views;

public partial class FolderPickerWindow : Window
{
    private readonly FileExplorerViewModel _viewModel = new();
    private VirtualFolderInfo? _selectedFolder;

    public FolderPickerWindow(VirtualFileManager fileManager, VirtualFolderInfo initialFolder, string title)
    {
        ArgumentNullException.ThrowIfNull(fileManager);
        ArgumentNullException.ThrowIfNull(initialFolder);
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("标题不能为空。", nameof(title));
        }

        InitializeComponent();
        Title = title;
        DataContext = _viewModel;
        PickerContentControl.EntryInvoked += OnEntryInvoked;

        Loaded += async (_, _) =>
        {
            _viewModel.DisplayMode = FileExplorerDisplayMode.Details;
            _viewModel.SelectionMode = SelectionMode.Single;
            _viewModel.EntryFilter = FileExplorerEntryFilter.FoldersOnly;
            await _viewModel.SetFileManagerAsync(fileManager);
            await _viewModel.NavigateToFolderAsync(initialFolder);
        };
    }

    public static VirtualFolderInfo? PickFolder(Window? owner, VirtualFileManager fileManager, VirtualFolderInfo initialFolder, string title)
    {
        var window = new FolderPickerWindow(fileManager, initialFolder, title)
        {
            Owner = owner
        };

        return window.ShowDialog() == true ? window._selectedFolder : null;
    }

    private async void OnEntryInvoked(object? sender, EntryInvokedEventArgs e)
    {
        await _viewModel.OpenEntryAsync(e.Entry);
    }

    private void OnUseCurrentFolderClick(object sender, RoutedEventArgs e)
    {
        _selectedFolder = _viewModel.CurrentFolder;
        DialogResult = _selectedFolder is not null;
    }

    private void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        _selectedFolder = _viewModel.SelectedEntry as VirtualFolderInfo ?? _viewModel.CurrentFolder;
        DialogResult = _selectedFolder is not null;
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
