using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using JawjeleceeYairlubelhearrene.ViewModels;

namespace JawjeleceeYairlubelhearrene;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void Window_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return;
        }

        if (e.Data.GetData(DataFormats.FileDrop) is not string[] filePaths || filePaths.Length == 0)
        {
            return;
        }

        await ViewModel.HandleDroppedFileAsync(filePaths[0]);
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.HasGeneratedVideo)
        {
            return;
        }

        PreviewMediaElement.Play();
        ViewModel.PlayVideoRequested();
    }

    private void PauseButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.HasGeneratedVideo)
        {
            return;
        }

        PreviewMediaElement.Pause();
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.HasGeneratedVideo)
        {
            return;
        }

        PreviewMediaElement.Stop();
    }

    private void OpenWithExternalPlayerButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.HasGeneratedVideo)
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = ViewModel.GeneratedVideoPath,
            UseShellExecute = true
        });
    }

    private void PreviewMediaElement_MediaOpened(object sender, RoutedEventArgs e)
    {
        PreviewMediaElement.Stop();
    }
}