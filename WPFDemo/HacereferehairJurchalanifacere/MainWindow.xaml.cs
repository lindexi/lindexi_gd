using System.IO;
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
using Clipboard = System.Windows.Clipboard;
using DataObject = System.Windows.DataObject;

namespace HacereferehairJurchalanifacere;

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
        // lang=xml
        var mathML = """
                     <?xml version="1.0"?>
                     <math xmlns="http://www.w3.org/1998/Math/MathML" xmlns:m="http://schemas.openxmlformats.org/officeDocument/2006/math">
                         <mrow>
                             <msup><mi>a</mi><mn>2</mn></msup>
                             <mo>+</mo>
                             <msup><mi>b</mi><mn>2</mn></msup>
                             <mo>=</mo>
                             <msup><mi>c</mi><mn>2</mn></msup>
                         </mrow>
                     </math>
                     """;

        var dataObject = new DataObject();
        var buffer = Encoding.UTF8.GetBytes(mathML);
        var memoryStream = new MemoryStream(buffer);
        dataObject.SetData("MathML Presentation", memoryStream);
        Clipboard.SetDataObject(dataObject);

        SendKeys.SendWait("^v"); // 发送 ctrl+v 粘贴文本
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