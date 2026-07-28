using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;
using Silk.NET.Direct3D9;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using D3D9Format = Silk.NET.Direct3D9.Format;
using D3D11Map = Silk.NET.Direct3D11.Map;

namespace Pano.Net.DirectX11;

internal sealed unsafe class DirectX11D3DImageRenderer : IDisposable
{
    private const uint D3DSdkVersion = 32;
    private const uint D3DPresentIntervalDefault = 0;
    private const uint D3DCreateFpuPreserve = 0x00000002;
    private const uint D3DCreateMultithreaded = 0x00000004;
    private const uint D3DCreateHardwareVertexProcessing = 0x00000040;
    private const uint D3DUsageRenderTarget = 0x00000001;
    private const uint D3D11ResourceMiscShared = 0x00000002;
    private const uint MonitorDefaultToNearest = 0x00000002;
    private const int CameraBufferSize = 16;
    private const double DegreesToRadians = Math.PI / 180;

    private readonly Image _imageControl;
    private readonly D3DImage _d3dImage = new();
    private readonly D3D11 _d3d11 = D3D11.GetApi();
    private readonly D3D9 _d3d9 = D3D9.GetApi();
    private readonly DXGI _dxgi = DXGI.GetApi();
    private readonly D3DCompiler _compiler = D3DCompiler.GetApi();

    private IDXGIFactory1* _dxgiFactory;
    private IDXGIAdapter1* _dxgiAdapter;
    private ID3D11Device* _device;
    private ID3D11DeviceContext* _context;
    private IDirect3D9Ex* _d3d9Instance;
    private IDirect3DDevice9Ex* _d3d9Device;
    private IDirect3DTexture9* _sharedTexture9;
    private IDirect3DSurface9* _sharedSurface9;
    private ID3D11Texture2D* _sharedTexture11;
    private ID3D11RenderTargetView* _renderTargetView;
    private ID3D11Texture2D* _panoramaTexture;
    private ID3D11ShaderResourceView* _panoramaView;
    private ID3D11VertexShader* _vertexShader;
    private ID3D11PixelShader* _pixelShader;
    private ID3D11SamplerState* _sampler;
    private ID3D11Buffer* _cameraBuffer;
    private int _width;
    private int _height;
    private uint _d3d9AdapterIndex;
    private bool _initialized;

    public DirectX11D3DImageRenderer(Image imageControl)
    {
        ArgumentNullException.ThrowIfNull(imageControl);

        _imageControl = imageControl;
        _imageControl.Source = _d3dImage;
    }

    public string Status { get; private set; } = "DX11 尚未初始化";

    public string Initialize()
    {
        if (_initialized)
        {
            return Status;
        }

        CreateD3D9Device();
        CreateMatchingD3D11Device(out D3DFeatureLevel featureLevel);
        CreateShaders();
        CreatePipelineResources();

        _initialized = true;
        Resize(
            Math.Max(1, (int)Math.Ceiling(_imageControl.ActualWidth)),
            Math.Max(1, (int)Math.Ceiling(_imageControl.ActualHeight)));

        Status = $"DX11 {featureLevel} / D3D9Ex 共享表面已就绪";
        return Status;
    }

    private void CreateD3D9Device()
    {
        IDirect3D9Ex* instance = null;
        ThrowIfFailed(_d3d9.Direct3DCreate9Ex(D3DSdkVersion, &instance), "创建 Direct3D 9Ex");
        _d3d9Instance = instance;

        Window window = Window.GetWindow(_imageControl)
            ?? throw new InvalidOperationException("无法获取 WPF 主窗口句柄。");
        IntPtr windowHandle = new WindowInteropHelper(window).Handle;
        _d3d9AdapterIndex = FindD3D9AdapterForWindow(windowHandle);

        Silk.NET.Direct3D9.PresentParameters presentParameters = new()
        {
            Windowed = 1,
            SwapEffect = Swapeffect.Discard,
            HDeviceWindow = windowHandle,
            PresentationInterval = D3DPresentIntervalDefault,
            BackBufferFormat = D3D9Format.Unknown,
            BackBufferWidth = 1,
            BackBufferHeight = 1
        };

        uint behaviorFlags = D3DCreateHardwareVertexProcessing
            | D3DCreateMultithreaded
            | D3DCreateFpuPreserve;
        IDirect3DDevice9Ex* createdDevice = null;
        ThrowIfFailed(
            _d3d9Instance->CreateDeviceEx(
                _d3d9AdapterIndex,
                Devtype.Hal,
                windowHandle,
                behaviorFlags,
                &presentParameters,
                null,
                &createdDevice),
            $"在适配器 {_d3d9AdapterIndex} 上创建 Direct3D 9Ex 设备");
        _d3d9Device = createdDevice;
    }

