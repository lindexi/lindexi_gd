using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Direct3D;
using Windows.Win32.Graphics.Direct3D9;
using Windows.Win32.Graphics.Direct3D11;
using Windows.Win32.Graphics.Dxgi;
using Windows.Win32.Graphics.Dxgi.Common;
using Windows.Win32.System.Com;

namespace MediaSamples;

internal unsafe sealed partial class VideoDevice : IDisposable
{
    internal ID3D11Device* Device;
    internal ID3D11DeviceContext* Context;
    internal IDXGISwapChain* SwapChain;
    internal ID3D11Texture2D* Target;
    internal IDirect3D9Ex* Direct3D;
    internal IDirect3DDevice9Ex* Device9;
    internal IDirect3DTexture9* Texture9;
    internal IDirect3DSurface9* Surface9;
    private ID3D11VideoDevice* _videoDevice;
    private ID3D11VideoContext* _videoContext;
    private ID3D11VideoProcessorEnumerator* _enumerator;
    private ID3D11VideoProcessor* _processor;
    private uint _inputWidth, _inputHeight;
    internal int Width { get; private set; }
    internal int Height { get; private set; }
    private readonly HWND _window;
    private bool _shaderConversion;
    private bool _videoSupportChecked;

    internal VideoDevice(HWND window, bool d3d9, bool shared)
    {
        _window = window;
        RECT client;
        PInvoke.GetClientRect(window, &client);
        Width = Math.Max(1, client.right - client.left);
        Height = Math.Max(1, client.bottom - client.top);
        try
        {
            if (d3d9 || shared)
            {
                IDirect3D9Ex* api = null;
                PInvoke.Direct3DCreate9Ex(PInvoke.D3D_SDK_VERSION, &api).ThrowOnFailure();
                Direct3D = api;
                var present = new D3DPRESENT_PARAMETERS
                {
                    Windowed = true, hDeviceWindow = window,
                    BackBufferWidth = (uint)Width, BackBufferHeight = (uint)Height, BackBufferCount = 1,
                    BackBufferFormat = D3DFORMAT.D3DFMT_A8R8G8B8,
                    SwapEffect = D3DSWAPEFFECT.D3DSWAPEFFECT_DISCARD,
                    PresentationInterval = unchecked((uint)PInvoke.D3DPRESENT_INTERVAL_IMMEDIATE)
                };
                IDirect3DDevice9Ex* device9 = null;
                api->CreateDeviceEx(0, D3DDEVTYPE.D3DDEVTYPE_HAL, window,
                    PInvoke.D3DCREATE_HARDWARE_VERTEXPROCESSING | PInvoke.D3DCREATE_MULTITHREADED | PInvoke.D3DCREATE_FPU_PRESERVE,
                    &present, null, &device9).ThrowOnFailure();
                Device9 = device9;
                if (d3d9) return;
            }
            ID3D11Device* device = null;
            ID3D11DeviceContext* context = null;
            var flags = D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_BGRA_SUPPORT | D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_VIDEO_SUPPORT;
            if (shared)
            {
                PInvoke.D3D11CreateDevice(null, D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_HARDWARE, default, flags,
                    null, 0, PInvoke.D3D11_SDK_VERSION, &device, null, &context).ThrowOnFailure();
            }
            else
            {
                var desc = new DXGI_SWAP_CHAIN_DESC
                {
                    BufferDesc = new DXGI_MODE_DESC { Width = (uint)Width, Height = (uint)Height, Format = DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM },
                    SampleDesc = new DXGI_SAMPLE_DESC { Count = 1 }, BufferUsage = DXGI_USAGE.DXGI_USAGE_RENDER_TARGET_OUTPUT,
                    BufferCount = 2, OutputWindow = window, Windowed = true,
                    SwapEffect = DXGI_SWAP_EFFECT.DXGI_SWAP_EFFECT_DISCARD
                };
                IDXGISwapChain* swap = null;
                PInvoke.D3D11CreateDeviceAndSwapChain(null, D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_HARDWARE, default,
                    flags, null, 0, PInvoke.D3D11_SDK_VERSION, &desc, &swap, &device, null, &context).ThrowOnFailure();
                SwapChain = swap;
            }
            Device = device;
            Context = context;
            var multi = (ID3D11Multithread*)Query(context, typeof(ID3D11Multithread).GUID);
            multi->SetMultithreadProtected(true);
            Release(multi);
            if (shared)
            {
                CreateSharedSurface();
            }
            else
            {
                void* target = null;
                var iid = typeof(ID3D11Texture2D).GUID;
                SwapChain->GetBuffer(0, &iid, &target).ThrowOnFailure();
                Target = (ID3D11Texture2D*)target;
            }
            Console.WriteLine("D3D11 hardware device initialized; BGRA/video support enabled.");
        }
        catch { Dispose(); throw; }
    }

