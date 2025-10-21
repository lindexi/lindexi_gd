using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
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
using Application = Microsoft.Office.Interop.PowerPoint.Application;

namespace NayernajojanaiYayhefalur;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var id = "PowerPoint.Application";
        CLSIDFromProgIDEx(id, out var guid);
        GetActiveObject(ref guid, IntPtr.Zero, out var obj);
        Microsoft.Office.Interop.PowerPoint.Application? app = obj as Application;
        if (app is null)
        {
            return;
        }

        var appName = app.Name;
        var caption = app.Caption;
        TextBlock.Text = $"当前打开 {caption} - {appName}";
        app.SlideShowNextSlide += App_SlideShowNextSlide;
    }

    private void App_SlideShowNextSlide(Microsoft.Office.Interop.PowerPoint.SlideShowWindow wn)
    {
        TextBlock.Text = $"当前播放到第 {wn.View.CurrentShowPosition} 页";
    }

    //[DllImport(Microsoft.Win32.Win32Native.OLE32, PreserveSig = false)]
    [DllImport("ole32.dll", PreserveSig = false)]
    [ResourceExposure(ResourceScope.None)]
    [SuppressUnmanagedCodeSecurity]
    [System.Security.SecurityCritical]  // auto-generated
    private static extern void CLSIDFromProgIDEx([MarshalAs(UnmanagedType.LPWStr)] string progId, out Guid clsid);

    //[DllImport(Microsoft.Win32.Win32Native.OLE32, PreserveSig = false)]
    [DllImport("ole32.dll", PreserveSig = false)]
    [ResourceExposure(ResourceScope.None)]
    [SuppressUnmanagedCodeSecurity]
    [System.Security.SecurityCritical]  // auto-generated
    private static extern void CLSIDFromProgID([MarshalAs(UnmanagedType.LPWStr)] string progId, out Guid clsid);

    //[DllImport(Microsoft.Win32.Win32Native.OLEAUT32, PreserveSig = false)]
    [DllImport("oleaut32.dll", PreserveSig = false)]
    [ResourceExposure(ResourceScope.None)]
    [SuppressUnmanagedCodeSecurity]
    [System.Security.SecurityCritical]  // auto-generated
    private static extern void GetActiveObject(ref Guid rclsid, IntPtr reserved, [MarshalAs(UnmanagedType.Interface)] out object ppunk);
}