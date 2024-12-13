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
using Windows.Devices.Enumeration.Pnp;

namespace ChoneceabuQeceewiwufe;

//[Guid("062584A6-F830-4BDC-A4D2-0A10AB062B1D")]
//public unsafe partial struct InkDesktopHost : INativeGuid
//{
//    static Guid* INativeGuid.NativeGuid => (Guid*) Unsafe.AsPointer(ref Unsafe.AsRef(in CLSID_InkDesktopHost));
//}

[ComImport, Guid("4ce7d875-a981-4140-a1ff-ad93258e8d59")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public unsafe interface InkDesktopHost
{
    void QueueWorkItem(IntPtr workItem); 
    void CreateInkPresenter(Guid* riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppv); 
    void CreateAndInitializeInkPresenter([MarshalAs(UnmanagedType.Interface)] object rootVisual, float width, float height, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppv);
}

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

                var hResult = PInvoke.CoCreateInstance(rclsidInkDesktopHost, null, CLSCTX.CLSCTX_INPROC_SERVER,
                    out InkDesktopHost inkDesktopHost);

                var t = inkDesktopHost.As<InkDesktopHost>();
                var tryGetComInstance = ComWrappers.TryGetComInstance(inkDesktopHost,out var unknown);

                var rridIInkPresenterDesktop = new Guid("73f3c0d9-2e8b-48f3-895e-20cbd27b723b");
                inkDesktopHost.CreateInkPresenter(&rridIInkPresenterDesktop,out var inkPresenterDesktop);

                //void* inkDesktopHost = (void*) IntPtr.Zero;
                //var hResult = CoCreateInstance(&rclsidInkDesktopHost, null, CLSCTX.CLSCTX_INPROC_SERVER,
                //    &riidInkDesktopHost,
                //    &inkDesktopHost);

                //Marshal.GetObjectForIUnknown(new IntPtr(inkDesktopHost))

                //IUnknownVftbl* v = *((IUnknownVftbl**) inkDesktopHost);
                //void* inkPresenterDesktop = default;

                //v->QueryInterface(new IntPtr(inkDesktopHost), &riidInkDesktopHost, (nint*) inkPresenterDesktop);
                ////PInvoke.CoGetClassObject(&rclsidInkDesktopHost,CLSCTX.CLSCTX_INPROC_SERVER,null,&riidInkDesktopHost,IUnknownVftbl)

                //var lpVtbl = (void**) inkDesktopHost;
                //// 遇到 0x80040154 是因为将 riidInkDesktopHost 当成 rclsidInkDesktopHost 传入 

                //if (hResult.Failed)
                //{
                //}

                // QueryInterface - 0
                //((delegate* unmanaged[Cdecl]<void*, Guid*, void**, int>) lpVtbl[0])((void*) inkDesktopHost, &rridIInkPresenterDesktop, &inkPresenterDesktop);

                // AddRef - 1
                // Release - 2
                /*
                   MIDL_INTERFACE("4ce7d875-a981-4140-a1ff-ad93258e8d59")
                   IInkDesktopHost : public IUnknown
                   {
                   public:
                       virtual HRESULT STDMETHODCALLTYPE QueueWorkItem(
                           /* [in] * / __RPC__in_opt IInkHostWorkItem *workItem) = 0;

                       virtual HRESULT STDMETHODCALLTYPE CreateInkPresenter(
                           /* [in] * / __RPC__in REFIID riid,
                           /* [iid_is][out] * / __RPC__deref_out_opt void **ppv) = 0;

                       virtual HRESULT STDMETHODCALLTYPE CreateAndInitializeInkPresenter(
                           /* [in] * / __RPC__in_opt IUnknown *rootVisual,
                           /* [in] * / float width,
                           /* [in] * / float height,
                           /* [in] * / __RPC__in REFIID riid,
                           /* [iid_is][out] * / __RPC__deref_out_opt void **ppv) = 0;

                   };
                 */
                // QueueWorkItem - 4
                // CreateInkPresenter - 5
                // CreateAndInitializeInkPresenter - 6
                // CreateInkPresenter
                // IInkPresenterDesktop 
                //var result = ((delegate* unmanaged[Stdcall]<IntPtr, Guid*, void**, int>) lpVtbl[1])(inkDesktopHost, &rridIInkPresenterDesktop, &inkPresenterDesktop);
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
        global::System.Guid* riid, void** ppv);
}