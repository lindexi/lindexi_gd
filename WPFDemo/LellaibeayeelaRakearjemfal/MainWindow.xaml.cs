using System.Runtime.InteropServices;
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

        }

        const int WM_NCPAINT = 0x0085;
        const int WM_SETTINGCHANGE = 0x001A;
        if (msg is WM_NCPAINT or WM_SETTINGCHANGE)
        {
            Window window = this;
            var rect = new Int32Rect(0,0, (int)Math.Ceiling(window.ActualWidth), (int) Math.Ceiling(window.ActualHeight));
            unsafe
            {
                InvalidateRect(hwnd, &rect, true);
            }
        }

        return IntPtr.Zero;
    }

    [DllImport("User32")]
    private static extern unsafe bool InvalidateRect(IntPtr hwnd, Int32Rect* lpRect, bool bErase);

    private void UpdateButton_OnClick(object sender, RoutedEventArgs e)
    {
        TheTextBlock.Text = $"abc{Random.Shared.Next(10)}";
        InvalidateVisual();
        UpdateLayout();
    }
}
