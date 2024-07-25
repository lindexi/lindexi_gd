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

using Windows.Win32;

namespace DacemcalqeleHalibarbubem;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        KeyboardHookListener.KeyDown += KeyboardHookListener_KeyDown;
        KeyboardHookListener.KeyUp += KeyboardHookListener_KeyUp;
        KeyboardHookListener.HookKeyboard();

        Loaded += MainWindow_Loaded;

        AllowsTransparency = true;
        Opacity = 0.7;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
    }

    private readonly Stopwatch _lastCtrlKeyDown = new Stopwatch();

    private void KeyboardHookListener_KeyDown(object? sender, KeyboardHookListener.RawKeyEventArgs args)
    {
        if (args.Key == Key.LWin)
        {
            ShowNotActive();
        }
    }

    private async void ShowNotActive()
    {
        MessageTextBox.Text = $"{DateTime.Now}";
        Show();
        Topmost = true;
        Topmost = false;

        _isHiding = true;
        await Task.Delay(TimeSpan.FromSeconds(10));
        if (_isHiding)
        {
            Hide();
        }
    }

    private void KeyboardHookListener_KeyUp(object? sender, KeyboardHookListener.RawKeyEventArgs args)
    {
        if (args.Key == Key.LeftCtrl)
        {
            if (_lastCtrlKeyDown.IsRunning && _lastCtrlKeyDown.Elapsed < TimeSpan.FromMilliseconds(500))
            {
                ShowNotActive();
            }
            else
            {
                _lastCtrlKeyDown.Restart();
            }
        }
    }

    private bool _isHiding = false;
    protected override void OnPreviewMouseMove(MouseEventArgs e)
    {
        _isHiding = false;
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        DragMove();
    }
}