    private void CreateSharedSurface()
    {
        var desc = new D3D11_TEXTURE2D_DESC
        {
            Width = (uint)Width, Height = (uint)Height, MipLevels = 1, ArraySize = 1,
            Format = DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,
            SampleDesc = new DXGI_SAMPLE_DESC { Count = 1 },
            BindFlags = D3D11_BIND_FLAG.D3D11_BIND_RENDER_TARGET | D3D11_BIND_FLAG.D3D11_BIND_SHADER_RESOURCE,
            MiscFlags = D3D11_RESOURCE_MISC_FLAG.D3D11_RESOURCE_MISC_SHARED
        };
        ID3D11Texture2D* target = null;
        Device->CreateTexture2D(&desc, (D3D11_SUBRESOURCE_DATA*)null, &target).ThrowOnFailure();
        Target = target;
        var resource = (IDXGIResource*)Query(target, typeof(IDXGIResource).GUID);
        HANDLE handle = default;
        try { resource->GetSharedHandle(&handle).ThrowOnFailure(); }
        finally { Release(resource); }
        IDirect3DTexture9* texture = null;
        Device9->CreateTexture((uint)Width, (uint)Height, 1, 1, D3DFORMAT.D3DFMT_A8R8G8B8,
            D3DPOOL.D3DPOOL_DEFAULT, &texture, &handle).ThrowOnFailure();
        Texture9 = texture;
        IDirect3DSurface9* surface = null;
        texture->GetSurfaceLevel(0, &surface).ThrowOnFailure();
        Surface9 = surface;
    }

    internal bool SharedOutputSizeChanged()
    {
        RECT client;
        if (!PInvoke.GetClientRect(_window, &client)) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        return client.right > client.left && client.bottom > client.top
            && (client.right - client.left != Width || client.bottom - client.top != Height);
    }

    internal void ResizeSharedOutput()
    {
        RECT client;
        if (!PInvoke.GetClientRect(_window, &client)) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        Context->ClearState();
        Context->Flush();
        Release(Surface9); Surface9 = null;
        Release(Texture9); Texture9 = null;
        Release(Target); Target = null;
        Release(_processor); _processor = null;
        Release(_enumerator); _enumerator = null;
        Width = client.right - client.left;
        Height = client.bottom - client.top;
        CreateSharedSurface();
        Console.WriteLine($"Shared output rebuilt: {Width}x{Height}.");
    }

    internal static void* Query(void* value, Guid iid)
    {
        void* result = null;
        var status = ((IUnknown*)value)->QueryInterface(&iid, &result);
        if (status.Failed) throw new COMException($"QueryInterface({iid}) failed: 0x{status.Value:X8}.", status.Value);
        return result;
    }

    internal static void Release(void* value)
    {
        if (value != null) ((IUnknown*)value)->Release();
    }

