using PptxGenerator;

using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace PptxGeneratorWpfDemo;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindowViewModel ViewModel => DataContext as MainWindowViewModel ?? throw new InvalidCastException("DataContext must be of type MainWindowViewModel.");

    private void ImageBorder_OnMouseLeftButtonDown(object? sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (ViewModel.SlideChatManager.PreviewImage is { } previewImage)
        {
            var imageFilePath = Path.Join(AppContext.BaseDirectory, $"PreviewBitmap_{Path.GetRandomFileName()}.png");
            previewImage.Save(imageFilePath);
            Process.Start(new ProcessStartInfo(imageFilePath) { UseShellExecute = true });
        }
    }

    /// <summary>
    /// MCP 服务地址输入框失去焦点时，尝试连接 MCP 服务并切换渲染管道。
    /// </summary>
    private void McpServiceUrl_LostFocus(object sender, System.Windows.RoutedEventArgs e)
    {
        _ = ViewModel.TryConnectMcpRenderAsync();
    }
}