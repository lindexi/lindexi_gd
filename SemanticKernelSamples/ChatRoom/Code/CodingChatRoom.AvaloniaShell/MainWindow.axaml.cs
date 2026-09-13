using System;
using Avalonia.Controls;
using CodingChatRoom.AvaloniaShell.ViewModels;

namespace CodingChatRoom.AvaloniaShell;

public partial class MainWindow : Window
{
    private bool _isClosing;

    public MainWindow()
    {
        InitializeComponent();
        Closing += OnClosing;
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_isClosing || DataContext is not MainViewModel viewModel)
        {
            return;
        }

        e.Cancel = true;
        _isClosing = true;
        await viewModel.DisposeAsync().ConfigureAwait(true);
        Close();
    }
}
