using Lsj.Util;
using Lsj.Util.Win32;
using Lsj.Util.Win32.BaseTypes;
using Lsj.Util.Win32.ComInterfaces;
using Lsj.Util.Win32.DirectX.ComInterfaces;
using Lsj.Util.Win32.DirectX.Enums;
using Lsj.Util.Win32.DirectX.Structs;
using Lsj.Util.Win32.Enums;
using Lsj.Util.Win32.Extensions.COM;
using Lsj.Util.Win32.Extensions.DirectX.Direct3D11;
using Lsj.Util.Win32.Extensions.MediaFoundation;
using Lsj.Util.Win32.MediaFoundation.ComInterfaces;
using Lsj.Util.Win32.MediaFoundation.Enums;
using Lsj.Util.Win32.MediaFoundation.GUIDs;
using Lsj.Util.Win32.NativeUI;
using Lsj.Util.Win32.NativeUI.DirectX;
using Lsj.Util.Win32.Structs;
using System.Runtime.InteropServices;
using static Lsj.Util.Win32.Constants;
using static Lsj.Util.Win32.DirectX.Enums.D3D11_CREATE_DEVICE_FLAG;
using static Lsj.Util.Win32.DirectX.Enums.D3D11_VIDEO_FRAME_FORMAT;
using static Lsj.Util.Win32.DirectX.Enums.D3D11_VIDEO_USAGE;
using static Lsj.Util.Win32.DirectX.Enums.D3D11_VPIV_DIMENSION;
using static Lsj.Util.Win32.DirectX.Enums.D3D11_VPOV_DIMENSION;
using static Lsj.Util.Win32.MediaFoundation.Constants;
using static Lsj.Util.Win32.MediaFoundation.GUIDs.MediaFoundationAttributes;
using static Lsj.Util.Win32.MediaFoundation.Mfapi;
using static Lsj.Util.Win32.MediaFoundation.Mfplat;
using static Lsj.Util.Win32.UnsafePInvokeExtensions;

namespace D3D11CaptureEngine
{
    public class PlayerWindow : D3D11Window
    {
        private readonly UIThreadManager _uiThreadManager;
        public PlayerWindow(UIThreadManager uiThreadManager)
        {
            _uiThreadManager = uiThreadManager;
        }

        public P<IMFDXGIDeviceManager> DXGIDeviceManager { get; private set; }

        public P<IMFCaptureEngine> CaptureEngine { get; private set; }

        public P<ID3D11VideoDevice> D3D11VideoDevice { get; private set; }

        public P<ID3D11VideoContext> D3D11VideoContext { get; private set; }

        public P<ID3D11VideoProcessorEnumerator> D3D11VideoProcessorEnumerator { get; private set; }

        public P<ID3D11VideoProcessor> D3D11VideoProcessor { get; private set; }

        public P<ID3D11Texture2D> CPURGBTexture { get; private set; }

        public uint VideoWidth { get; private set; }

        public uint VideoHeight { get; private set; }

        private bool _isResizing = false;

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

            var factory = COMExtensions.CreateInstance<IMFCaptureEngineClassFactory>(CLSIDs.CLSID_MFCaptureEngineClassFactory, IIDs.IID_IMFCaptureEngineClassFactory);
            factory.Value.CreateInstance(CLSIDs.CLSID_MFCaptureEngine, IIDs.IID_IMFCaptureEngine, out var engine);

            CaptureEngine = (nint)engine;

            MFCreateAttributes(out var captureEngineAttributes, 3);
            captureEngineAttributes.Value.SetUnknown(MF_CAPTURE_ENGINE_D3D_MANAGER, DXGIDeviceManager.AsIUnknown());
            captureEngineAttributes.Value.SetUINT32(MF_CAPTURE_ENGINE_USE_VIDEO_DEVICE_ONLY, 0);
            captureEngineAttributes.Value.SetUINT32(MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS, 1);

            var callback = new CaptureEngineOnEventCallback();
            callback.OnInitialized += CaptureEngineCallback_OnInitialized;
            var result = CaptureEngine.Value.Initialize(callback.GetNativePointer(), captureEngineAttributes, NULL, NULL);

            return true;
        }

        private void CaptureEngineCallback_OnInitialized(object? sender, EventArgs<HRESULT> e)
        {
            Console.WriteLine($"Capture Engine Initialized: {e.Value}");
            if (e.Value.Succeed)
            {
                _ = _uiThreadManager.InvokeAsync(OnCaptureEngineInitialized);
            }
        }

