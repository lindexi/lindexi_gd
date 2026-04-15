using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VirtualFileExplorer.Core;
using VirtualFileExplorer.ViewModels;

namespace VirtualFileExplorer.Views;

/// <summary>
/// FileExplorerUserControl.xaml 的交互逻辑
/// </summary>
public partial class FileExplorerUserControl : UserControl
{
    private readonly FileExplorerViewModel _viewModel;

    public FileExplorerUserControl()
    {
        InitializeComponent();
        _viewModel = new FileExplorerViewModel();
        DataContext = _viewModel;

        SetCurrentValue(TileEntryTemplateProperty, (DataTemplate) Resources["DefaultTileEntryTemplate"]);
        SetCurrentValue(DetailsEntryTemplateProperty, (DataTemplate) Resources["DefaultDetailsEntryTemplate"]);
    }

    public VirtualFileManager? FileManager
    {
        get => (VirtualFileManager?) GetValue(FileManagerProperty);
        set => SetValue(FileManagerProperty, value);
    }

    public static readonly DependencyProperty FileManagerProperty = DependencyProperty.Register(
        nameof(FileManager), typeof(VirtualFileManager), typeof(FileExplorerUserControl),
        new PropertyMetadata(null, OnFileManagerChanged));

    public DataTemplate? TileEntryTemplate
    {
        get => (DataTemplate?) GetValue(TileEntryTemplateProperty);
        set => SetValue(TileEntryTemplateProperty, value);
    }

    public static readonly DependencyProperty TileEntryTemplateProperty = DependencyProperty.Register(
        nameof(TileEntryTemplate), typeof(DataTemplate), typeof(FileExplorerUserControl), new PropertyMetadata(null));

    public DataTemplate? DetailsEntryTemplate
    {
        get => (DataTemplate?) GetValue(DetailsEntryTemplateProperty);
        set => SetValue(DetailsEntryTemplateProperty, value);
    }

    public static readonly DependencyProperty DetailsEntryTemplateProperty = DependencyProperty.Register(
        nameof(DetailsEntryTemplate), typeof(DataTemplate), typeof(FileExplorerUserControl), new PropertyMetadata(null));

    public object? ToolBarContent
    {
        get => GetValue(ToolBarContentProperty);
        set => SetValue(ToolBarContentProperty, value);
    }

    public static readonly DependencyProperty ToolBarContentProperty = DependencyProperty.Register(
        nameof(ToolBarContent), typeof(object), typeof(FileExplorerUserControl), new PropertyMetadata(null));

    public object? EmptyContent
    {
        get => GetValue(EmptyContentProperty);
        set => SetValue(EmptyContentProperty, value);
    }

    public static readonly DependencyProperty EmptyContentProperty = DependencyProperty.Register(
        nameof(EmptyContent), typeof(object), typeof(FileExplorerUserControl), new PropertyMetadata(null));

    private static void OnFileManagerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (FileExplorerUserControl) d;
        control._viewModel.SetFileManager((VirtualFileManager?) e.NewValue);
    }

    private void OnBreadcrumbClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: RelativeFolderLink link })
        {
            ExecuteAction(() => _viewModel.NavigateToFolder(link.Folder));
        }
    }

    private void OnEntryDoubleClick(object sender, MouseButtonEventArgs e)
    {
        ExecuteAction(() => _viewModel.OpenEntry(_viewModel.SelectedEntry));
    }

    private void OnCreateFolderClick(object sender, RoutedEventArgs e)
    {
        var folderName = TextPromptWindow.ShowDialog(GetOwnerWindow(), "新建文件夹", "请输入新文件夹名称：");
        if (folderName is null)
        {
            return;
        }

        ExecuteAction(() => _viewModel.CreateFolder(folderName));
    }

    private void OnRenameClick(object sender, RoutedEventArgs e)
    {
        var selectedEntry = _viewModel.SelectedEntry;
        if (selectedEntry is null)
        {
            return;
        }

        var newName = TextPromptWindow.ShowDialog(GetOwnerWindow(), "重命名", "请输入新名称：", selectedEntry.Name);
        if (newName is null)
        {
            return;
        }

        ExecuteAction(() => _viewModel.RenameSelected(newName));
    }

    private void OnCopyClick(object sender, RoutedEventArgs e)
    {
        var defaultValue = _viewModel.CurrentFolderPath;
        var targetFolderPath = TextPromptWindow.ShowDialog(GetOwnerWindow(), "复制到", "请输入目标文件夹路径：", defaultValue);
        if (targetFolderPath is null)
        {
            return;
        }

        ExecuteAction(() => _viewModel.CopySelected(targetFolderPath));
    }

    private void OnMoveClick(object sender, RoutedEventArgs e)
    {
        var defaultValue = _viewModel.CurrentFolderPath;
        var targetFolderPath = TextPromptWindow.ShowDialog(GetOwnerWindow(), "移动到", "请输入目标文件夹路径：", defaultValue);
        if (targetFolderPath is null)
        {
            return;
        }

        ExecuteAction(() => _viewModel.MoveSelected(targetFolderPath));
    }

    private void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        var selectedEntry = _viewModel.SelectedEntry;
        if (selectedEntry is null)
        {
            return;
        }

        var messageBoxResult = MessageBox.Show(GetOwnerWindow(), $"确定删除“{selectedEntry.Name}”吗？", "删除确认",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (messageBoxResult != MessageBoxResult.Yes)
        {
            return;
        }

        ExecuteAction(_viewModel.DeleteSelected);
    }

    private void ExecuteAction(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        try
        {
            action();
        }
        catch (Exception exception)
        {
            MessageBox.Show(GetOwnerWindow(), exception.Message, "操作失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private Window? GetOwnerWindow()
    {
        return Window.GetWindow(this);
    }
}
