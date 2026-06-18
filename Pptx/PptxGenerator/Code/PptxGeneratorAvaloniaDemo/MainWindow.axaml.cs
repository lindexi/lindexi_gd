using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Path = System.IO.Path;

namespace PptxGenerator;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindowViewModel ViewModel => DataContext as MainWindowViewModel ?? throw new InvalidCastException("DataContext must be of type MainWindowViewModel.");

    private void ImageBorder_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (ViewModel.SlideChatManager.PreviewImage is { } previewImage)
        {
            var imageFilePath = Path.Join(AppContext.BaseDirectory, $"PreviewBitmap_{Path.GetRandomFileName()}.png");
            previewImage.Save(imageFilePath);
            Process.Start(new ProcessStartInfo(imageFilePath) { UseShellExecute = true });
        }
    }
}