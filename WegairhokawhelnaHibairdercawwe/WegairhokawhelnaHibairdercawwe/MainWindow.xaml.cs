using System.Diagnostics;
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
using Windows.Win32;

namespace WegairhokawhelnaHibairdercawwe;

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
        const int WM_LBUTTONDOWN = 0x0201;
        if (msg == WM_LBUTTONDOWN)
        {
            var messageExtraInfo = PInvoke.GetMessageExtraInfo();
            var value = messageExtraInfo.Value.ToInt64();
            var mask = 0xFFFFFF80;
            var result = value & mask;

            if (result == 0xFF515780)
            {
                // 这是 Touch 过来
            }
            else if (result == 0xFF515700)
            {
                // 收到 Pen 的
            }
            else if (value == 0)
            {
                // 这是鼠标
            }
        }

        return IntPtr.Zero;
    }
}