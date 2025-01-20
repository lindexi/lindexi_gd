using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

using UnoInk.Inking.InkCore;

using WejallkachawDadeawejearhuce.Inking.Contexts;
using WejallkachawDadeawejearhuce.Inking.WpfInking;

namespace WejallkachawDadeawejearhuce.Wpf.Inking;

public class WpfInkCanvasWindow : Window, IWpfInkCanvasWindow
{
    public WpfInkCanvasWindow()
    {
        Left = 0;
        Top = 0;

        AllowsTransparency = true;
        WindowStyle = WindowStyle.None;
        WindowState = WindowState.Maximized;
        Background = Brushes.Transparent;
        Topmost = true;

        Content = _wpfInkCanvas = new WpfInkCanvas();

        Loaded += WpfInkCanvasWindow_Loaded;
    }

    private readonly WpfInkCanvas _wpfInkCanvas;

    public IWpfInkCanvas WpfInkCanvas => _wpfInkCanvas;

    private void WpfInkCanvasWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _wpfInkCanvas.Down(new InkingInputArgs(10, new StylusPoint(0, 0)));
        for (int i = 0; i < 1000; i++)
        {
            _wpfInkCanvas.Move(new InkingInputArgs(10, new StylusPoint(i, i)));
        }
        _wpfInkCanvas.Up(new InkingInputArgs(10, new StylusPoint(1000, 1000)));
    }

    void IWpfInkCanvasWindow.Show()
    {
        Dispatcher.Invoke(Show, DispatcherPriority.Send);
    }

    void IWpfInkCanvasWindow.Hide()
    {
        Dispatcher.Invoke(Hide, DispatcherPriority.Send);
    }

    void IWpfInkCanvasWindow.Close()
    {
        Dispatcher.Invoke(Close, DispatcherPriority.Send);
    }
}