    private uint FindD3D9AdapterForWindow(IntPtr windowHandle)
    {
        IntPtr windowMonitor = MonitorFromWindow(windowHandle, MonitorDefaultToNearest);
        if (windowMonitor == IntPtr.Zero)
        {
            throw new InvalidOperationException("无法确定 WPF 窗口所在的显示器。");
        }

        uint adapterCount = _d3d9Instance->GetAdapterCount();
        for (uint adapterIndex = 0; adapterIndex < adapterCount; adapterIndex++)
        {
            if (_d3d9Instance->GetAdapterMonitor(adapterIndex) == windowMonitor)
            {
                return adapterIndex;
            }
        }

        throw new InvalidOperationException("没有找到与 WPF 窗口所在显示器匹配的 D3D9Ex 适配器。");
    }

    private void CreateMatchingD3D11Device(out D3DFeatureLevel featureLevel)
    {
        Luid d3d9AdapterLuid;
        ThrowIfFailed(
            _d3d9Instance->GetAdapterLUID(_d3d9AdapterIndex, &d3d9AdapterLuid),
            $"读取 D3D9Ex 适配器 {_d3d9AdapterIndex} 的 LUID");

        Guid factoryGuid = IDXGIFactory1.Guid;
        void* factoryPointer = null;
        ThrowIfFailed(_dxgi.CreateDXGIFactory1(&factoryGuid, &factoryPointer), "创建 DXGI 工厂");
        _dxgiFactory = (IDXGIFactory1*)factoryPointer;

        _dxgiAdapter = FindMatchingDxgiAdapter(d3d9AdapterLuid);
        if (_dxgiAdapter == null)
        {
            throw new InvalidOperationException("没有找到与 WPF/D3D9Ex 桌面适配器匹配的 DXGI 适配器。");
        }

        ID3D11Device* createdDevice = null;
        ID3D11DeviceContext* createdContext = null;
        D3DFeatureLevel chosenFeatureLevel;
        ThrowIfFailed(
            _d3d11.CreateDevice(
                (IDXGIAdapter*)_dxgiAdapter,
                D3DDriverType.Unknown,
                0,
                (uint)CreateDeviceFlag.BgraSupport,
                null,
                0,
                D3D11.SdkVersion,
                &createdDevice,
                &chosenFeatureLevel,
                &createdContext),
            "在 WPF 桌面适配器上创建 Direct3D 11 设备");

        _device = createdDevice;
        _context = createdContext;
        featureLevel = chosenFeatureLevel;
    }

    private IDXGIAdapter1* FindMatchingDxgiAdapter(Luid d3d9AdapterLuid)
    {
        for (uint adapterIndex = 0; ; adapterIndex++)
        {
            IDXGIAdapter1* adapter = null;
            int result = _dxgiFactory->EnumAdapters1(adapterIndex, &adapter);
            if (result == unchecked((int)0x887A0002))
            {
                return null;
            }

            ThrowIfFailed(result, "枚举 DXGI 适配器");

            AdapterDesc1 description;
            ThrowIfFailed(adapter->GetDesc1(&description), "读取 DXGI 适配器信息");
            if (*(long*)&description.AdapterLuid == *(long*)&d3d9AdapterLuid)
            {
                return adapter;
            }

            adapter->Release();
        }
    }