        private void OnCaptureEngineInitialized()
        {
            var result = CaptureEngine.Value.GetSink(MF_CAPTURE_ENGINE_SINK_TYPE.MF_CAPTURE_ENGINE_SINK_TYPE_PREVIEW, out var captureSink);

            result = CaptureEngine.Value.GetSource(out var source);

            result = source.Value.GetStreamIndexFromFriendlyName(MF_CAPTURE_ENGINE_FIRST_SOURCE_VIDEO_STREAM, out var streamIndex);
            Console.WriteLine($"MF_CAPTURE_ENGINE_FIRST_SOURCE_VIDEO_STREAM: {streamIndex}");

            //int i = 0;
            //while (true)
            //{
            //    result = source.Value.GetAvailableDeviceMediaType(streamIndex, i, out var tempMediaType);
            //    if (result == MF_E_NO_MORE_TYPES)
            //    {
            //        break;
            //    }
            //    PrintMediaType(tempMediaType);
            //    tempMediaType.AsIUnknown().Value.Release();
            //    i++;
            //}

            result = source.Value.GetCurrentDeviceMediaType(streamIndex, out var mediaType);
            MFGetAttributeSize(mediaType.AsP<IMFAttributes>(), MF_MT_FRAME_SIZE, out var width, out var height);
            VideoWidth = width;
            VideoHeight = height;
            PrintMediaType(mediaType);

            D3D11_TEXTURE2D_DESC cpuTextureDesc = new D3D11_TEXTURE2D_DESC
            {
                Width = VideoWidth,
                Height = VideoHeight,
                MipLevels = 1,
                ArraySize = 1,
                Format = DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM, // 对应 MFVideoFormat_RGB32
                Usage = D3D11_USAGE.D3D11_USAGE_DEFAULT,

            };
            cpuTextureDesc.SampleDesc.Count = 1;
            Device.Value.CreateTexture2D(cpuTextureDesc, null, out var texture);
            CPURGBTexture = texture;

            D3D11VideoDevice = Device.AsIUnknown().ToInterface<ID3D11VideoDevice>(IIDs.IID_ID3D11VideoDevice);
            D3D11VideoContext = DeviceContext.AsIUnknown().ToInterface<ID3D11VideoContext>(IIDs.IID_ID3D11VideoContext);

            User32.GetClientRect(Handle, out var rect);
            CreateVideoProcessor(rect.right - rect.left, rect.bottom - rect.top);

            captureSink.Value.AddStream(streamIndex, mediaType, NULL, out var sinkStreamIndex);
            mediaType.AsIUnknown().Value.Release();

            var previewSink = captureSink.AsIUnknown().ToInterface<IMFCapturePreviewSink>(IIDs.IID_IMFCapturePreviewSink, true, true);
            var callback = new CaptureEngineOnSampleCallback();
            callback.OnSampleCallback += Callback_OnSampleCallback;
            previewSink.Value.SetSampleCallback(sinkStreamIndex, callback.GetNativePointer());

            result = CaptureEngine.Value.StartPreview();

            previewSink.AsIUnknown().Value.Release();
            callback.Release();
        }

