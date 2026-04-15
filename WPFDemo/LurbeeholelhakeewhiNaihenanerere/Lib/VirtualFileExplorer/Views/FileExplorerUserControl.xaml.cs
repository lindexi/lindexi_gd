using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using VirtualFileExplorer.Core;
using VirtualFileExplorer.ViewModels;

namespace VirtualFileExplorer.Views;

public partial class FileExplorerUserControl : UserControl
{
    private readonly FileExplorerViewModel _viewModel;

    public FileExplorerUserControl()
    {
        InitializeComponent();
        _viewModel = new FileExplorerViewModel();
        DataContext = _viewModel;
        _viewModel.SelectionMode = SelectionMode.Extended;
    }

    public VirtualFileManager? FileManager
    {
        get => (VirtualFileManager?)GetValue(FileManagerProperty);
        set => SetValue(FileManagerProperty, value);
    }

    public static readonly DependencyProperty FileManagerProperty = DependencyProperty.Register(
        nameof(FileManager), typeof(VirtualFileManager), typeof(FileExplorerUserControl), new PropertyMetadata(null, OnFileManagerChanged));

    public DataTemplate? TileEntryTemplate
    {
        get => (DataTemplate?)GetValue(TileEntryTemplateProperty);
        set => SetValue(TileEntryTemplateProperty, value);
    }

    public static readonly DependencyProperty TileEntryTemplateProperty = DependencyProperty.Register(
        nameof(TileEntryTemplate), typeof(DataTemplate), typeof(FileExplorerUserControl), new PropertyMetadata(null));

    public DataTemplate? TileFileTemplate
    {
        get => (DataTemplate?)GetValue(TileFileTemplateProperty);
        set => SetValue(TileFileTemplateProperty, value);
    }

    public static readonly DependencyProperty TileFileTemplateProperty = DependencyProperty.Register(
        nameof(TileFileTemplate), typeof(DataTemplate), typeof(FileExplorerUserControl), new PropertyMetadata(null));

    public DataTemplate? TileFolderTemplate
    {
        get => (DataTemplate?)GetValue(TileFolderTemplateProperty);
        set => SetValue(TileFolderTemplateProperty, value);
    }

    public static readonly DependencyProperty TileFolderTemplateProperty = DependencyProperty.Register(
        nameof(TileFolderTemplate), typeof(DataTemplate), typeof(FileExplorerUserControl), new PropertyMetadata(null));

    public object? ToolBarContent
    {
        get => GetValue(ToolBarContentProperty);
        set => SetValue(ToolBarContentProperty, value);
    }

    public static readonly DependencyProperty ToolBarContentProperty = DependencyProperty.Register(
        nameof(ToolBarContent), typeof(object), typeof(FileExplorerUserControl), new PropertyMetadata(null));

    public object? NavigationContent
    {
        get => GetValue(NavigationContentProperty);
        set => SetValue(NavigationContentProperty, value);
    }

    public static readonly DependencyProperty NavigationContentProperty = DependencyProperty.Register(
        nameof(NavigationContent), typeof(object), typeof(FileExplorerUserControl), new PropertyMetadata(null));

    public object? EmptyContent
    {
        get => GetValue(EmptyContentProperty);
        set => SetValue(EmptyContentProperty, value);
    }

    public static readonly DependencyProperty EmptyContentProperty = DependencyProperty.Register(
        nameof(EmptyContent), typeof(object), typeof(FileExplorerUserControl), new PropertyMetadata(null));

    private static void OnFileManagerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (FileExplorerUserControl)d;
        control.ApplyFileManagerAsync((VirtualFileManager?)e.NewValue);
    }

    private async void ApplyFileManagerAsync(VirtualFileManager? fileManager)
    {
        await ExecuteActionAsync(() => _viewModel.SetFileManagerAsync(fileManager));
    }

    private async void OnEntryInvoked(object sender, EntryInvokedEventArgs e)
    {
        await ExecuteActionAsync(() => _viewModel.OpenEntryAsync(e.Entry));
    }

    private async void OnCreateFolderClick(object sender, RoutedEventArgs e)
    {
        var folderName = TextPromptWindow.ShowDialog(GetOwnerWindow(), "新建文件夹", "请输入新文件夹名称：");
        if (string.IsNullOrWhiteSpace(folderName))
        {
            return;
        }

        await ExecuteActionAsync(() => _viewModel.CreateFolderAsync(folderName));
    }

    private async void OnRenameClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedEntries.Count != 1 || _viewModel.SelectedEntry is null)
        {
            return;
        }

        var newName = TextPromptWindow.ShowDialog(GetOwnerWindow(), "重命名", "请输入新名称：", _viewModel.SelectedEntry.Name);
        if (string.IsNullOrWhiteSpace(newName))
        {
            return;
        }

        await ExecuteActionAsync(() => _viewModel.RenameSelectedAsync(newName));
    }

    private async void OnCopyClick(object sender, RoutedEventArgs e)
    {
        if (FileManager is null || _viewModel.CurrentFolder is null)
        {
            return;
        }

        var targetFolder = FolderPickerWindow.PickFolder(GetOwnerWindow(), FileManager, _viewModel.CurrentFolder, "选择复制目标文件夹");
        if (targetFolder is null)
        {
            return;
        }

        await ExecuteActionAsync(() => _viewModel.CopySelectedAsync(targetFolder));
    }

    private async void OnMoveClick(object sender, RoutedEventArgs e)
    {
        if (FileManager is null || _viewModel.CurrentFolder is null)
        {
            return;
        }

        var targetFolder = FolderPickerWindow.PickFolder(GetOwnerWindow(), FileManager, _viewModel.CurrentFolder, "选择移动目标文件夹");
        if (targetFolder is null)
        {
            return;
        }

        await ExecuteActionAsync(() => _viewModel.MoveSelectedAsync(targetFolder));
    }

    private async void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedEntries.Count == 0)
        {
            return;
        }

        var itemNames = _viewModel.SelectedEntries.Take(3).Select(entry => entry.Name).ToList();
        var summary = string.Join("、", itemNames);
        if (_viewModel.SelectedEntries.Count > itemNames.Count)
        {
            summary += " 等项目";
        }

        var result = MessageBox.Show(GetOwnerWindow(), $"确定删除 {summary} 吗？", "删除确认", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        await ExecuteActionAsync(() => _viewModel.DeleteSelectedAsync());
    }

    private void OnSortDirectionClick(object sender, RoutedEventArgs e)
    {
        _viewModel.SortDirection = _viewModel.SortDirection == ListSortDirection.Ascending
            ? ListSortDirection.Descending
            : ListSortDirection.Ascending;
    }

    private async Task ExecuteActionAsync(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        try
        {
            await action();
        }
        catch (InvalidOperationException exception)
        {
            MessageBox.Show(GetOwnerWindow(), exception.Message, "操作失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (ArgumentException exception)
        {
            MessageBox.Show(GetOwnerWindow(), exception.Message, "输入无效", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (IOException exception)
        {
            MessageBox.Show(GetOwnerWindow(), exception.Message, "文件操作失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private Window? GetOwnerWindow()
    {
        return Window.GetWindow(this);
    }
}
