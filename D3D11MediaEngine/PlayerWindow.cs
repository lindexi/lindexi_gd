using Lsj.Util.Win32;
using Lsj.Util.Win32.BaseTypes;
using Lsj.Util.Win32.ComInterfaces;
using Lsj.Util.Win32.DirectX.ComInterfaces;
using Lsj.Util.Win32.Enums;
using Lsj.Util.Win32.Extensions.COM;
using Lsj.Util.Win32.Extensions.DirectX.Direct3D11;
using Lsj.Util.Win32.MediaFoundation.ComInterfaces;
using Lsj.Util.Win32.MediaFoundation.Enums;
using Lsj.Util.Win32.MediaFoundation.Structs;
using Lsj.Util.Win32.NativeUI.DirectX;
using Lsj.Util.Win32.Structs;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static Lsj.Util.Win32.Constants;
using static Lsj.Util.Win32.DirectX.Enums.D3D11_CREATE_DEVICE_FLAG;
using static Lsj.Util.Win32.DirectX.Enums.DXGI_FORMAT;
using static Lsj.Util.Win32.MediaFoundation.GUIDs.MediaFoundationAttributes;
using static Lsj.Util.Win32.MediaFoundation.Mfplat;
using static Lsj.Util.Win32.UnsafePInvokeExtensions;

namespace D3D11MediaEngine
{
    public class PlayerWindow : D3D11Window
    {
        public P<IMFDXGIDeviceManager> DXGIDeviceManager { get; private set; }

        public P<IMFMediaEngine> MFMediaEngine { get; private set; }

        public Stopwatch PlayerStopwatch { get; } = new Stopwatch();

        protected override bool OnAfterInitD3D11(HWND hWnd)
        {
            if (!MFCreateDXGIDeviceManager(out var resetToken, out var deviceManager))
            {
                return false;
            }

            if (!deviceManager.Value.ResetDevice(Device.AsIUnknown(), resetToken))
            {
                deviceManager.AsIUnknown().Value.Release();
                return false;
            }

            DXGIDeviceManager = deviceManager;

            var multhread = DeviceContext.AsIUnknown().ToInterface<ID3D11Multithread>(IIDs.IID_ID3D11Multithread);
            multhread.Value.SetMultithreadProtected(true);
            multhread.AsIUnknown().Value.Release();

            var factory = COMExtensions.CreateInstance<IMFMediaEngineClassFactory>(CLSIDs.CLSID_MFMediaEngineClassFactory, IIDs.IID_IMFMediaEngineClassFactory);
            MFCreateAttributes(out var mediaEngineAttributes, 3);

            var callback = new MediaEngineCallback();
            mediaEngineAttributes.Value.SetUnknown(MF_MEDIA_ENGINE_CALLBACK, callback.GetNativePointer().AsIUnknown());
            mediaEngineAttributes.Value.SetUnknown(MF_MEDIA_ENGINE_DXGI_MANAGER, DXGIDeviceManager.AsIUnknown());
            mediaEngineAttributes.Value.SetUINT32(MF_MEDIA_ENGINE_VIDEO_OUTPUT_FORMAT, (uint)DXGI_FORMAT_B8G8R8A8_UNORM);

            factory.Value.CreateInstance(0, mediaEngineAttributes, out var engine);

            mediaEngineAttributes.AsIUnknown().Value.Release();
            factory.AsIUnknown().Value.Release();

            MFMediaEngine = engine;


            var url = OleAut32.SysAllocString(Program.VideoPath);
            engine.Value.SetSource(url);
            OleAut32.SysFreeString(url);
            engine.Value.SetLoop(true);
            engine.Value.Play();

            PlayerStopwatch.Restart();

            return true;
        }


        private void RenderFrame()
        {
            if (MFMediaEngine != NULL)
            {
                var result = MFMediaEngine.Value.OnVideoStreamTick(out var pts);

                if (result == HRESULT.S_OK)
                {
                    SwapChain.Value.GetBuffer(0, IIDs.IID_ID3D11Texture2D, out var backBufferPtr);
                    P<ID3D11Texture2D> backBuffer = backBufferPtr;
                    backBuffer.Value.GetDesc(out var backDesc);
                    result = MFMediaEngine.Value.TransferVideoFrame(backBuffer.AsIUnknown(), NullRef<MFVideoNormalizedRect>(), NullRef<RECT>(), NullRef<MFARGB>());
                    backBuffer.AsIUnknown().Value.Release();

                    SwapChain.Value.Present(1, 0);
                }
            }
        }


        protected override LRESULT OnPaint(HWND hWnd, WPARAM wParam, LPARAM lParam)
        {
            RenderFrame();
            return 0;
        }

        protected override LRESULT OnSize(HWND hWnd, WPARAM wParam, LPARAM lParam, WindowSizingTypes sizingType, int width, int height)
        {
            base.OnSize(hWnd, wParam, lParam, sizingType, width, height);
            return 0;
        }

        protected override P<ID3D11Device> CreateD3D11Device()
        {
#if DEBUG
            DeviceHelper.CreateDevice(out var device, out NullRef<P<ID3D11DeviceContext>>(), D3D11_CREATE_DEVICE_DEBUG);
            return device;
#endif
            return base.CreateD3D11Device();
        }

        class MediaEngineCallback : BaseComObject<IMFMediaEngineNotify>
        {
            protected override int VTableItemCount => 4;

            protected override void InitVTable(VTableInitHelper<IMFMediaEngineNotify> vTable)
            {
                vTable.SetMethod(3, GetMethodInfo(nameof(EventNotify)));
            }

            public override HRESULT QueryInterface(in IID riid, out nint ppvObject)
            {
                if (riid == IIDs.IID_IMFMediaEngineNotify)
                {
                    AddRef();
                    ppvObject = GetNativePointer();
                    return HRESULT.S_OK;
                }
                return base.QueryInterface(riid, out ppvObject);
            }

            public HRESULT EventNotify([In] MF_MEDIA_ENGINE_EVENT @event, [In] DWORD_PTR param1, [In] DWORD param2)
            {
                Console.WriteLine($"[MediaEngineCallback] {@event}");
                return HRESULT.S_OK;
            }
        }
    }
}
