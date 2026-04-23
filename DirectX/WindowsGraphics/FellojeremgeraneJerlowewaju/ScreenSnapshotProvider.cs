using System.Runtime.InteropServices;

using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace FellojeremgeraneJerlowewaju;

class ScreenSnapshotProvider
{
    public async Task<FileInfo> TakeSnapshotAsync()
    {
        // 可以不调用 InitializeComWrappers 方法。多调用也没啥坏处
        //global::WinRT.ComWrappersSupport.InitializeComWrappers();

        // 这里的权限请求也可以不调用，直接创建捕获会话。多调用也没啥坏处
        //if (GraphicsCaptureSession.IsSupported())
        //{
        //    var status = await GraphicsCaptureAccess.RequestAccessAsync(GraphicsCaptureAccessKind.Programmatic);
        //}

        var result = D3D11.D3D11CreateDevice(null, DriverType.Hardware, DeviceCreationFlags.BgraSupport, null, out ID3D11Device? device);
        result.CheckError();

        using var disposeDevice = device;

        using var dxgiDevice = device!.QueryInterface<IDXGIDevice>();

        // 不能这样直接转换，应该在 WinRT 里面应该调用 FromAbi 才能转换
        // 在这个方法里面用的是 Marshal.GetObjectForIUnknown 转换，这是不正确的
        //IDirect3DDevice direct3DDevice = Vortice.Direct3D11.D3D11.CreateDirect3D11DeviceFromDXGIDevice<IDirect3DDevice>(dxgiDevice);

        CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice.NativePointer, out var graphicsDevice);
        IDirect3DDevice? direct3DDevice = WinRT.MarshalInterface<IDirect3DDevice>.FromAbi(graphicsDevice);

        // 这里的大小参数也是固定的
        using Direct3D11CaptureFramePool direct3D11CaptureFramePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
            direct3DDevice,
            Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized,
            1,
            new Windows.Graphics.SizeInt32 { Width = 1920, Height = 1080 });

        // 这里的需求只是取一个截图界面，那就不需要监听事件。如果要监听事件，需要小心 Direct3D11CaptureFramePool 被释放，建议需要放在字段
        //direct3D11CaptureFramePool.FrameArrived += (sender, args) =>
        //{
        //    var direct3D11CaptureFrame = direct3D11CaptureFramePool.TryGetNextFrame();
        //};

        // 如果有多个屏幕，这里需要优化
        var captureItem = GraphicsCaptureItem.TryCreateFromDisplayId(new DisplayId(0));
        using GraphicsCaptureSession graphicsCaptureSession = direct3D11CaptureFramePool.CreateCaptureSession(captureItem);
        graphicsCaptureSession.StartCapture();

        while (true)
        {
            var direct3D11CaptureFrame = direct3D11CaptureFramePool.TryGetNextFrame();
            if (direct3D11CaptureFrame is not null)
            {
                using (direct3D11CaptureFrame)
                {
                    return await SaveFrameToFileAsync(direct3D11CaptureFrame);
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(0.1));
        }
    }

    private static async Task<FileInfo> SaveFrameToFileAsync(Direct3D11CaptureFrame direct3D11CaptureFrame)
    {
        ArgumentNullException.ThrowIfNull(direct3D11CaptureFrame);

        var fileName = $"{Path.GetFileNameWithoutExtension(Path.GetRandomFileName())}.png";
        var tempFolderPath = Path.GetTempPath();
        StorageFolder tempFolder = await StorageFolder.GetFolderFromPathAsync(tempFolderPath);
        StorageFile storageFile = await tempFolder.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);

        using SoftwareBitmap softwareBitmap = await SoftwareBitmap.CreateCopyFromSurfaceAsync(
            direct3D11CaptureFrame.Surface,
            BitmapAlphaMode.Premultiplied);
        using SoftwareBitmap bitmapToSave = SoftwareBitmap.Convert(
            softwareBitmap,
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied);
        using IRandomAccessStream stream = await storageFile.OpenAsync(FileAccessMode.ReadWrite);

        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
        encoder.SetSoftwareBitmap(bitmapToSave);
        await encoder.FlushAsync();
        
        return new FileInfo(storageFile.Path);
    }

    /// <summary>
    /// 把自己创建的原生Win32 `IDXGIDevice`（从D3D11设备查询得到），封装转换成WinRT标准的 `Windows.Graphics.DirectX.Direct3D11.IDirect3DDevice` 对象
    /// </summary>
    /// <param name="dxgiDevice"></param>
    /// <param name="graphicsDevice"></param>
    /// <returns></returns>
    [DllImport
    (
        "d3d11.dll",
        EntryPoint = "CreateDirect3D11DeviceFromDXGIDevice",
        SetLastError = true,
        CharSet = CharSet.Unicode
    )]
    static extern UInt32 CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);
}