    private void CreateShaders()
    {
        const string shaderSource = @"cbuffer Camera : register(b0)
{
    float Yaw;
    float Pitch;
    float HorizontalFov;
    float Aspect;
};
Texture2D Panorama : register(t0);
SamplerState PanoramaSampler : register(s0);
struct VertexOutput { float4 Position : SV_Position; float2 Screen : TEXCOORD0; };
VertexOutput VSMain(uint id : SV_VertexID)
{
    VertexOutput output;
    float2 position = float2((id << 1) & 2, id & 2);
    output.Screen = position * float2(2, -2) + float2(-1, 1);
    output.Position = float4(output.Screen, 0, 1);
    return output;
}
float4 PSMain(VertexOutput input) : SV_Target
{
    float verticalFov = 2 * atan(tan(HorizontalFov * 0.5) / Aspect);
    float3 forward = float3(sin(Pitch) * sin(Yaw), sin(Pitch) * cos(Yaw), cos(Pitch));
    float3 right = normalize(float3(cos(Yaw), -sin(Yaw), 0));
    float3 up = normalize(cross(right, forward));
    float3 direction = normalize(forward + right * input.Screen.x * tan(HorizontalFov * 0.5)
        + up * input.Screen.y * tan(verticalFov * 0.5));
    float2 uv = float2(atan2(direction.x, direction.y) / (2 * 3.14159265359) + 0.5,
                       acos(clamp(direction.z, -1, 1)) / 3.14159265359);
    return Panorama.Sample(PanoramaSampler, uv);
}";

        ID3D10Blob* vertexShaderBlob = CompileShader(shaderSource, "VSMain", "vs_5_0");
        ID3D10Blob* pixelShaderBlob = CompileShader(shaderSource, "PSMain", "ps_5_0");
        try
        {
            ID3D11VertexShader* createdVertexShader = null;
            ID3D11PixelShader* createdPixelShader = null;
            ThrowIfFailed(
                _device->CreateVertexShader(
                    vertexShaderBlob->GetBufferPointer(),
                    vertexShaderBlob->GetBufferSize(),
                    null,
                    &createdVertexShader),
                "创建顶点着色器");
            ThrowIfFailed(
                _device->CreatePixelShader(
                    pixelShaderBlob->GetBufferPointer(),
                    pixelShaderBlob->GetBufferSize(),
                    null,
                    &createdPixelShader),
                "创建像素着色器");
            _vertexShader = createdVertexShader;
            _pixelShader = createdPixelShader;
        }
        finally
        {
            vertexShaderBlob->Release();
            pixelShaderBlob->Release();
        }
    }

    private ID3D10Blob* CompileShader(string source, string entryPoint, string target)
    {
        byte[] sourceBytes = Encoding.UTF8.GetBytes(source);
        byte[] entryPointBytes = Encoding.ASCII.GetBytes(entryPoint + "\0");
        byte[] targetBytes = Encoding.ASCII.GetBytes(target + "\0");
        ID3D10Blob* shaderBlob = null;
        ID3D10Blob* errorBlob = null;

        fixed (byte* sourcePointer = sourceBytes)
        fixed (byte* entryPointPointer = entryPointBytes)
        fixed (byte* targetPointer = targetBytes)
        {
            int result = _compiler.Compile(
                sourcePointer,
                (nuint)sourceBytes.Length,
                (byte*)null,
                null,
                null,
                entryPointPointer,
                targetPointer,
                0,
                0,
                &shaderBlob,
                &errorBlob);
            if (result < 0)
            {
                string message = errorBlob == null
                    ? $"HRESULT 0x{result:X8}"
                    : Marshal.PtrToStringAnsi(
                        (IntPtr)errorBlob->GetBufferPointer(),
                        (int)errorBlob->GetBufferSize()) ?? "未知着色器错误";
                Release(ref errorBlob);
                throw new InvalidOperationException($"编译 {entryPoint} 失败：{message}");
            }
        }

        Release(ref errorBlob);
        return shaderBlob;
    }

    private void CreatePipelineResources()
    {
        SamplerDesc samplerDescription = new()
        {
            Filter = Filter.MinMagMipLinear,
            AddressU = TextureAddressMode.Wrap,
            AddressV = TextureAddressMode.Clamp,
            AddressW = TextureAddressMode.Clamp,
            MaxLOD = float.MaxValue
        };
        ID3D11SamplerState* createdSampler = null;
        ThrowIfFailed(_device->CreateSamplerState(&samplerDescription, &createdSampler), "创建采样器");
        _sampler = createdSampler;

        BufferDesc bufferDescription = new()
        {
            ByteWidth = CameraBufferSize,
            Usage = Usage.Dynamic,
            BindFlags = (uint)BindFlag.ConstantBuffer,
            CPUAccessFlags = (uint)CpuAccessFlag.Write
        };
        ID3D11Buffer* createdBuffer = null;
        ThrowIfFailed(_device->CreateBuffer(&bufferDescription, null, &createdBuffer), "创建相机常量缓冲区");
        _cameraBuffer = createdBuffer;
    }

    public void Resize(int width, int height)
    {
        if (!_initialized || width <= 0 || height <= 0 || (width == _width && height == _height))
        {
            return;
        }

        _width = width;
        _height = height;
        ReleaseRenderTarget();
        CreateRenderTarget();
    }

