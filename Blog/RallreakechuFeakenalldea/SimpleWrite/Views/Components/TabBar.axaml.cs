using System;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

using SimpleWrite.Models;
using SimpleWrite.Utils;
using SimpleWrite.ViewModels;

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

    public SimpleWriteMainViewModel MainViewModel => (SimpleWriteMainViewModel) DataContext!;

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

    private void CloseTabButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control control)
        {
            return;
        }

        if (control.DataContext is not EditorModel editorModel)
        {
            return;
        }

        _ = MainViewModel.EditorViewModel.RequestCloseDocumentAsync(editorModel);
    }

    private void EditorTabListBox_OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (sender is not ListBox listBox)
        {
            return;
        }

        if (Math.Abs(e.Delta.Y) < double.Epsilon)
        {
            return;
        }

        if (listBox.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault() is not { } scrollViewer)
        {
            return;
        }

        var maxHorizontalOffset = Math.Max(0, scrollViewer.Extent.Width - scrollViewer.Viewport.Width);
        if (maxHorizontalOffset <= 0)
        {
            return;
        }

        const double wheelStep = 48;
        var targetOffsetX = Math.Clamp(scrollViewer.Offset.X - e.Delta.Y * wheelStep, 0, maxHorizontalOffset);
        if (Math.Abs(targetOffsetX - scrollViewer.Offset.X) < double.Epsilon)
        {
            return;
        }

        scrollViewer.Offset = new Vector(targetOffsetX, scrollViewer.Offset.Y);
        e.Handled = true;
    }
}