        private void Callback_OnSampleCallback(object? sender, P<IMFSample> sample)
        {
            if (_isResizing || sample == NULL)
            {
                return;
            }

            var result = sample.Value.ConvertToContiguousBuffer(out var buffer);
            if (result)
            {
                P<ID3D11Texture2D> texture = NULL;
                uint subresourceIndex = 0;
                if (buffer.AsIUnknown().ToInterfaceOrNull<IMFDXGIBuffer>(IIDs.IID_IMFDXGIBuffer, true, false) is P<IMFDXGIBuffer> dxgiBuffer)
                {
                    dxgiBuffer.Value.GetResource(IIDs.IID_ID3D11Texture2D, out var texturePtr);
                    dxgiBuffer.Value.GetSubresourceIndex(out var index);
                    subresourceIndex = index;
                    dxgiBuffer.AsIUnknown().Value.Release();

                    texture = (nint)texturePtr;
                }
                else
                {
                    var buffer2d2 = buffer.AsIUnknown().ToInterface<IMF2DBuffer2>(IIDs.IID_IMF2DBuffer2, true, true);
                    var buffer2d = buffer2d2.AsP<IMF2DBuffer>();

                    result = buffer2d2.Value.Lock2DSize(MF2DBuffer_LockFlags.MF2DBuffer_LockFlags_Read, out var scanline0, out var pitch, out var ppbBufferStart, out var length);
                    DeviceContext.Value.UpdateSubresource(CPURGBTexture.AsP<ID3D11Resource>(), 0, NullRef<D3D11_BOX>(), ppbBufferStart, (uint)pitch, 0);
                    result = buffer2d.Value.Unlock2D();
                    buffer2d2.AsIUnknown().Value.Release();

                    CPURGBTexture.AsIUnknown().Value.AddRef();
                    texture = (nint)CPURGBTexture;
                }
                //texture.Value.GetDesc(out var desc);

                var inputDesc = new D3D11_VIDEO_PROCESSOR_INPUT_VIEW_DESC
                {
                    ViewDimension = D3D11_VPIV_DIMENSION_TEXTURE2D,
                    Texture2D = new D3D11_TEX2D_VPIV
                    {
                        MipSlice = 0,
                        ArraySlice = subresourceIndex,
                    }
                };

                D3D11VideoDevice.Value.CreateVideoProcessorInputView(texture.AsP<ID3D11Resource>(), D3D11VideoProcessorEnumerator, inputDesc, out var inputView);

                SwapChain.Value.GetBuffer(0, IIDs.IID_ID3D11Texture2D, out var backBufferPtr);
                P<ID3D11Texture2D> backBuffer = backBufferPtr;
                backBuffer.Value.GetDesc(out var backDesc);
                //Console.WriteLine($"BackBuffer {backDesc.Width}x{backDesc.Height}");

                var outputDesc = new D3D11_VIDEO_PROCESSOR_OUTPUT_VIEW_DESC
                {
                    ViewDimension = D3D11_VPOV_DIMENSION_TEXTURE2D,
                    Texture2D = new D3D11_TEX2D_VPOV
                    {
                        MipSlice = 0,
                    }
                };
                D3D11VideoDevice.Value.CreateVideoProcessorOutputView(backBuffer.AsP<ID3D11Resource>(), D3D11VideoProcessorEnumerator, outputDesc, out var outputView);

                var stream = new D3D11_VIDEO_PROCESSOR_STREAM
                {
                    Enable = true,
                    pInputSurface = inputView,
                };


                D3D11VideoContext.Value.VideoProcessorBlt(D3D11VideoProcessor, outputView, 0, 1, [stream]);
                SwapChain.Value.Present(1, 0);

                texture.AsIUnknown().Value.Release();
                backBuffer.AsIUnknown().Value.Release();
                inputView.AsIUnknown().Value.Release();
                outputView.AsIUnknown().Value.Release();

            }
        }

        private void PrintMediaType(P<IMFMediaType> mediaType)
        {
            if (mediaType == NULL)
            {
                Console.WriteLine($"[PrintMediaType] NULL");
            }

            var attribute = mediaType.AsP<IMFAttributes>();
            MFGetAttributeSize(mediaType.AsP<IMFAttributes>(), MF_MT_FRAME_SIZE, out var width, out var height);

            attribute.Value.GetGUID(MF_MT_MAJOR_TYPE, out var majorType);
            attribute.Value.GetGUID(MF_MT_SUBTYPE, out var subType);

            Console.WriteLine($"[PrintMediaType] {GUIDToStringHelper.MajorMediaTypesToString(majorType)} {GUIDToStringHelper.MediaSubtypesToString(subType)} {width} {height}");
        }

        protected override P<ID3D11Device> CreateD3D11Device()
        {
#if DEBUG
            DeviceHelper.CreateDevice(out var device, out NullRef<P<ID3D11DeviceContext>>(), D3D11_CREATE_DEVICE_DEBUG);
            return device;
#endif
            return base.CreateD3D11Device();
        }

        protected override LRESULT OnSize(HWND hWnd, WPARAM wParam, LPARAM lParam, WindowSizingTypes sizingType, int width, int height)
        {
            if (sizingType != WindowSizingTypes.SIZE_MINIMIZED)
            {
                _isResizing = true;
                base.OnSize(hWnd, wParam, lParam, sizingType, width, height);
                if (D3D11VideoDevice != NULL)
                {
                    CreateVideoProcessor(width, height);
                }
                _isResizing = false;
            }
            return 0;
        }

