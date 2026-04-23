using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;

using Microsoft.UI;

using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

using GraphicsDisplayId = Windows.Graphics.DisplayId;

namespace JalfijefallKelweehelhelwellu;

sealed record ScreenSnapshotDisplay(string Key, string Name);

class ScreenSnapshotProvider : IDisposable
{
    public ScreenSnapshotProvider()
    {
        global::WinRT.ComWrappersSupport.InitializeComWrappers();
        if (!GraphicsCaptureSession.IsSupported())
        {
            throw new PlatformNotSupportedException("当前系统不支持 Windows.Graphics.Capture 屏幕捕获。");
        }

        var result = D3D11.D3D11CreateDevice(null, DriverType.Hardware, DeviceCreationFlags.BgraSupport, null, out ID3D11Device? device);
        result.CheckError();

        using var dxgiDevice = device!.QueryInterface<IDXGIDevice>();
        var createGraphicsDeviceResult = CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice.NativePointer, out var graphicsDevice);
        Marshal.ThrowExceptionForHR((int)createGraphicsDeviceResult);

        try
        {
            _direct3DDevice = WinRT.MarshalInterface<IDirect3DDevice>.FromAbi(graphicsDevice)
                ?? throw new InvalidOperationException("无法创建 WinRT Direct3D 设备。");
        }
        finally
        {
            Marshal.Release(graphicsDevice);
        }

