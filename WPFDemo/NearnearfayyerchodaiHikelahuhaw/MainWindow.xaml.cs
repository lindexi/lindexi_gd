using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
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
using Windows.Win32.System.Com;
using System.Runtime.Versioning;
using System.Windows.Interop;

namespace NearnearfayyerchodaiHikelahuhaw;

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

    private unsafe void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        //Windows.Win32.PInvoke.CoInitializeEx(null, COINIT.COINIT_APARTMENTTHREADED | COINIT.COINIT_DISABLE_OLE1DDE);

        var cw = new StrategyBasedComWrappers();

        var rclsid = Guid.Parse("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7");
        var riid = Guid.Parse("d57c7288-d4ad-4768-be02-9d969532d960");

        void* ppv;

        var hr = CoCreateInstance(&rclsid, IntPtr.Zero,
            CLSCTX.CLSCTX_ALL, &riid, &ppv);
        hr.ThrowOnFailure();

        var fileOpenDialog = (IFileOpenDialog) cw.GetOrCreateObjectForComInstance(new IntPtr(ppv), CreateObjectFlags.None);
        var windowInteropHelper = new WindowInteropHelper(this);
        fileOpenDialog.Show(windowInteropHelper.Handle);
    }

    [DllImport("OLE32.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [SupportedOSPlatform("windows5.0")]
    internal static extern unsafe Windows.Win32.Foundation.HRESULT CoCreateInstance(global::System.Guid* rclsid, IntPtr pUnkOuter, Windows.Win32.System.Com.CLSCTX dwClsContext, global::System.Guid* riid, void* ppv);
}