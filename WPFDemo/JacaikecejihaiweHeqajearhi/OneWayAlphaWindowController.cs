using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Shell;
using System.Windows.Threading;

namespace JacaikecejihaiweHeqajearhi;

internal enum AlphaUpgradeState
{
    OpaqueX8,
    Upgrading,
    AlphaModeApplied,
    Faulted,
    Closed,
}

internal sealed class OneWayAlphaWindowController : IDisposable
{
    private const int GwlExStyle = -20;
    private const int WmStyleChanging = 0x007C;
    private const uint WsExLayered = 0x00080000U;

    private readonly Window _window;
    private readonly HwndSourceHook _windowMessageHook;
    private readonly CancellationTokenSource _lifetimeCancellationTokenSource = new();

    private HwndSource? _source;
    private Task? _upgradeTask;
    private IntPtr _hwnd;
    private bool _keepLayeredStyle;
    private bool _hookInstalled;
    private bool _disposed;

    internal OneWayAlphaWindowController(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        _window = window;
        _windowMessageHook = WindowMessageHook;
        State = AlphaUpgradeState.OpaqueX8;

        _window.SourceInitialized += OnSourceInitialized;
        _window.Closed += OnWindowClosed;

        if (new WindowInteropHelper(_window).Handle != IntPtr.Zero)
        {
            InitializeHwnd();
        }
    }

    internal AlphaUpgradeState State { get; private set; }

    internal Task UpgradeToTransparentAsync(Action prepareTransparentContent)
    {
        ArgumentNullException.ThrowIfNull(prepareTransparentContent);

        ThrowIfDisposed();
        _window.Dispatcher.VerifyAccess();

        if (State == AlphaUpgradeState.AlphaModeApplied)
        {
            return Task.CompletedTask;
        }

        if (_upgradeTask is not null)
        {
            return _upgradeTask;
        }

        if (State != AlphaUpgradeState.OpaqueX8)
        {
            throw new InvalidOperationException(
                $"Cannot upgrade the window while its state is {State}.");
        }

        _upgradeTask = UpgradeCoreAsync(prepareTransparentContent);
        return _upgradeTask;
    }

    internal void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _keepLayeredStyle = false;
        _lifetimeCancellationTokenSource.Cancel();

        _window.SourceInitialized -= OnSourceInitialized;
        _window.Closed -= OnWindowClosed;
        RemoveHook();

