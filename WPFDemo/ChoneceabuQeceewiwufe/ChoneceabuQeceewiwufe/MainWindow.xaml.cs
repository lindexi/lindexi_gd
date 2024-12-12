using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.Foundation;
using Windows.UI.Input.Inking;
using Windows.UI.Input.Inking.Analysis;
using Windows.UI.Input.Inking.Core;
using Windows.Win32;
using Windows.Win32.System.Com;
using WinRT;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WinRT.Interop;
using InkPresenter = Windows.UI.Input.Inking.InkPresenter;
using Point = Windows.Foundation.Point;
using winmdroot = global::Windows.Win32;

namespace ChoneceabuQeceewiwufe;

//[Guid("062584A6-F830-4BDC-A4D2-0A10AB062B1D")]
//public unsafe partial struct InkDesktopHost : INativeGuid
//{
//    static Guid* INativeGuid.NativeGuid => (Guid*) Unsafe.AsPointer(ref Unsafe.AsRef(in CLSID_InkDesktopHost));
//}

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
        var thread = new Thread(() =>
        {
            global::WinRT.ComWrappersSupport.InitializeComWrappers();

            // 如果不在 csproj 加上
            // <WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
            // <WindowsPackageType>None</WindowsPackageType>
            // 将抛出 没有注册类 (0x80040154 (REGDB_E_CLASSNOTREG))
            global::Microsoft.UI.Xaml.Application.Start(p =>
            {
                //  __RP_Marker_ClassById(RuntimeProfiler::ProfId_InkCanvas);
                // https://learn.microsoft.com/en-us/windows/win32/input_ink/inkdesktophost
                //// 0x8001010e
                //var coreInkPresenterHost = new CoreInkPresenterHost();
                // winrt::check_hresult(CoCreateInstance(__uuidof(InkDesktopHost), nullptr, CLSCTX_INPROC_SERVER, IID_PPV_ARGS(m_threadData->m_inkHost.put())));
                // https://github.com/microsoft/microsoft-ui-xaml/blob/ff21f9b212cea2191b959649e45e52486c8465aa/src/controls/dev/InkCanvas/InkCanvas.cpp#L92C9-L92C150

                // InkPresenterDesktop.h
                // MIDL_INTERFACE("4ce7d875-a981-4140-a1ff-ad93258e8d59")
                // IInkDesktopHost : public IUnknown
                var riidInkDesktopHost = new Guid("4ce7d875-a981-4140-a1ff-ad93258e8d59");
                // class DECLSPEC_UUID("062584a6-f830-4bdc-a4d2-0a10ab062b1d") InkDesktopHost;
                var rclsidInkDesktopHost = new Guid("062584A6-F830-4BDC-A4D2-0A10AB062B1D");

                fixed (global::System.Guid* riidLocal = &riidInkDesktopHost)
                {
                    fixed (global::System.Guid* rclsidLocal = &rclsidInkDesktopHost)
                    {
                        var hResult = CoCreateInstance(rclsidLocal, null, CLSCTX.CLSCTX_INPROC_SERVER, riidLocal,
                            out var inkDesktopHost);

                        // 遇到 0x80040154 是因为将 riidInkDesktopHost 当成 rclsidInkDesktopHost 传入 

                        if (hResult.Failed)
                        {
                        }
                    }
                }
            });
        });
        thread.IsBackground = true;
        thread.Start();
    }

    private async void InkCanvas_OnStrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
    {
        var inkStrokeBuilder = new InkStrokeBuilder();
        var inkStroke = inkStrokeBuilder.CreateStroke(e.Stroke.StylusPoints.Select(t => new Point(t.X, t.Y)));
        var inkAnalyzer = new InkAnalyzer();
        inkAnalyzer.AddDataForStroke(inkStroke);
        var result = await inkAnalyzer.AnalyzeAsync();
        foreach (IInkAnalysisNode inkAnalysisNode in inkAnalyzer.AnalysisRoot.FindNodes(InkAnalysisNodeKind.InkDrawing))
        {
            var inkAnalysisInkDrawing = inkAnalysisNode as InkAnalysisInkDrawing;
            var value = inkAnalysisInkDrawing?.DrawingKind;
            if (value == InkAnalysisDrawingKind.Triangle)
            {
                MessageBox.Show("xx");
            }
        }
    }

    [DllImport("OLE32.dll", ExactSpelling = true)]
    internal static extern unsafe winmdroot.Foundation.HRESULT CoCreateInstance(global::System.Guid* rclsid,
        [MarshalAs(UnmanagedType.IUnknown)] object pUnkOuter, winmdroot.System.Com.CLSCTX dwClsContext,
        global::System.Guid* riid, out IntPtr ppv);
}