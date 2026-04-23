
using Windows.Graphics.Capture;

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

        var direct3D11CaptureFramePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
            Direct3D11Helper.GetDefaultAdapter(),
            Direct3D11Helper.GetDevice(),
            Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized,
            1,
            new Windows.Graphics.SizeInt32 { Width = 1, Height = 1 });
        

    }
}