        private void CreateVideoProcessor(int width, int height)
        {
            var desc = new D3D11_VIDEO_PROCESSOR_CONTENT_DESC
            {
                InputFrameFormat = D3D11_VIDEO_FRAME_FORMAT_PROGRESSIVE,
                InputWidth = VideoWidth,
                InputHeight = VideoHeight,
                OutputWidth = (uint)width,
                OutputHeight = (uint)height,
                Usage = D3D11_VIDEO_USAGE_PLAYBACK_NORMAL,
            };
            Console.WriteLine($"Input {VideoWidth}x{VideoHeight} Ouput {width}x{height}");
            D3D11VideoDevice.Value.CreateVideoProcessorEnumerator(desc, out var enumerator);
            if (D3D11VideoProcessorEnumerator != NULL)
            {
                D3D11VideoProcessorEnumerator.AsIUnknown().Value.Release();
            }
            D3D11VideoProcessorEnumerator = enumerator;

            D3D11VideoDevice.Value.CreateVideoProcessor(D3D11VideoProcessorEnumerator, 0, out var processor);
            if (D3D11VideoProcessor != NULL)
            {
                D3D11VideoProcessor.AsIUnknown().Value.Release();
            }
            D3D11VideoProcessor = processor;

            var sourceRect = new RECT
            {
                left = 0,
                top = 0,
                right = (int)VideoWidth,
                bottom = (int)VideoHeight,
            };
            D3D11VideoContext.Value.VideoProcessorSetStreamSourceRect(D3D11VideoProcessor, 0, true, sourceRect);

            var destRect = new RECT
            {
                left = 0,
                top = 0,
                right = width,
                bottom = height,
            };
            D3D11VideoContext.Value.VideoProcessorSetStreamDestRect(D3D11VideoProcessor, 0, true, destRect);
            D3D11VideoContext.Value.VideoProcessorSetOutputTargetRect(D3D11VideoProcessor, true, destRect);
        }

        class CaptureEngineOnEventCallback : BaseComObject<IMFCaptureEngineOnEventCallback>
        {
            public event EventHandler<EventArgs<HRESULT>>? OnInitialized;

            protected override int VTableItemCount => 4;

            protected override void InitVTable(VTableInitHelper<IMFCaptureEngineOnEventCallback> vTable)
            {
                vTable.SetMethod(3, GetMethodInfo(nameof(OnEvent)));
            }

            public override HRESULT QueryInterface(in IID riid, out nint ppvObject)
            {
                if (riid == IIDs.IID_IMFCaptureEngineOnEventCallback)
                {
                    AddRef();
                    ppvObject = GetNativePointer();
                    return HRESULT.S_OK;
                }
                return base.QueryInterface(riid, out ppvObject);
            }

            public HRESULT OnEvent([In] P<IMFMediaEvent> pEvent)
            {
                var result = pEvent.Value.GetType(out var eventType);
                if (eventType == MediaEventType.MEExtendedType)
                {
                    result = pEvent.Value.GetExtendedType(out var extendedType);
                    Console.WriteLine($"[CaptureEngineOnEventCallback] {eventType} {GUIDToStringHelper.MediaCaptureEventExtendedTypesToString(extendedType)}");

                    if (extendedType == MediaCaptureEventExtendedTypeGuids.MF_CAPTURE_ENGINE_INITIALIZED)
                    {
                        OnInitialized?.Invoke(this, EventArgs<HRESULT>.FromValue(result));
                    }
                }
                else
                {
                    Console.WriteLine($"[CaptureEngineOnEventCallback] {eventType}");
                }
                return HRESULT.S_OK;
            }
        }

        class CaptureEngineOnSampleCallback : BaseComObject<IMFCaptureEngineOnSampleCallback>
        {
            public event EventHandler<P<IMFSample>>? OnSampleCallback;

            protected override int VTableItemCount => 4;

            protected override void InitVTable(VTableInitHelper<IMFCaptureEngineOnSampleCallback> vTable)
            {
                vTable.SetMethod(3, GetMethodInfo(nameof(OnSample)));
            }

            public override HRESULT QueryInterface(in IID riid, out nint ppvObject)
            {
                if (riid == IIDs.IID_IMFCaptureEngineOnSampleCallback)
                {
                    AddRef();
                    ppvObject = GetNativePointer();
                    return HRESULT.S_OK;
                }
                return base.QueryInterface(riid, out ppvObject);
            }

            public HRESULT OnSample([In] P<IMFSample> pSample)
            {
                // pSample 不需要 Release

                //Console.WriteLine($"[CaptureEngineOnSampleCallback] OnSample");
                OnSampleCallback?.Invoke(this, pSample);
                return HRESULT.S_OK;
            }
        }
    }
}
