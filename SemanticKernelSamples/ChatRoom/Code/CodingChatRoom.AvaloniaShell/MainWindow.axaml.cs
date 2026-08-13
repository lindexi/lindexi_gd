using System;

using Avalonia.Controls;

using CodingChatRoom.AvaloniaShell.Services;

namespace CodingChatRoom.AvaloniaShell;

public partial class MainWindow : Window
{
    private readonly CodingChatHistoryLoader? _historyLoader;

    public MainWindow()
    {
        InitializeComponent();
    }

    internal MainWindow(CodingChatHistoryLoader historyLoader)
        : this()
    {
        ArgumentNullException.ThrowIfNull(historyLoader);
        _historyLoader = historyLoader;
        Opened += OnOpened;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        Opened -= OnOpened;
        await _historyLoader!.LoadAsync();
    }
}