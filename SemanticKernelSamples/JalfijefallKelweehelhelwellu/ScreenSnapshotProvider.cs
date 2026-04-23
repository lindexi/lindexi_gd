
using System.Runtime.InteropServices;
using DirectN.Extensions;
using DirectN.Extensions.Utilities;

using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using DirectN;
using WinRT;

namespace JalfijefallKelweehelhelwellu;

class ScreenSnapshotProvider
{
    public async Task TakeSnapshot()
    {
        global::WinRT.ComWrappersSupport.InitializeComWrappers();

        if (GraphicsCaptureSession.IsSupported())
        {
            var status = await GraphicsCaptureAccess.RequestAccessAsync(GraphicsCaptureAccessKind.Programmatic);
        }

        var d3D11CreateDevice = D3D11Functions.D3D11CreateDevice(null,D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_HARDWARE,D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_BGRA_SUPPORT);
        ID3D11Device d3D11Device = d3D11CreateDevice.Object;

        var isComObject = Marshal.IsComObject(d3D11CreateDevice);

        var comInterfaceForObject = Marshal.GetComInterfaceForObject<ID3D11Device, ID3D11Device>(d3D11Device);

        IDirect3DDevice device = d3D11CreateDevice.As<IDirect3DDevice>();

        var direct3D11CaptureFramePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
            device,
            Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized,
            1,
            new Windows.Graphics.SizeInt32 { Width = 1, Height = 1 });
        

    }
}