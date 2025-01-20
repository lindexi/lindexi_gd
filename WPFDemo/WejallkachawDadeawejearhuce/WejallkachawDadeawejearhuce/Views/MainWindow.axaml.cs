using Avalonia.Controls;
using Avalonia.Platform;

namespace WejallkachawDadeawejearhuce.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        WindowState = WindowState.FullScreen;
        ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome;
    }
}
