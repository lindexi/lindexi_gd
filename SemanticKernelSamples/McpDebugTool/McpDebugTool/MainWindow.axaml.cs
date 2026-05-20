using Avalonia.Controls;
using McpDebugTool.ViewModels;

namespace McpDebugTool;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override async void OnClosed(System.EventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.DisposeAsync();
        }

        base.OnClosed(e);
    }
}