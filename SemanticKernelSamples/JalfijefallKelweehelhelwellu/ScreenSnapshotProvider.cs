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

namespace JalfijefallKelweehelhelwellu;

class ScreenSnapshotProvider : IDisposable
{
    public ScreenSnapshotProvider()
    {
        var result = D3D11.D3D11CreateDevice(null, DriverType.Hardware, DeviceCreationFlags.BgraSupport, null, out ID3D11Device? device);
        result.CheckError();

        using var dxgiDevice = device!.QueryInterface<IDXGIDevice>();
        CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice.NativePointer, out var graphicsDevice);

        IDirect3DDevice? direct3DDevice = WinRT.MarshalInterface<IDirect3DDevice>.FromAbi(graphicsDevice);

        var captureItem = GraphicsCaptureItem.TryCreateFromDisplayId(new DisplayId(0));

        var direct3D11CaptureFramePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
            direct3DDevice,
            Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized,
            1,
            new Windows.Graphics.SizeInt32 { Width = 1920, Height = 1080 });

        direct3D11CaptureFramePool.FrameArrived += (sender, args) =>
        {
            var direct3D11CaptureFrame = sender.TryGetNextFrame();
            if (direct3D11CaptureFrame is null)
            {
                return;
            }

            if (_snapshotTakenTaskCompletionSource is { } snapshotTakenTaskCompletionSource && _saveFile is { } saveFile)
            {
                _snapshotTakenTaskCompletionSource = null;
                _saveFile = null;

                SaveFrameToFileAsync(direct3D11CaptureFrame, saveFile)
                    .ContinueWith(saveTask =>
                {
                    if (saveTask.IsCompletedSuccessfully)
                    {
                        snapshotTakenTaskCompletionSource.SetResult();
                    }
                    else
                    {
                        snapshotTakenTaskCompletionSource.SetException(saveTask.Exception!);
                    }
                });
            }
            else
            {
                direct3D11CaptureFrame.Dispose();
            }
        };

        var graphicsCaptureSession = direct3D11CaptureFramePool.CreateCaptureSession(captureItem);
        graphicsCaptureSession.StartCapture();

        _direct3D11CaptureFramePool = direct3D11CaptureFramePool;
        _graphicsCaptureSession = graphicsCaptureSession;
        _direct3DDevice = direct3DDevice;
        _id3D11Device = device;
    }

    public async Task TakeSnapshotAsync(FileInfo saveFile)
    {
        ArgumentNullException.ThrowIfNull(saveFile);

        _saveFile = saveFile;
        _snapshotTakenTaskCompletionSource = new TaskCompletionSource();
        await _snapshotTakenTaskCompletionSource.Task;
    }

    private FileInfo? _saveFile;
    private TaskCompletionSource? _snapshotTakenTaskCompletionSource;
    private Direct3D11CaptureFramePool _direct3D11CaptureFramePool;
    private GraphicsCaptureSession _graphicsCaptureSession;
    private IDirect3DDevice _direct3DDevice;
    private ID3D11Device _id3D11Device;

    private static async Task SaveFrameToFileAsync(Direct3D11CaptureFrame direct3D11CaptureFrame, FileInfo saveFile)
    {
        ArgumentNullException.ThrowIfNull(direct3D11CaptureFrame);

        var storageFolder = await StorageFolder.GetFolderFromPathAsync(saveFile.DirectoryName);
        StorageFile storageFile =
            await storageFolder.CreateFileAsync(saveFile.Name, CreationCollisionOption.FailIfExists);

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
        direct3D11CaptureFrame.Dispose();
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
        CharSet = CharSet.Unicode,
        ExactSpelling = true,
        CallingConvention = CallingConvention.StdCall
    )]
    static extern UInt32 CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);

    public void Dispose()
    {
        _direct3D11CaptureFramePool.Dispose();
        _graphicsCaptureSession.Dispose();
        _direct3DDevice.Dispose();
        _id3D11Device.Dispose();
    }
}