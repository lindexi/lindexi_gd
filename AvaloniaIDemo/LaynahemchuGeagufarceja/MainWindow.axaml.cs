using Avalonia.Controls;
using Avalonia.Interactivity;

namespace LaynahemchuGeagufarceja;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}