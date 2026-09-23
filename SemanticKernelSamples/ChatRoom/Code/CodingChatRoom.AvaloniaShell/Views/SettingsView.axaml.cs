using System;
using System.Diagnostics;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

using CodingChatRoom.AvaloniaShell.ViewModels;

namespace CodingChatRoom.AvaloniaShell.Views;

/// <summary>
/// 显示模型服务与 Windows 沙箱设置。
/// </summary>
public partial class SettingsView : UserControl
{
    /// <summary>
    /// 初始化设置视图。
    /// </summary>
    public SettingsView()
    {
        InitializeComponent();
    }

    private void OnWindowsSandboxServerAddressLostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel
            && viewModel.TestWindowsSandboxConnectionCommand.CanExecute(null))
        {
            viewModel.TestWindowsSandboxConnectionCommand.Execute(null);
        }
    }

    private void OnImportModelsClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button
            && button.FindAncestorOfType<Border>()?.FindDescendantOfType<OpenAIModelCatalogView>() is { } catalogView)
        {
            catalogView.LoadModels(button, e);
        }
    }

    private async void OnInstallOrUpdateLanguageServerClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var startInfo = new ProcessStartInfo("dotnet")
            {
                UseShellExecute = false,
                CreateNoWindow = false,
            };
            startInfo.ArgumentList.Add("tool");
            startInfo.ArgumentList.Add("update");
            startInfo.ArgumentList.Add("-g");
            startInfo.ArgumentList.Add("roslyn-language-server");

            using Process? process = Process.Start(startInfo);
            if (process is not null)
            {
                await process.WaitForExitAsync();
            }
        }
        catch (Exception exception)
        {
            Trace.TraceError($"安装或更新语言服务器失败：{exception}");
        }
    }

    private async void OnCopyLanguageServerInstallCommandClick(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this)?.Clipboard is not { } clipboard)
        {
            return;
        }

        try
        {
            var dataTransfer = new DataTransfer();
            dataTransfer.Add(DataTransferItem.CreateText("dotnet tool update -g roslyn-language-server"));
            await clipboard.SetDataAsync(dataTransfer);
        }
        catch (Exception exception)
        {
            Trace.TraceError($"复制语言服务器安装命令失败：{exception}");
        }
    }
}
