using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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

using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace CucherelahiBewilargalkalbea;
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
        HookUIAutomationMessage(hwnd);
    }

    private void HookUIAutomationMessage(IntPtr windowPtr)
    {
        // 获取原本的消息处理
        var hWnd = new HWND(windowPtr);
        _oldWndProc = (IntPtr) Windows.Win32.PInvoke.GetWindowLong(hWnd, WINDOW_LONG_PTR_INDEX.GWL_WNDPROC);

        // 加入新的过滤处理
        WindowProc? hookUIAutomationMessageWndProc = HookUIAutomationMessageWndProc;
        _hookUIAutomationMessageWndProcPointer = Marshal.GetFunctionPointerForDelegate(hookUIAutomationMessageWndProc);
        Windows.Win32.PInvoke.SetWindowLong(hWnd, WINDOW_LONG_PTR_INDEX.GWL_WNDPROC,
            // 这里强转 int 只有 x86 下才可用
            (int) _hookUIAutomationMessageWndProcPointer);
    }

    private IntPtr HookUIAutomationMessageWndProc(IntPtr hwnd, uint msg, IntPtr wparam, IntPtr lparam)
    {
        const uint WM_GETOBJECT = 0x003D;
        if (msg == WM_GETOBJECT)
        {
            return IntPtr.Zero;
        }

        // 以下是调试代码
        if (msg is 0x0201 or 0x0202)
        {
            return IntPtr.Zero;
        }

        // 过滤通过之后，再调用原先的消息循环
        return CallWindowProc(_oldWndProc, hwnd, msg, wparam, lparam);
    }

    public delegate IntPtr WindowProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

    private IntPtr _oldWndProc;
    private IntPtr _hookUIAutomationMessageWndProcPointer;

    [DllImport("user32", CharSet = CharSet.Unicode)]
    public static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint uMsg, IntPtr wParam,
        IntPtr lParam);

    private void Button_OnClick(object sender, RoutedEventArgs e)
    {
        throw new NotSupportedException($"不应该进来，因为鼠标被 Hook 了");
    }
}
