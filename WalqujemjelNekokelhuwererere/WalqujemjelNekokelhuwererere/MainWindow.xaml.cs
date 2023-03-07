using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.Touch;

namespace WalqujemjelNekokelhuwererere;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        SourceInitialized += MainWindow_SourceInitialized;
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        var windowInteropHelper = new WindowInteropHelper(this);
        var hwnd = windowInteropHelper.Handle;

        // 如果启用了 TWF_WANTPALM ，则不会缓冲触摸输入中的数据包，并且不会在将数据包发送到应用程序之前执行手掌检测。 如果要在处理 WM_TOUCH 消息时实现最小延迟，则启用 TWF_WANTPALM 最有用
        PInvoke.RegisterTouchWindow(new HWND(hwnd), REGISTER_TOUCH_WINDOW_FLAGS.TWF_WANTPALM);

        HwndSource source = HwndSource.FromHwnd(hwnd)!; // 这里在 SourceInitialized 一定是存在的
        source.AddHook(Hook);
    }

    private IntPtr Hook(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
    {
        if (msg == WM_TOUCH)
        {
            // 触摸进来的
            var currentMessageTime = PInvoke.GetMessageTime();
            if (_lastTouchMessageTime != 0)
            {
                var delay = currentMessageTime - _lastTouchMessageTime;
                // 这就是消息的延迟
            }

            _lastTouchMessageTime = currentMessageTime;
        }

        return IntPtr.Zero;
    }

    private int _lastTouchMessageTime;

    private const int WM_TOUCH = 0x0240;
}