        _id3D11Device = device;
        _displayContexts = CreateDisplayContexts(_direct3DDevice);
        _displayContextMap = _displayContexts.ToDictionary(context => context.Display.Key, StringComparer.Ordinal);
        _displays = new ReadOnlyCollection<ScreenSnapshotDisplay>(_displayContexts.Select(context => context.Display).ToList());
    }

    public IReadOnlyList<ScreenSnapshotDisplay> GetDisplays()
    {
        return _displays;
    }

    public Task TakeSnapshotAsync(FileInfo saveFile)
    {
        ArgumentNullException.ThrowIfNull(saveFile);

        var display = _displayContexts.FirstOrDefault()?.Display
            ?? throw new InvalidOperationException("当前没有可用的显示器可供截图。");
        return TakeSnapshotAsync(display, saveFile);
    }

    public Task TakeSnapshotAsync(ScreenSnapshotDisplay display, FileInfo saveFile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(display);
        ArgumentNullException.ThrowIfNull(saveFile);
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_displayContextMap.TryGetValue(display.Key, out var displayContext))
        {
            throw new InvalidOperationException($"找不到屏幕 `{display.Name}` 对应的捕获会话。");
        }

        return displayContext.TakeSnapshotAsync(saveFile, cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var displayContext in _displayContexts)
        {
            displayContext.Dispose();
        }

        _direct3DDevice.Dispose();
        _id3D11Device.Dispose();
    }

    private readonly ReadOnlyCollection<ScreenSnapshotDisplay> _displays;
    private readonly List<DisplayCaptureContext> _displayContexts;
    private readonly Dictionary<string, DisplayCaptureContext> _displayContextMap;
    private readonly IDirect3DDevice _direct3DDevice;
    private readonly ID3D11Device _id3D11Device;
    private bool _disposed;

    private static List<DisplayCaptureContext> CreateDisplayContexts(IDirect3DDevice direct3DDevice)
    {
        ArgumentNullException.ThrowIfNull(direct3DDevice);

        List<DisplayDefinition> displayDefinitions = [];
        var success = EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (monitorHandle, _, _, _) =>
        {
            MONITORINFOEX monitorInfo = new()
            {
                cbSize = Marshal.SizeOf<MONITORINFOEX>()
            };

            if (!GetMonitorInfo(monitorHandle, ref monitorInfo))
            {
                return true;
            }

            displayDefinitions.Add(new DisplayDefinition(
                monitorHandle,
                monitorInfo.szDevice,
                (monitorInfo.dwFlags & MonitorInfoPrimaryFlag) != 0,
                monitorInfo.rcMonitor.Left,
                monitorInfo.rcMonitor.Top));
            return true;
        }, IntPtr.Zero);

        if (!success)
        {
            throw new InvalidOperationException("无法枚举当前显示器。");
        }

        return displayDefinitions
            .OrderBy(definition => definition.Top)
            .ThenBy(definition => definition.Left)
            .Select((definition, index) => CreateDisplayContext(definition, index, direct3DDevice))
            .Where(context => context is not null)
            .Cast<DisplayCaptureContext>()
            .ToList();
    }

    private static DisplayCaptureContext? CreateDisplayContext(DisplayDefinition definition, int displayIndex, IDirect3DDevice direct3DDevice)
    {
        GraphicsDisplayId displayId;

        try
        {
            var interopDisplayId = Win32Interop.GetDisplayIdFromMonitor(definition.MonitorHandle);
            displayId = new GraphicsDisplayId { Value = interopDisplayId.Value };
        }
        catch (Exception e)
        {
            // [Starting with v1.1.4, bootstrapper fails for unpacked apps with 'access is denied' · Issue #2918 · microsoft/WindowsAppSDK](https://github.com/microsoft/WindowsAppSDK/issues/2918 )
            // [Microsoft.Internal.FrameworkUdk.System.dll not resolving/loading · Issue #6944 · microsoft/microsoft-ui-xaml](https://github.com/microsoft/microsoft-ui-xaml/issues/6944 )
            // [Microsoft.Internal.FrameworkUdk.dll fails to dynamically load in call to PushNotificationManager.CreateChannelAsync from an upackaged app · Issue #3106 · microsoft/WindowsAppSDK](https://github.com/microsoft/WindowsAppSDK/issues/3106 )
            displayId = new GraphicsDisplayId { Value = (ulong) displayIndex };
        }

        var captureItem = GraphicsCaptureItem.TryCreateFromDisplayId(displayId);
        if (captureItem is null)
        {
            return null;
        }

        var displayKey = $"screen{displayIndex:D2}";
        var displayName = definition.IsPrimary
            ? $"屏幕 {displayIndex}（主屏幕，{definition.DeviceName}）"
            : $"屏幕 {displayIndex}（{definition.DeviceName}）";

        var display = new ScreenSnapshotDisplay(displayKey, displayName);
        return new DisplayCaptureContext(display, captureItem, direct3DDevice);
    }

    private static async Task SaveFrameToFileAsync(Direct3D11CaptureFrame direct3D11CaptureFrame, FileInfo saveFile)
    {
        ArgumentNullException.ThrowIfNull(direct3D11CaptureFrame);
        ArgumentNullException.ThrowIfNull(saveFile);

        if (saveFile.DirectoryName is null)
        {
            throw new ArgumentException("截图文件必须位于有效目录中。", nameof(saveFile));
        }

        var storageFolder = await StorageFolder.GetFolderFromPathAsync(saveFile.DirectoryName);
        var storageFile = await storageFolder.CreateFileAsync(saveFile.Name, CreationCollisionOption.ReplaceExisting);

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
    }

    /// <summary>
    /// 把自己创建的原生Win32 `IDXGIDevice`（从D3D11设备查询得到），封装转换成WinRT标准的 `Windows.Graphics.DirectX.Direct3D11.IDirect3DDevice` 对象
    /// </summary>
    [DllImport
    (
        "d3d11.dll",
        EntryPoint = "CreateDirect3D11DeviceFromDXGIDevice",
        SetLastError = true,
        CharSet = CharSet.Unicode,
        ExactSpelling = true,
        CallingConvention = CallingConvention.StdCall
    )]
    private static extern uint CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);

    private const uint MonitorInfoPrimaryFlag = 0x00000001;

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayMonitors(
        IntPtr hdc,
        IntPtr lprcClip,
        MonitorEnumProc lpfnEnum,
        IntPtr dwData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, IntPtr lprcMonitor, IntPtr dwData);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MONITORINFOEX
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    private sealed record DisplayDefinition(
        IntPtr MonitorHandle,
        string DeviceName,
        bool IsPrimary,
        int Left,
        int Top);

    private sealed class DisplayCaptureContext : IDisposable
    {
        public DisplayCaptureContext(ScreenSnapshotDisplay display, GraphicsCaptureItem captureItem, IDirect3DDevice direct3DDevice)
        {
            Display = display;
            _captureItem = captureItem;
            _direct3DDevice = direct3DDevice;
            _currentSize = captureItem.Size;

            _direct3D11CaptureFramePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
                direct3DDevice,
                Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized,
                1,
                _currentSize);

            _direct3D11CaptureFramePool.FrameArrived += OnFrameArrived;
            _graphicsCaptureSession = _direct3D11CaptureFramePool.CreateCaptureSession(_captureItem);
            _graphicsCaptureSession.StartCapture();
        }

        public ScreenSnapshotDisplay Display { get; }

        public Task TakeSnapshotAsync(FileInfo saveFile, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(saveFile);

            if (saveFile.Directory is null)
            {
                throw new ArgumentException("截图文件必须位于有效目录中。", nameof(saveFile));
            }

            saveFile.Directory.Create();

            lock (_syncRoot)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                if (_snapshotTakenTaskCompletionSource is not null)
                {
                    throw new InvalidOperationException($"屏幕 `{Display.Name}` 当前还有未完成的截图任务。");
                }

                _saveFile = saveFile;
                _snapshotTakenTaskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                _cancellationRegistration = cancellationToken.CanBeCanceled
                    ? cancellationToken.Register(static state => ((DisplayCaptureContext)state!).CancelPendingSnapshot(), this)
                    : default;
                return _snapshotTakenTaskCompletionSource.Task;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _direct3D11CaptureFramePool.FrameArrived -= OnFrameArrived;

            TaskCompletionSource? pendingTaskCompletionSource;

            lock (_syncRoot)
            {
                pendingTaskCompletionSource = _snapshotTakenTaskCompletionSource;
                _snapshotTakenTaskCompletionSource = null;
                _saveFile = null;
                _cancellationRegistration.Dispose();
            }

            pendingTaskCompletionSource?.TrySetException(new ObjectDisposedException(nameof(ScreenSnapshotProvider)));
            _graphicsCaptureSession.Dispose();
            _direct3D11CaptureFramePool.Dispose();
        }

        private readonly object _syncRoot = new();
        private readonly GraphicsCaptureItem _captureItem;
        private readonly IDirect3DDevice _direct3DDevice;
        private readonly Direct3D11CaptureFramePool _direct3D11CaptureFramePool;
        private readonly GraphicsCaptureSession _graphicsCaptureSession;
        private FileInfo? _saveFile;
        private TaskCompletionSource? _snapshotTakenTaskCompletionSource;
        private CancellationTokenRegistration _cancellationRegistration;
        private SizeInt32 _currentSize;
        private bool _disposed;

        private void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
        {
            var direct3D11CaptureFrame = sender.TryGetNextFrame();
            if (direct3D11CaptureFrame is null)
            {
                return;
            }

            if (direct3D11CaptureFrame.ContentSize.Width > 0
                && direct3D11CaptureFrame.ContentSize.Height > 0
                && (direct3D11CaptureFrame.ContentSize.Width != _currentSize.Width
                    || direct3D11CaptureFrame.ContentSize.Height != _currentSize.Height))
            {
                _currentSize = direct3D11CaptureFrame.ContentSize;
                sender.Recreate(
                    _direct3DDevice,
                    Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized,
                    1,
                    _currentSize);
            }

            TaskCompletionSource? snapshotTakenTaskCompletionSource;
            FileInfo? saveFile;
            CancellationTokenRegistration cancellationRegistration;

            lock (_syncRoot)
            {
                snapshotTakenTaskCompletionSource = _snapshotTakenTaskCompletionSource;
                saveFile = _saveFile;
                cancellationRegistration = _cancellationRegistration;

                _snapshotTakenTaskCompletionSource = null;
                _saveFile = null;
                _cancellationRegistration = default;
            }

            if (snapshotTakenTaskCompletionSource is null || saveFile is null)
            {
                cancellationRegistration.Dispose();
                direct3D11CaptureFrame.Dispose();
                return;
            }

            _ = CompleteSnapshotAsync(direct3D11CaptureFrame, saveFile, snapshotTakenTaskCompletionSource, cancellationRegistration);
        }

        private async Task CompleteSnapshotAsync(
            Direct3D11CaptureFrame direct3D11CaptureFrame,
            FileInfo saveFile,
            TaskCompletionSource snapshotTakenTaskCompletionSource,
            CancellationTokenRegistration cancellationRegistration)
        {
            try
            {
                await SaveFrameToFileAsync(direct3D11CaptureFrame, saveFile);
                snapshotTakenTaskCompletionSource.TrySetResult();
            }
            catch (Exception exception)
            {
                snapshotTakenTaskCompletionSource.TrySetException(exception);
            }
            finally
            {
                cancellationRegistration.Dispose();
                direct3D11CaptureFrame.Dispose();
            }
        }

        private void CancelPendingSnapshot()
        {
            TaskCompletionSource? snapshotTakenTaskCompletionSource;

            lock (_syncRoot)
            {
                snapshotTakenTaskCompletionSource = _snapshotTakenTaskCompletionSource;
                _snapshotTakenTaskCompletionSource = null;
                _saveFile = null;
                _cancellationRegistration = default;
            }

            snapshotTakenTaskCompletionSource?.TrySetCanceled();
        }
    }
}
