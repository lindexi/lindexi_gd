using System;
using Avalonia.Controls;
using OllamaHubLogViewer.ViewModels;

namespace OllamaHubLogViewer;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel = new();

    /// <summary>
    /// 初始化日志查看器主窗口。
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        await _viewModel.InitializeAsync();
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.Dispose();
        base.OnClosed(e);
    }
}