    internal void Process(ID3D11Texture2D* texture, uint slice)
    {
        ResizeOutput();
        if (!_videoSupportChecked)
        {
            var iid = typeof(ID3D11VideoDevice).GUID;
            void* value = null;
            var status = ((IUnknown*)Device)->QueryInterface(&iid, &value);
            if (status.Value == unchecked((int)0x80004002)) _shaderConversion = true;
            else status.ThrowOnFailure();
            _videoDevice = (ID3D11VideoDevice*)value;
            _videoSupportChecked = true;
            Console.WriteLine(_shaderConversion ? "Video processor unavailable; using GPU NV12 shader conversion." : "D3D11 video processor available.");
        }
        if (_shaderConversion) { ProcessWithShader(texture, slice); return; }
        D3D11_TEXTURE2D_DESC input = default;
        texture->GetDesc(&input);
        if (_processor == null || input.Width != _inputWidth || input.Height != _inputHeight)
        {
            Release(_processor); _processor = null;
            Release(_enumerator); _enumerator = null;
            if (_videoDevice == null) _videoDevice = (ID3D11VideoDevice*)Query(Device, typeof(ID3D11VideoDevice).GUID);
            if (_videoContext == null) _videoContext = (ID3D11VideoContext*)Query(Context, typeof(ID3D11VideoContext).GUID);
            var desc = new D3D11_VIDEO_PROCESSOR_CONTENT_DESC
            {
                InputFrameFormat = D3D11_VIDEO_FRAME_FORMAT.D3D11_VIDEO_FRAME_FORMAT_PROGRESSIVE,
                InputWidth = input.Width, InputHeight = input.Height,
                OutputWidth = (uint)Width, OutputHeight = (uint)Height,
                InputFrameRate = new DXGI_RATIONAL { Numerator = 30, Denominator = 1 },
                OutputFrameRate = new DXGI_RATIONAL { Numerator = 30, Denominator = 1 },
                Usage = D3D11_VIDEO_USAGE.D3D11_VIDEO_USAGE_PLAYBACK_NORMAL
            };
            ID3D11VideoProcessorEnumerator* enumerator = null;
            _videoDevice->CreateVideoProcessorEnumerator(&desc, &enumerator).ThrowOnFailure();
            _enumerator = enumerator;
            ID3D11VideoProcessor* processor = null;
            _videoDevice->CreateVideoProcessor(enumerator, 0, &processor).ThrowOnFailure();
            _processor = processor;
            _inputWidth = input.Width; _inputHeight = input.Height;
            _videoContext->VideoProcessorSetStreamAutoProcessingMode(processor, 0, false);
        }
        var sourceRect = new RECT(0, 0, (int)(VideoWidth == 0 ? input.Width : VideoWidth), (int)(VideoHeight == 0 ? input.Height : VideoHeight));
        var targetRect = new RECT(0, 0, Width, Height);
        _videoContext->VideoProcessorSetStreamSourceRect(_processor, 0, true, &sourceRect);
        _videoContext->VideoProcessorSetStreamDestRect(_processor, 0, true, &targetRect);
        _videoContext->VideoProcessorSetOutputTargetRect(_processor, true, &targetRect);
        var inDesc = new D3D11_VIDEO_PROCESSOR_INPUT_VIEW_DESC { ViewDimension = D3D11_VPIV_DIMENSION.D3D11_VPIV_DIMENSION_TEXTURE2D };
        inDesc.Anonymous.Texture2D.ArraySlice = slice;
        var outDesc = new D3D11_VIDEO_PROCESSOR_OUTPUT_VIEW_DESC { ViewDimension = D3D11_VPOV_DIMENSION.D3D11_VPOV_DIMENSION_TEXTURE2D };
        ID3D11VideoProcessorInputView* inputView = null;
        ID3D11VideoProcessorOutputView* outputView = null;
        try
        {
            _videoDevice->CreateVideoProcessorInputView((ID3D11Resource*)texture, _enumerator, &inDesc, &inputView).ThrowOnFailure();
            _videoDevice->CreateVideoProcessorOutputView((ID3D11Resource*)Target, _enumerator, &outDesc, &outputView).ThrowOnFailure();
            var stream = new D3D11_VIDEO_PROCESSOR_STREAM { Enable = true, pInputSurface = inputView };
            _videoContext->VideoProcessorBlt(_processor, outputView, 0, 1, &stream).ThrowOnFailure();
        }
        finally { Release(outputView); Release(inputView); }
    }

    internal void Present()
    {
        Context->Flush();
        if (SwapChain != null) SwapChain->Present(1, 0).ThrowOnFailure();
    }

    public void Dispose()
    {
        DisposeShaders();
        Release(_processor); _processor = null;
        Release(_enumerator); _enumerator = null;
        Release(_videoContext); _videoContext = null;
        Release(_videoDevice); _videoDevice = null;
        Release(Surface9); Surface9 = null;
        Release(Texture9); Texture9 = null;
        Release(Target); Target = null;
        Release(SwapChain); SwapChain = null;
        Release(Context); Context = null;
        Release(Device); Device = null;
        Release(Device9); Device9 = null;
        Release(Direct3D); Direct3D = null;
    }
}
