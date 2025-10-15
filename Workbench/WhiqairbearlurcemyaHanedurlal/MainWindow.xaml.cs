using System.Diagnostics;
using System.Runtime.InteropServices;
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

namespace WhiqairbearlurcemyaHanedurlal;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        LogTextBlock.Text = IFPD.GetQuickRenderingStatus() ? "Quick Rendering is enabled." : "Quick Rendering is disabled.";
    }

    private void SelectionModeButton_OnClick(object sender, RoutedEventArgs e)
    {
        InkCanvas.EditingMode = InkCanvasEditingMode.Select;

        var stopwatch = Stopwatch.StartNew();
        var result = IFPD.EnableQuickRendering(false);
        stopwatch.Stop();
        LogTextBlock.Text = $"EnableQuickRendering(false) Result={result}; Cost={stopwatch.ElapsedMilliseconds};" + (IFPD.GetQuickRenderingStatus()
            ? "Quick Rendering is enabled."
            : "Quick Rendering is disabled.");
    }

    private void PenModeButton_OnClick(object sender, RoutedEventArgs e)
    {
        InkCanvas.EditingMode = InkCanvasEditingMode.Ink;

        var stopwatch = Stopwatch.StartNew();
        var result = IFPD.EnableQuickRendering(true);
        stopwatch.Stop();
        LogTextBlock.Text = $"EnableQuickRendering(true) Result={result}; Cost={stopwatch.ElapsedMilliseconds};" + (IFPD.GetQuickRenderingStatus()
            ? "Quick Rendering is enabled."
            : "Quick Rendering is disabled.");
    }
}

static class IFPD
{
    [DllImport(LibName, EntryPoint = "IFPDEnableQuickRendering")]
    public static extern Result EnableQuickRendering(bool enable);

    [DllImport(LibName, EntryPoint = "IFPDGetQuickRenderingStatus")]
    public static extern bool GetQuickRenderingStatus();

    [DllImport(LibName, EntryPoint = "IFPDResetQuickRendering")]
    public static extern Result ResetQuickRendering();

    const string LibName = "IFPD.TouchLatency32.dll";

    public enum Result : int
    {
        Success = 0,

        /// <summary>
        /// Operation failed, no privilege
        /// 没有权限的情况，如有其他应用正在占用此功能，需要等进程退出或调用 Reset 接口释放资源
        /// </summary>
        NoPrivilege = 0x40000001,

        /// <summary>
        /// Operation failed, platform is of no cap.
        /// 设备没这个能力
        /// </summary>
        NoCapability = 0x40000002,

        /// <summary>
        /// Operation failed.
        /// </summary>
        Failed = 0x40000003,
    }
}