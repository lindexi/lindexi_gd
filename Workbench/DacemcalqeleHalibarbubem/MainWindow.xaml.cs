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

        Opacity = 0.6;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
    }

    private readonly Stopwatch _lastCtrlKeyDown = new Stopwatch();

    private async void KeyboardHookListener_KeyDown(object? sender, KeyboardHookListener.RawKeyEventArgs args)
    {
        if (args.Key == Key.LeftCtrl)
        {   
            if (_lastCtrlKeyDown.IsRunning && _lastCtrlKeyDown.Elapsed < TimeSpan.FromMilliseconds(500))
            {
                MessageTextBox.Text = $"{DateTime.Now}";
                Show();
                Topmost = true;
                Topmost = false;

                await Task.Delay(1000);
                Hide();
            }
            else
            {
                _lastCtrlKeyDown.Restart();
            }
        }
    }

    private void KeyboardHookListener_KeyUp(object? sender, KeyboardHookListener.RawKeyEventArgs args)
    {
        
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        DragMove();
    }
}