using System;
using System.Diagnostics;
using winmdroot = global::Windows.Win32;
using System.Runtime.InteropServices;
using WinRT.Interop;
using InkPresenter = Windows.UI.Input.Inking.InkPresenter;
using Point = Windows.Foundation.Point;
using Windows.Devices.Enumeration.Pnp;
using System.Threading;
using Windows.Win32.System.Com;
using WinRT;

namespace WerlelerefaywaNenurkecearcel;

public class WinUIInk
{
    public unsafe void Start()
    {
        var thread = new Thread(() =>
        {
            global::WinRT.ComWrappersSupport.InitializeComWrappers();

            // <WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
            // <WindowsPackageType>None</WindowsPackageType>
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

                global::System.Guid* riidLocal = &riidInkDesktopHost;
                global::System.Guid* rclsidLocal = &rclsidInkDesktopHost;
                void* ppv;
                var hResult = CoCreateInstance(rclsidLocal, null, CLSCTX.CLSCTX_INPROC_SERVER, riidLocal, &ppv);

                // 测试 AddRef 方法
                void*** vftbl = (void***)ppv;
                ((delegate* unmanaged[Stdcall]<void*, int>) (*vftbl)[1])(ppv);

                // 测试 Release 方法
                ((delegate* unmanaged[Stdcall]<void*, int>) (*vftbl)[2])(ppv);

                // 测试 QueryInterface 方法
                void* inkDesktopHost;
                var rv = ((delegate* unmanaged[Stdcall]<void*, void*, void*, int>) (*vftbl)[0])(ppv, riidLocal, &inkDesktopHost);
                Debug.Assert(inkDesktopHost == ppv);

                // 测试 CreateInkPresenter 方法
                var rridIInkPresenterDesktop = new Guid("73f3c0d9-2e8b-48f3-895e-20cbd27b723b");
                void* inkPresenterDesktop;

                rv = ((delegate* unmanaged[Stdcall]<void*, Guid*, void**, int>) (*vftbl)[4])(ppv, &rridIInkPresenterDesktop, &inkPresenterDesktop);
                //inkDesktopHost.CreateInkPresenter(&rridIInkPresenterDesktop, out var inkPresenterDesktop);

                ;

                //winmdroot.PInvoke.CoCreateInstance(rclsidInkDesktopHost, null, CLSCTX.CLSCTX_INPROC_SERVER,
                //    out InkDesktopHost inkDesktopHost);

                //var t = inkDesktopHost.As<InkDesktopHost>();
                //var tryGetComInstance = ComWrappers.TryGetComInstance(inkDesktopHost, out var unknown);

                //var rridIInkPresenterDesktop = new Guid("73f3c0d9-2e8b-48f3-895e-20cbd27b723b");
                //inkDesktopHost.CreateInkPresenter(&rridIInkPresenterDesktop, out var inkPresenterDesktop);

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


    [DllImport("OLE32.dll", ExactSpelling = true)]
    internal static extern unsafe winmdroot.Foundation.HRESULT CoCreateInstance(global::System.Guid* rclsid,
        [MarshalAs(UnmanagedType.IUnknown)] object pUnkOuter, winmdroot.System.Com.CLSCTX dwClsContext,
        global::System.Guid* riid, void** ppv);
}