using Avalonia.Controls;
using Avalonia.Interactivity;
using SimpleWrite.Models;
using System;
using System.Diagnostics;

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

    private void OpenInFileExplorerMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { DataContext: EditorModel { FileInfo: { } fileInfo } })
        {
            return;
        }

        if (!fileInfo.Exists || !OperatingSystem.IsWindows())
        {
            return;
        }

        Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{fileInfo.FullName}\"")
        {
            UseShellExecute = true
        });
    }
}