        _lifetimeCancellationTokenSource.Dispose();
        State = AlphaUpgradeState.Closed;
    }

    void IDisposable.Dispose()
    {
        Dispose();
    }

    private async Task UpgradeCoreAsync(Action prepareTransparentContent)
    {
        CancellationToken cancellationToken = _lifetimeCancellationTokenSource.Token;

        EnsureHwndInitialized();
        State = AlphaUpgradeState.Upgrading;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            InstallHook();

            prepareTransparentContent();
            cancellationToken.ThrowIfCancellationRequested();
            InstallFullGlassWindowChrome();

            await WaitForRenderAsync(cancellationToken);
            ThrowIfDisposed();
            VerifyHwnd();
            VerifyTransparentClearColor();

            cancellationToken.ThrowIfCancellationRequested();
            _keepLayeredStyle = true;
            SetExtendedStyle(GetExtendedStyle() | WsExLayered);
            VerifyLayeredStyle();

            State = AlphaUpgradeState.AlphaModeApplied;
        }
        catch
        {
            if (!_disposed)
            {
                if ((GetExtendedStyleSafely() & WsExLayered) == 0)
                {
                    _keepLayeredStyle = false;
                    RemoveHook();
                }

                State = AlphaUpgradeState.Faulted;
            }

            throw;
        }
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        InitializeHwnd();
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        Dispose();
    }

    private void InitializeHwnd()
    {
        _window.Dispatcher.VerifyAccess();

        IntPtr hwnd = new WindowInteropHelper(_window).Handle;
        if (hwnd == IntPtr.Zero)
        {
            throw new InvalidOperationException("The Window does not have an HWND yet.");
        }

        HwndSource source = HwndSource.FromHwnd(hwnd)
            ?? throw new InvalidOperationException("Cannot obtain the HwndSource for the Window.");

        if (_hwnd != IntPtr.Zero && _hwnd != hwnd)
        {
            throw new InvalidOperationException("The Window HWND changed during its lifetime.");
        }

        _hwnd = hwnd;
        _source = source;
    }

    private void EnsureHwndInitialized()
    {
        if (_hwnd == IntPtr.Zero || _source is null)
        {
            InitializeHwnd();
        }
    }

    private void InstallHook()
    {
        if (_hookInstalled)
        {
            return;
        }

        HwndSource source = _source
            ?? throw new InvalidOperationException("The Window HwndSource is not initialized.");

        source.AddHook(_windowMessageHook);
        _hookInstalled = true;
    }

    private void RemoveHook()
    {
        if (!_hookInstalled || _source is null)
        {
            return;
        }

        if (!_source.IsDisposed)
        {
            _source.RemoveHook(_windowMessageHook);
        }

        _hookInstalled = false;
    }

    private IntPtr WindowMessageHook(
        IntPtr hwnd,
        int message,
        IntPtr wParam,
        IntPtr lParam,
        ref bool handled)
    {
        if (_keepLayeredStyle &&
            message == WmStyleChanging &&
            wParam.ToInt64() == GwlExStyle)
        {
            StyleStruct style = Marshal.PtrToStructure<StyleStruct>(lParam);
            style.StyleNew |= WsExLayered;
            Marshal.StructureToPtr(style, lParam, false);
            handled = true;
        }

        return IntPtr.Zero;
    }

    private void InstallFullGlassWindowChrome()
    {
        WindowChrome.SetWindowChrome(
            _window,
            new WindowChrome
            {
                GlassFrameThickness = WindowChrome.GlassFrameCompleteThickness,
                CaptionHeight = 0,
                CornerRadius = new CornerRadius(),
                ResizeBorderThickness = new Thickness(),
                UseAeroCaptionButtons = false,
            });
    }

    private async Task WaitForRenderAsync(CancellationToken cancellationToken)
    {
        await _window.Dispatcher.InvokeAsync(
            static () => { },
            DispatcherPriority.Render,
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        _window.InvalidateVisual();

        await _window.Dispatcher.InvokeAsync(
            static () => { },
            DispatcherPriority.ContextIdle,
            cancellationToken);
    }

    private uint GetExtendedStyle()
    {
        SetLastError(0);
        IntPtr value = GetWindowLongPtr(_hwnd, GwlExStyle);
        int error = Marshal.GetLastWin32Error();

        if (value == IntPtr.Zero && error != 0)
        {
            throw new Win32Exception(error);
        }

        return unchecked((uint)value.ToInt64());
    }

    private void SetExtendedStyle(uint style)
    {
        SetLastError(0);
        IntPtr previous = SetWindowLongPtr(
            _hwnd,
            GwlExStyle,
            new IntPtr(unchecked((int)style)));

        int error = Marshal.GetLastWin32Error();
        if (previous == IntPtr.Zero && error != 0)
        {
            throw new Win32Exception(error);
        }
    }

    private void VerifyHwnd()
    {
        IntPtr currentHwnd = new WindowInteropHelper(_window).Handle;
        if (_hwnd == IntPtr.Zero ||
            currentHwnd != _hwnd ||
            _source is null ||
            _source.IsDisposed ||
            !IsWindow(_hwnd))
        {
            throw new InvalidOperationException("The Window HWND is no longer valid.");
        }
    }

    private void VerifyTransparentClearColor()
    {
        HwndTarget? target = _source?.CompositionTarget;
        if (target is null || target.BackgroundColor.A != 0)
        {
            throw new NotSupportedException(
                "WindowChrome did not establish a transparent clear color.");
        }
    }

    private uint GetExtendedStyleSafely()
    {
        if (_hwnd == IntPtr.Zero || !IsWindow(_hwnd))
        {
            return 0;
        }

        try
        {
            return GetExtendedStyle();
        }
        catch (Win32Exception)
        {
            return WsExLayered;
        }
    }

    private void VerifyLayeredStyle()
    {
        if ((GetExtendedStyle() & WsExLayered) == 0)
        {
            throw new InvalidOperationException("WPF or the operating system removed WS_EX_LAYERED.");
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(OneWayAlphaWindowController));
        }
    }

    private static IntPtr GetWindowLongPtr(IntPtr hwnd, int index)
    {
        return IntPtr.Size == 8
            ? GetWindowLongPtr64(hwnd, index)
            : new IntPtr(GetWindowLong32(hwnd, index));
    }

    private static IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value)
    {
        return IntPtr.Size == 8
            ? SetWindowLongPtr64(hwnd, index, value)
            : new IntPtr(SetWindowLong32(hwnd, index, value.ToInt32()));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct StyleStruct
    {
        public uint StyleOld;
        public uint StyleNew;
    }

    [DllImport("kernel32.dll")]
    private static extern void SetLastError(uint errorCode);

    [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
    private static extern int GetWindowLong32(IntPtr hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
    private static extern int SetWindowLong32(IntPtr hwnd, int index, int value);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hwnd, int index, IntPtr value);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindow(IntPtr hwnd);
}
