using System.Diagnostics;
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
using Clipboard = System.Windows.Forms.Clipboard;

namespace QarchananaFeweajeka;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var windowInteropHelper = new WindowInteropHelper(this);
        Win32WindowHelper.SetNoActivate(windowInteropHelper.Handle);
    }

    private void SendButton_OnClick(object sender, RoutedEventArgs e)
    {
        SendKeys.SendWait("%="); // 发送 alt+= 让Word打开公式编辑器
        Clipboard.SetText("a^2+b^2=c^2"); // 将文本放入剪贴板
        SendKeys.SendWait("^v"); // 发送 ctrl+v 粘贴文本
        SendKeys.SendWait("{Enter}"); // 发送回车键让 Latex 公式成为 Word 公式
    }
}

public static class Win32WindowHelper
{
    /// <summary>
    /// 使窗口永不激活
    /// </summary>
    /// <param name="hWnd"></param>
    /// [.NET/C# 使窗口永不激活（No Activate 永不获得焦点） - walterlv](https://blog.walterlv.com/post/no-activate-window.html )
    public static void SetNoActivate(IntPtr hWnd)
    {
        var exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
        SetWindowLong(hWnd, GWL_EXSTYLE, new IntPtr(exStyle.ToInt32() | WS_EX_NOACTIVATE));
    }

    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int GWL_EXSTYLE = -20;

    public static IntPtr GetWindowLong(IntPtr hWnd, int nIndex)
    {
        return Environment.Is64BitProcess
            ? GetWindowLong64(hWnd, nIndex)
            : GetWindowLong32(hWnd, nIndex);
    }

    public static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        return Environment.Is64BitProcess
            ? SetWindowLong64(hWnd, nIndex, dwNewLong)
            : SetWindowLong32(hWnd, nIndex, dwNewLong);
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    private static extern IntPtr GetWindowLong32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLong64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    private static extern IntPtr SetWindowLong32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLong64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
}