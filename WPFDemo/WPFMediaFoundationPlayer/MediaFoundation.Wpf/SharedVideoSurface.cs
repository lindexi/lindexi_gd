using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Direct3D;
using Windows.Win32.Graphics.Direct3D9;
using Windows.Win32.Graphics.Direct3D11;
using Windows.Win32.Graphics.Dxgi;
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
    private ID3D11Query* query11;
    private IDirect3DQuery9* query9;
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
            Check(PInvoke.Direct3DCreate9Ex(PInvoke.D3D_SDK_VERSION, &created9), "Direct3DCreate9Ex"); api = created9;
            LUID luid;
            Check(api->GetAdapterLUID(0, &luid), "IDirect3D9Ex.GetAdapterLUID(0)");
            var iid = typeof(IDXGIFactory1).GUID;
            void* value = null;
            Check(PInvoke.CreateDXGIFactory1(&iid, &value), "CreateDXGIFactory1"); factory = (IDXGIFactory1*)value;
            for (uint index = 0; ; index++)
            {
                IDXGIAdapter1* candidate = null;
                Check(factory->EnumAdapters1(index, &candidate), "IDXGIFactory1.EnumAdapters1 (D3D9 LUID match)");
                DXGI_ADAPTER_DESC1 description;
                try { Check(candidate->GetDesc1(&description), "IDXGIAdapter1.GetDesc1"); }
                catch { candidate->Release(); throw; }
                if (description.AdapterLuid.LowPart == luid.LowPart && description.AdapterLuid.HighPart == luid.HighPart)
                {
                    System.Diagnostics.Trace.WriteLine($"Adapter name={new string(description.Description.AsSpan()).TrimEnd('\0')} vendor=0x{description.VendorId:X4} device=0x{description.DeviceId:X4} LUID={luid.HighPart:X8}:{luid.LowPart:X8} flags={description.Flags}", "MediaFoundation.Controls");
                    var driverInterface = typeof(ID3D11Device).GUID;
                    long driverVersion = 0;
                    var driverResult = candidate->CheckInterfaceSupport(&driverInterface, &driverVersion);
                    System.Diagnostics.Trace.WriteLine($"Adapter driverVersion=0x{driverVersion:X16} queryHRESULT=0x{driverResult.Value:X8} (version available only on successful query)", "MediaFoundation.Controls");
                    adapter = candidate; break;
                }
                candidate->Release();
            }
            var window = PInvoke.GetDesktopWindow();
            var present = new D3DPRESENT_PARAMETERS
            {
                Windowed = true, hDeviceWindow = window, BackBufferWidth = 1, BackBufferHeight = 1, BackBufferCount = 1,
                BackBufferFormat = D3DFORMAT.D3DFMT_A8R8G8B8, SwapEffect = D3DSWAPEFFECT.D3DSWAPEFFECT_DISCARD,
                PresentationInterval = unchecked((uint)PInvoke.D3DPRESENT_INTERVAL_IMMEDIATE)
            };
            System.Diagnostics.Trace.WriteLine($"D3D9 CreateDeviceEx flags=HARDWARE_VERTEXPROCESSING|MULTITHREADED|FPU_PRESERVE (0x{(PInvoke.D3DCREATE_HARDWARE_VERTEXPROCESSING | PInvoke.D3DCREATE_MULTITHREADED | PInvoke.D3DCREATE_FPU_PRESERVE):X}) adapter=0", "MediaFoundation.Controls");
            IDirect3DDevice9Ex* createdDevice9 = null;
            Check(api->CreateDeviceEx(0, D3DDEVTYPE.D3DDEVTYPE_HAL, window,
                PInvoke.D3DCREATE_HARDWARE_VERTEXPROCESSING | PInvoke.D3DCREATE_MULTITHREADED | PInvoke.D3DCREATE_FPU_PRESERVE,
                &present, null, &createdDevice9), "IDirect3D9Ex.CreateDeviceEx"); device9 = createdDevice9;
            ID3D11Device* created = null;
            ID3D11DeviceContext* immediate = null;
            var result = PInvoke.D3D11CreateDevice((IDXGIAdapter*)adapter, D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_UNKNOWN, default,
                D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_BGRA_SUPPORT | D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_VIDEO_SUPPORT,
                null, 0, PInvoke.D3D11_SDK_VERSION, &created, null, &immediate);
            System.Diagnostics.Trace.WriteLine($"D3D11CreateDevice flags=BGRA_SUPPORT|VIDEO_SUPPORT (0x820) HRESULT=0x{result.Value:X8}", "MediaFoundation.Controls");
            if (result.Value == unchecked((int)0x887A0004))
            {
                Release(immediate); Release(created); immediate = null; created = null;
                System.Diagnostics.Trace.WriteLine("D3D11CreateDevice VIDEO_SUPPORT: 0x887A0004; retry same adapter BGRA_SUPPORT; Media Engine and frame transfer must still succeed.", "MediaFoundation.Controls");
                result = PInvoke.D3D11CreateDevice((IDXGIAdapter*)adapter, D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_UNKNOWN, default,
                    D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_BGRA_SUPPORT,
                    null, 0, PInvoke.D3D11_SDK_VERSION, &created, null, &immediate);
                System.Diagnostics.Trace.WriteLine($"D3D11CreateDevice fallback flags=BGRA_SUPPORT (0x20) HRESULT=0x{result.Value:X8}", "MediaFoundation.Controls");
                device = created; context = immediate;
                Check(result, "D3D11CreateDevice (same adapter, BGRA_SUPPORT; VIDEO_SUPPORT returned 0x887A0004)");
            }
            device = created; context = immediate;
            Check(result, "D3D11CreateDevice (same adapter, BGRA_SUPPORT | VIDEO_SUPPORT)");
            iid = typeof(ID3D11Multithread).GUID;
            value = null;
            Check(context->QueryInterface(&iid, &value), "ID3D11DeviceContext.QueryInterface(ID3D11Multithread)");
            try { ((ID3D11Multithread*)value)->SetMultithreadProtected(true); }
            finally { Release(value); }
            var queryDescription = new D3D11_QUERY_DESC { Query = D3D11_QUERY.D3D11_QUERY_EVENT };
            ID3D11Query* createdQuery11 = null;
            Check(device->CreateQuery(&queryDescription, &createdQuery11), "ID3D11Device.CreateQuery(EVENT)");
            query11 = createdQuery11;
            IDirect3DQuery9* createdQuery9 = null;
            Check(device9->CreateQuery(D3DQUERYTYPE.D3DQUERYTYPE_EVENT, &createdQuery9), "IDirect3DDevice9.CreateQuery(EVENT)");
            query9 = createdQuery9;
        }
        catch { Dispose(); throw; }
        finally { Release(adapter); Release(factory); }
    }

    internal void Resize(int width, int height)
    {
        ReleaseSurface();
        try
        {
            // D3D9/D3D11 interop requires creation on D3D9, then OpenSharedResource on the same adapter.
            HANDLE handle = default;
            IDirect3DTexture9* shared = null;
            Check(device9->CreateTexture((uint)width, (uint)height, 1, 1, D3DFORMAT.D3DFMT_A8R8G8B8,
                D3DPOOL.D3DPOOL_DEFAULT, &shared, &handle), "IDirect3DDevice9Ex.CreateTexture (shared A8R8G8B8 render target)");
            texture9 = shared;
            var iid = typeof(ID3D11Texture2D).GUID;
            void* opened = null;
            Check(device->OpenSharedResource(handle, &iid, &opened), "ID3D11Device.OpenSharedResource(ID3D11Texture2D)");
            texture = (ID3D11Texture2D*)opened;
            IDirect3DSurface9* level = null;
            Check(shared->GetSurfaceLevel(0, &level), "IDirect3DTexture9.GetSurfaceLevel(0)"); surface = level;
            Width = width; Height = height;
            D3D11_TEXTURE2D_DESC description;
            texture->GetDesc(&description);
            System.Diagnostics.Trace.WriteLine($"SharedTexture bound D3D9->D3D11->D3DImage size={width}x{height} format={description.Format} bind={description.BindFlags} misc={description.MiscFlags} usage={description.Usage}", "MediaFoundation.Controls");
        }
        catch { ReleaseSurface(); throw; }
    }

    private static void Check(HRESULT result, string stage)
    {
        if (result.Value < 0 || (!stage.Contains("GetData", StringComparison.Ordinal) && !stage.Contains("GetDeviceRemovedReason", StringComparison.Ordinal) && !stage.Contains("CheckDeviceState", StringComparison.Ordinal) && !stage.Contains("Issue", StringComparison.Ordinal)))
            System.Diagnostics.Trace.WriteLine($"stage={stage} HRESULT=0x{result.Value:X8}", "MediaFoundation.Controls");
        if (result.Value < 0) throw new COMException(string.Format(Text.Get("NativeStageFailure"), stage, result.Value), result.Value);
    }
    internal void Flush()
    {
        context->End((ID3D11Asynchronous*)query11);
        context->Flush();
        var deadline = System.Diagnostics.Stopwatch.StartNew();
        HRESULT result;
        while ((result = context->GetData((ID3D11Asynchronous*)query11, null, 0, 0)).Value == 1)
        {
            Check(device->GetDeviceRemovedReason(), "ID3D11Device.GetDeviceRemovedReason");
            if (deadline.Elapsed > TimeSpan.FromSeconds(2)) throw new InvalidOperationException(Text.Get("NotReady"));
            Thread.Yield();
        }
        Check(result, "ID3D11DeviceContext.GetData(EVENT)");
        Check(query9->Issue(1), "IDirect3DQuery9.Issue(END)");
        while ((result = query9->GetData(null, 0, 1)).Value == 1)
        {
            if (deadline.Elapsed > TimeSpan.FromSeconds(2)) throw new InvalidOperationException(Text.Get("NotReady"));
            Thread.Yield();
        }
        Check(result, "IDirect3DQuery9.GetData(FLUSH)");
        Check(device->GetDeviceRemovedReason(), "ID3D11Device.GetDeviceRemovedReason");
        Check(device9->CheckDeviceState(PInvoke.GetDesktopWindow()), "IDirect3DDevice9Ex.CheckDeviceState");
    }
    internal void DiagnoseFrame()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("MF_DIAGNOSE_FRAME"), "1", StringComparison.Ordinal)) return;
        ID3D11Texture2D* staging = null;
        try
        {
            D3D11_TEXTURE2D_DESC desc;
            texture->GetDesc(&desc);
            desc.Usage = D3D11_USAGE.D3D11_USAGE_STAGING;
            desc.BindFlags = 0; desc.MiscFlags = 0;
            desc.CPUAccessFlags = D3D11_CPU_ACCESS_FLAG.D3D11_CPU_ACCESS_READ;
            Check(device->CreateTexture2D(&desc, null, &staging), "Diagnostic.CreateStagingTexture");
            context->CopyResource((ID3D11Resource*)staging, (ID3D11Resource*)texture);
            D3D11_MAPPED_SUBRESOURCE mapped;
            Check(context->Map((ID3D11Resource*)staging, 0, D3D11_MAP.D3D11_MAP_READ, 0, &mapped), "Diagnostic.MapFrame");
            try
            {
                var colors = new HashSet<uint>();
                for (int y = 0; y < Height; y += 3)
                    for (int x = 0; x < Width; x += 3)
                        colors.Add(*(uint*)((byte*)mapped.pData + y * mapped.RowPitch + x * 4) & 0x00FFFFFF);
                System.Diagnostics.Trace.WriteLine($"D3DImage GPU readback {Width}x{Height} colors={colors.Count}", "MediaFoundation.Controls");
            }
            finally { context->Unmap((ID3D11Resource*)staging, 0); }
        }
        finally { Release(staging); }
    }
    private static void Release(void* value) { if (value != null) ((IUnknown*)value)->Release(); }
    private void ReleaseSurface()
    {
        if (context != null) { context->ClearState(); context->Flush(); }
        Release(surface); surface = null; Release(texture9); texture9 = null; Release(texture); texture = null;
        Width = Height = 0;
    }
    public void Dispose()
    {
        ReleaseSurface(); Release(query11); query11 = null; Release(query9); query9 = null;
        Release(context); context = null; Release(device); device = null;
        Release(device9); device9 = null; Release(api); api = null;
    }
}
