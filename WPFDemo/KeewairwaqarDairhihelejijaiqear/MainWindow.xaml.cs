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

namespace KeewairwaqarDairhihelejijaiqear;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        SourceInitialized += MainWindow_SourceInitialized;
        Loaded+=OnLoaded;

        

        void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            SourceInitialized -= MainWindow_SourceInitialized;
            var windowInteropHelper = new WindowInteropHelper(this);
            int cloak = 1; // 1 to enable cloaking, 0 to disable
            DwmSetWindowAttribute(windowInteropHelper.Handle, DWMWINDOWATTRIBUTE.Cloak, ref cloak, sizeof(int));
        }

        void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            var windowInteropHelper = new WindowInteropHelper(this);
            int cloak = 0; // 1 to enable cloaking, 0 to disable
            DwmSetWindowAttribute(windowInteropHelper.Handle, DWMWINDOWATTRIBUTE.Cloak, ref cloak, sizeof(int));
        }
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    public static extern int DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE attr, ref int attrValue, int attrSize);
}

public enum DWMWINDOWATTRIBUTE : uint
{
    NCRenderingEnabled = 1,
    NCRenderingPolicy,
    TransitionsForceDisabled,
    AllowNCPaint,
    CaptionButtonBounds,
    NonClientRtlLayout,
    ForceIconicRepresentation,
    Flip3DPolicy,
    ExtendedFrameBounds,
    HasIconicBitmap,
    DisallowPeek,
    ExcludedFromPeek,
    Cloak,
    Cloaked,
    FreezeRepresentation
}