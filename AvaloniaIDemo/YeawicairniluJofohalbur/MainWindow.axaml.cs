using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace YeawicairniluJofohalbur
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void OpenFileDialogButton_OnClick(object? sender, RoutedEventArgs e)
        {
            await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions());
        }
    }
}