    private void CreateRenderTarget()
    {
        Texture2DDesc textureDescription = new()
        {
            Width = (uint)_width,
            Height = (uint)_height,
            MipLevels = 1,
            ArraySize = 1,
            Format = Silk.NET.DXGI.Format.FormatB8G8R8A8Unorm,
            SampleDesc = new SampleDesc(1, 0),
            Usage = Usage.Default,
            BindFlags = (uint)(BindFlag.RenderTarget | BindFlag.ShaderResource),
            MiscFlags = D3D11ResourceMiscShared
        };
        ID3D11Texture2D* createdTexture = null;
        ThrowIfFailed(_device->CreateTexture2D(&textureDescription, null, &createdTexture), "创建 D3D11 共享纹理");
        _sharedTexture11 = createdTexture;

        void* sharedHandle = GetSharedHandle(_sharedTexture11);
        OpenSharedTextureInD3D9(sharedHandle);

        ID3D11RenderTargetView* createdRenderTargetView = null;
        ThrowIfFailed(
            _device->CreateRenderTargetView(
                (ID3D11Resource*)_sharedTexture11,
                null,
                &createdRenderTargetView),
            "创建共享渲染目标视图");
        _renderTargetView = createdRenderTargetView;

        BindD3DImageBackBuffer();
    }

    private static void* GetSharedHandle(ID3D11Texture2D* texture)
    {
        Guid resourceGuid = IDXGIResource.Guid;
        void* resourcePointer = null;
        ThrowIfFailed(texture->QueryInterface(&resourceGuid, &resourcePointer), "获取共享纹理的 DXGI 资源");

        try
        {
            void* sharedHandle = null;
            ThrowIfFailed(((IDXGIResource*)resourcePointer)->GetSharedHandle(&sharedHandle), "获取 D3D11 共享纹理句柄");
            return sharedHandle;
        }
        finally
        {
            ((IDXGIResource*)resourcePointer)->Release();
        }
    }

    private void OpenSharedTextureInD3D9(void* sharedHandle)
    {
        IDirect3DTexture9* createdTexture = null;
        ThrowIfFailed(
            _d3d9Device->CreateTexture(
                (uint)_width,
                (uint)_height,
                1,
                D3DUsageRenderTarget,
                D3D9Format.A8R8G8B8,
                Pool.Default,
                &createdTexture,
                &sharedHandle),
            "在 D3D9Ex 中打开共享纹理");
        _sharedTexture9 = createdTexture;

        IDirect3DSurface9* createdSurface = null;
        ThrowIfFailed(_sharedTexture9->GetSurfaceLevel(0, &createdSurface), "获取 D3D9Ex 共享表面");
        _sharedSurface9 = createdSurface;
    }

