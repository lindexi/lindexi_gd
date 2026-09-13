using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CodingChatRoom.AvaloniaShell.ViewModels;

namespace CodingChatRoom.AvaloniaShell;

public partial class MainWindow : Window
{
    private bool _isClosing;

    public MainWindow()
    {
        InitializeComponent();
        UpdateBackgroundForTransparencyLevel();
        Closing += OnClosing;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ActualTransparencyLevelProperty)
        {
            UpdateBackgroundForTransparencyLevel();
        }
    }

    private void UpdateBackgroundForTransparencyLevel()
    {
        Background = ActualTransparencyLevel == WindowTransparencyLevel.Mica
            ? Brushes.Transparent
            : Brushes.White;
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
