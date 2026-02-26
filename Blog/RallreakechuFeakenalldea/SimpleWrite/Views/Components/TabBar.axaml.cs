using Avalonia.Controls;
using Avalonia.Interactivity;

using SimpleWrite.Models;
using SimpleWrite.Utils;

namespace SimpleWrite.Views.Components;

/// <summary>
/// 上层 Tab 栏的内容，可显示当前打开的标签
/// </summary>
public partial class TabBar : UserControl
{
    public TabBar()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 在文件资源管理器中打开
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OpenInFileExplorerMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { DataContext: EditorModel { FileInfo: { } fileInfo } })
        {
            return;
        }

        FileExplorerHelper.TryOpenInFileExplorer(fileInfo);
    }
}