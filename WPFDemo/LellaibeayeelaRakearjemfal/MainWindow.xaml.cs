using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace LellaibeayeelaRakearjemfal;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        SourceInitialized += OnSourceInitialized;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var windowInteropHelper = new WindowInteropHelper(this);
        var hwnd = windowInteropHelper.Handle;

        HwndSource source = HwndSource.FromHwnd(hwnd)!;
        source.AddHook(Hook);
    }

    private IntPtr Hook(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
    {
        const int WM_SYSCOLORCHANGE = 0x0015;
        const int WM_THEMECHANGED = 0x031A;
        const int WM_DWMCOLORIZATIONCOLORCHANGED = 0x0320;
        if (msg is WM_SYSCOLORCHANGE or WM_THEMECHANGED or WM_DWMCOLORIZATIONCOLORCHANGED)
        {
            Window window = this;
            var renderHelper = new RenderHelper(window);
            renderHelper.ForceRedraw();
        }

        return IntPtr.Zero;
    }

    private void UpdateButton_OnClick(object sender, RoutedEventArgs e)
    {
        TheTextBlock.Text = $"abc{Random.Shared.Next(10)}";
        InvalidateVisual();
        UpdateLayout();
    }
}

class RenderHelper
{
    public RenderHelper(Window window)
    {
        _window = window;
    }

    private readonly Window _window;

    public void ForceRedraw()
    {
        _window.Dispatcher.InvokeAsync(() =>
        {
            InvalidateAllVisual(_window);

            CompositionTarget.Rendering += CompositionTargetOnRendering;
        }, DispatcherPriority.Render);
    }

    private int _currentCount;

    void CompositionTargetOnRendering(object? sender, EventArgs e)
    {
        if (_currentCount >= 2)
        {
            CompositionTarget.Rendering -= CompositionTargetOnRendering;
        }

        _currentCount++;
        InvalidateAllVisual(_window);
    }

    private void InvalidateAllVisual(DependencyObject element)
    {
        var childrenCount = VisualTreeHelper.GetChildrenCount(element);
        for (int i = 0; i < childrenCount; i++)
        {
            var child = VisualTreeHelper.GetChild(element, i);
            InvalidateAllVisual(child);
        }

        if (element is UIElement uiElement)
        {
            uiElement.InvalidateVisual();
        }
    }
}