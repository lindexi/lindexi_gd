using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Direct3D;
using Windows.Win32.Graphics.Direct3D9;
using Windows.Win32.Graphics.Direct3D11;
using Windows.Win32.Graphics.Dxgi;
using Windows.Win32.Graphics.Dxgi.Common;
using Windows.Win32.System.Com;

namespace MediaFoundation.Controls;

internal sealed unsafe class SharedVideoSurface : IDisposable
{
    private IDirect3D9Ex* api;
    private IDirect3DDevice9Ex* device9;
    private IDirect3DTexture9* texture9;
    private IDirect3DSurface9* surface;
    private ID3D11Device* device;
    private ID3D11DeviceContext* context;
    private ID3D11Texture2D* texture;
    internal nint Device => (nint)device;
    internal nint Texture => (nint)texture;
    internal nint Surface => (nint)surface;
    internal int Width { get; private set; }
    internal int Height { get; private set; }

    internal SharedVideoSurface()
    {
        IDXGIFactory1* factory = null;
        IDXGIAdapter1* adapter = null;
        try
        {
            IDirect3D9Ex* created9 = null;
            PInvoke.Direct3DCreate9Ex(PInvoke.D3D_SDK_VERSION, &created9).ThrowOnFailure(); api = created9;
            LUID luid;
            api->GetAdapterLUID(0, &luid).ThrowOnFailure();
            var iid = typeof(IDXGIFactory1).GUID;
            void* value = null;
            PInvoke.CreateDXGIFactory1(&iid, &value).ThrowOnFailure(); factory = (IDXGIFactory1*)value;
            for (uint index = 0; ; index++)
            {
                IDXGIAdapter1* candidate = null;
                factory->EnumAdapters1(index, &candidate).ThrowOnFailure();
                DXGI_ADAPTER_DESC1 description;
                try { candidate->GetDesc1(&description).ThrowOnFailure(); }
                catch { candidate->Release(); throw; }
                if (description.AdapterLuid.LowPart == luid.LowPart && description.AdapterLuid.HighPart == luid.HighPart) { adapter = candidate; break; }
                candidate->Release();
            }
            var window = PInvoke.GetDesktopWindow();
            var present = new D3DPRESENT_PARAMETERS
            {
                Windowed = true, hDeviceWindow = window, BackBufferWidth = 1, BackBufferHeight = 1, BackBufferCount = 1,
                BackBufferFormat = D3DFORMAT.D3DFMT_A8R8G8B8, SwapEffect = D3DSWAPEFFECT.D3DSWAPEFFECT_DISCARD,
                PresentationInterval = unchecked((uint)PInvoke.D3DPRESENT_INTERVAL_IMMEDIATE)
            };
            IDirect3DDevice9Ex* createdDevice9 = null;
            api->CreateDeviceEx(0, D3DDEVTYPE.D3DDEVTYPE_HAL, window,
                PInvoke.D3DCREATE_HARDWARE_VERTEXPROCESSING | PInvoke.D3DCREATE_MULTITHREADED | PInvoke.D3DCREATE_FPU_PRESERVE,
                &present, null, &createdDevice9).ThrowOnFailure(); device9 = createdDevice9;
            ID3D11Device* created = null;
            ID3D11DeviceContext* immediate = null;
            PInvoke.D3D11CreateDevice((IDXGIAdapter*)adapter, D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_UNKNOWN, default,
                D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_BGRA_SUPPORT | D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_VIDEO_SUPPORT,
                null, 0, PInvoke.D3D11_SDK_VERSION, &created, null, &immediate).ThrowOnFailure();
            device = created; context = immediate;
            iid = typeof(ID3D11Multithread).GUID;
            value = null;
            context->QueryInterface(&iid, &value).ThrowOnFailure();
            try { ((ID3D11Multithread*)value)->SetMultithreadProtected(true); }
            finally { Release(value); }
        }
        catch { Dispose(); throw; }
        finally { Release(adapter); Release(factory); }
    }

    internal void Resize(int width, int height)
    {
        ReleaseSurface(); Width = width; Height = height;
        try
        {
            var description = new D3D11_TEXTURE2D_DESC
            {
                Width = (uint)width, Height = (uint)height, MipLevels = 1, ArraySize = 1,
                Format = DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM, SampleDesc = new DXGI_SAMPLE_DESC { Count = 1 },
                BindFlags = D3D11_BIND_FLAG.D3D11_BIND_RENDER_TARGET | D3D11_BIND_FLAG.D3D11_BIND_SHADER_RESOURCE,
                MiscFlags = D3D11_RESOURCE_MISC_FLAG.D3D11_RESOURCE_MISC_SHARED
            };
            ID3D11Texture2D* created = null;
            device->CreateTexture2D(&description, null, &created).ThrowOnFailure(); texture = created;
            var iid = typeof(IDXGIResource).GUID;
            void* value = null;
            texture->QueryInterface(&iid, &value).ThrowOnFailure();
            HANDLE handle;
            try { ((IDXGIResource*)value)->GetSharedHandle(&handle).ThrowOnFailure(); }
            finally { Release(value); }
            IDirect3DTexture9* shared = null;
            device9->CreateTexture((uint)width, (uint)height, 1, 1, D3DFORMAT.D3DFMT_A8R8G8B8, D3DPOOL.D3DPOOL_DEFAULT, &shared, &handle).ThrowOnFailure(); texture9 = shared;
            IDirect3DSurface9* level = null;
            shared->GetSurfaceLevel(0, &level).ThrowOnFailure(); surface = level;
        }
        catch { ReleaseSurface(); throw; }
    }

    internal void Flush()
    {
        context->Flush();
        device->GetDeviceRemovedReason().ThrowOnFailure();
        device9->CheckDeviceState(PInvoke.GetDesktopWindow()).ThrowOnFailure();
    }
    private static void Release(void* value) { if (value != null) ((IUnknown*)value)->Release(); }
    private void ReleaseSurface()
    {
        if (context != null) { context->ClearState(); context->Flush(); }
        Release(surface); surface = null; Release(texture9); texture9 = null; Release(texture); texture = null;
    }
    public void Dispose()
    {
        ReleaseSurface(); Release(context); context = null; Release(device); device = null;
        Release(device9); device9 = null; Release(api); api = null;
    }
}
