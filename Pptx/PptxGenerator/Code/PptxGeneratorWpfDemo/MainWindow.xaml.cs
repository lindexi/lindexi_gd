using PptxGenerator;

using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

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
        if (ViewModel.SlideChatManager.PreviewBitmap is { } previewBitmap)
        {
            var imageFilePath = Path.Join(AppContext.BaseDirectory, $"PreviewBitmap_{Path.GetRandomFileName()}.png");
            using var fileStream = new FileStream(imageFilePath, FileMode.Create);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(previewBitmap));
            encoder.Save(fileStream);
            Process.Start(new ProcessStartInfo(imageFilePath) { UseShellExecute = true });
        }
    }
}