    private void BindD3DImageBackBuffer()
    {
        _d3dImage.Lock();
        try
        {
            try
            {
                _d3dImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, (IntPtr)_sharedSurface9);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException(
                    $"D3DImage 接收适配器 {_d3d9AdapterIndex} 的 D3D9Ex 表面失败，HRESULT 0x{exception.HResult:X8}。",
                    exception);
            }
        }
        finally
        {
            _d3dImage.Unlock();
        }
    }

    public void LoadPanorama(string path)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("渲染器尚未初始化。");
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("全景图路径不能为空。", nameof(path));
        }

        ReleasePanorama();

        BitmapImage source = new();
        source.BeginInit();
        source.CacheOption = BitmapCacheOption.OnLoad;
        source.UriSource = new Uri(path, UriKind.Absolute);
        source.EndInit();
        source.Freeze();

        FormatConvertedBitmap bitmap = new(source, PixelFormats.Bgra32, null, 0);
        int stride = bitmap.PixelWidth * 4;
        byte[] pixels = new byte[stride * bitmap.PixelHeight];
        bitmap.CopyPixels(pixels, stride, 0);

        fixed (byte* pixelPointer = pixels)
        {
            Texture2DDesc textureDescription = new()
            {
                Width = (uint)bitmap.PixelWidth,
                Height = (uint)bitmap.PixelHeight,
                MipLevels = 1,
                ArraySize = 1,
                Format = Silk.NET.DXGI.Format.FormatB8G8R8A8Unorm,
                SampleDesc = new SampleDesc(1, 0),
                Usage = Usage.Immutable,
                BindFlags = (uint)BindFlag.ShaderResource
            };
            SubresourceData initialData = new()
            {
                PSysMem = pixelPointer,
                SysMemPitch = (uint)stride
            };
            ID3D11Texture2D* createdTexture = null;
            ThrowIfFailed(_device->CreateTexture2D(&textureDescription, &initialData, &createdTexture), "上传全景纹理");
            _panoramaTexture = createdTexture;
        }

        ID3D11ShaderResourceView* createdView = null;
        ThrowIfFailed(
            _device->CreateShaderResourceView((ID3D11Resource*)_panoramaTexture, null, &createdView),
            "创建全景纹理视图");
        _panoramaView = createdView;
        Status = $"已加载 {bitmap.PixelWidth}×{bitmap.PixelHeight} 全景图";
    }

    public void Render(double yaw, double pitch, double fieldOfView)
    {
        if (!_initialized || _panoramaView == null || _renderTargetView == null || _width <= 0 || _height <= 0)
        {
            return;
        }

        UpdateCameraBuffer(yaw, pitch, fieldOfView);

        _d3dImage.Lock();
        try
        {
            DrawPanorama();
            _d3dImage.AddDirtyRect(new Int32Rect(0, 0, _width, _height));
        }
        finally
        {
            _d3dImage.Unlock();
        }

        Status = $"DX11 / D3DImage GPU 渲染正常；RT {_width}×{_height}；D3DImage {_d3dImage.PixelWidth}×{_d3dImage.PixelHeight}";
    }

    private void UpdateCameraBuffer(double yaw, double pitch, double fieldOfView)
    {
        MappedSubresource mappedResource;
        ThrowIfFailed(
            _context->Map((ID3D11Resource*)_cameraBuffer, 0, D3D11Map.WriteDiscard, 0, &mappedResource),
            "更新相机常量");

        float* values = (float*)mappedResource.PData;
        values[0] = (float)(yaw * DegreesToRadians);
        values[1] = (float)(pitch * DegreesToRadians);
        values[2] = (float)(fieldOfView * DegreesToRadians);
        values[3] = (float)_width / _height;
        _context->Unmap((ID3D11Resource*)_cameraBuffer, 0);
    }

    private void DrawPanorama()
    {
        Viewport viewport = new(0, 0, _width, _height, 0, 1);
        _context->RSSetViewports(1, &viewport);

        ID3D11RenderTargetView* renderTarget = _renderTargetView;
        _context->OMSetRenderTargets(1, &renderTarget, null);
        _context->IASetPrimitiveTopology(D3DPrimitiveTopology.D3D11PrimitiveTopologyTrianglelist);
        _context->VSSetShader(_vertexShader, null, 0);
        _context->PSSetShader(_pixelShader, null, 0);

        ID3D11Buffer* cameraBuffer = _cameraBuffer;
        _context->PSSetConstantBuffers(0, 1, &cameraBuffer);
        ID3D11ShaderResourceView* panoramaView = _panoramaView;
        _context->PSSetShaderResources(0, 1, &panoramaView);
        ID3D11SamplerState* sampler = _sampler;
        _context->PSSetSamplers(0, 1, &sampler);

        _context->Draw(3, 0);
        _context->Flush();
    }

    private void ReleasePanorama()
    {
        Release(ref _panoramaView);
        Release(ref _panoramaTexture);
    }

    private void ReleaseRenderTarget()
    {
        if (_context != null)
        {
            _context->OMSetRenderTargets(0, null, null);
            _context->Flush();
        }

        if (_sharedSurface9 != null)
        {
            _d3dImage.Lock();
            try
            {
                _d3dImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero);
            }
            finally
            {
                _d3dImage.Unlock();
            }
        }

        Release(ref _renderTargetView);
        Release(ref _sharedTexture11);
        Release(ref _sharedSurface9);
        Release(ref _sharedTexture9);
    }

    private static void ThrowIfFailed(int result, string operation)
    {
        if (result < 0)
        {
            throw new InvalidOperationException($"{operation}失败，HRESULT 0x{result:X8}。");
        }
    }

    private static void Release<T>(ref T* value)
        where T : unmanaged, IComVtbl<T>
    {
        if (value == null)
        {
            return;
        }

        ((IUnknown*)value)->Release();
        value = null;
    }

    public void Dispose()
    {
        ReleasePanorama();
        ReleaseRenderTarget();
        Release(ref _cameraBuffer);
        Release(ref _sampler);
        Release(ref _pixelShader);
        Release(ref _vertexShader);
        Release(ref _context);
        Release(ref _device);
        Release(ref _dxgiAdapter);
        Release(ref _dxgiFactory);
        Release(ref _d3d9Device);
        Release(ref _d3d9Instance);
        _compiler.Dispose();
        _dxgi.Dispose();
        _d3d11.Dispose();
        _d3d9.Dispose();
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr windowHandle, uint flags);
}
