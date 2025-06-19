using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Runtime.InteropServices;
using static DotNetCampus.Installer.Boost.Win32;
using static DotNetCampus.Installer.Boost.Win32.User32;
using static DotNetCampus.Installer.Boost.Win32.Kernel32;
using static DotNetCampus.Installer.Boost.Win32.Gdi32;
using static DotNetCampus.Installer.Boost.Win32.GdiPlus;
using static DotNetCampus.Installer.Boost.Win32.WindowExStyles;
using static DotNetCampus.Installer.Boost.Win32.WindowStyles;
using WindowsMessages = DotNetCampus.Installer.Boost.Win32.WM;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using SIZE = DotNetCampus.Installer.Boost.Win32.Size;

namespace DotNetCampus.Installer.Boost;

/// <summary>
/// 提供基于原生 Win32 窗口的启动图显示
/// </summary>
/// Copy From https://github.com/kkwpsv/SplashImage
public class SplashScreen
{
    private readonly string _splashFile;
    private readonly string _windowClass;
    private readonly string _windowName;
    private readonly bool _topmost;
    private int _imageWidth;
    private int _imageHeight;
    private bool _requestClose = false;

    /// <summary>
    /// 创建一个可以显示本地文件作为启动图的 <see cref="SplashScreen"/> 的新实例。
    /// 如果文件不存在，仅 Debug 下会抛出异常。
    /// 如果图片实际宽高与传入不一致，则会进行拉伸。
    /// </summary>
    /// <param name="file">图片文件的完全限定路径。</param>
    /// <param name="width">图片的像素宽度，无需考虑 DPI 问题。如果为null，则自动获取。</param>
    /// <param name="height">图片的像素高度，无需考虑 DPI 问题。如果为null，则自动获取。</param>
    /// <param name="windowTitle">启动图窗口的名称。</param>
    /// <param name="topmost">是否置顶显示。</param>
    public SplashScreen(FileInfo file, int? width = null, int? height = null, string? windowTitle = null,
        bool topmost = false)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (width.HasValue && width <= 0)
        {
            ThrowArgumentOutOfRangeException(nameof(width), "图片的宽度必须为非负值。");
        }

        if (height.HasValue && height <= 0)
        {
            ThrowArgumentOutOfRangeException(nameof(height), "图片的高度必须为非负值。");
        }

        if (!file.Exists)
        {
            ThrowFileNotFoundException(file);
        }

        _splashFile = file.FullName;
        _imageWidth = width ?? 0;
        _imageHeight = height ?? 0;
        _windowClass = windowTitle ?? "Splash Image";
        _windowName = windowTitle ?? "Splash Image";
        _topmost = topmost;

        static void ThrowArgumentOutOfRangeException(string paramName, string message)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }

        static void ThrowFileNotFoundException(FileInfo file)
        {
            throw new FileNotFoundException($"启动图文件不存在：{file.FullName}。", file.FullName);
        }
    }

    /// <summary>
    /// 标记当前是否正在显示启动图，0 表示尚未显示，1 表示已经显示。
    /// </summary>
    private volatile int _isRunning;

    /// <summary>
    /// 在后台线程显示启动图。此方法是线程安全的，多次调用会使后面的调用无效（因为已经显示出来了，不需要再次显示）。
    /// 如果在显示启动图的过程中发生任何异常，将仅 Debug 下抛出。
    /// </summary>
    public Task ShowAsync() => Task.Run(Show);

    /// <summary>
    /// 立即显示启动图。此方法是线程安全的，多次调用会使后面的调用无效（因为已经显示出来了，不需要再次显示）。显示过程将会卡住调用方线程，直到关闭才能返回
    /// </summary>
    public void Show()
    {
        var isRunning = Interlocked.CompareExchange(ref _isRunning, 1, 0);
        // 在以上方法调用之前，如果 _isRunning 为 0，则说明首次进入。
        try
        {
            if (isRunning is 0)
            {
                ShowCore();
            }
        }
        finally
        {
            _isRunning = 0;
        }
    }

    /// <summary>
    /// 关闭 SplashScreen 窗口。
    /// 即使在 <see cref="ShowAsync"/> 之前调用也是安全的。
    /// 调用了 <see cref="Close"/> 后，哪怕再次调用 <see cref="ShowAsync"/> 也不会显示。
    /// 在关闭窗口时，你可能会遇到主窗口被置于其他窗口的后面的问题，这是 Windows 的 Bug。
    /// 解决方法是在调用此 Close 方法前，Activate 一下主窗口。
    /// 详见：关闭模态窗口后，父窗口居然跑到了其他窗口的后面
    /// https://blog.walterlv.com/post/fix-owner-window-dropping-down-when-close-a-modal-child-window.html
    /// </summary>
    public void Close()
    {
        //发送WM.CLOSE消息，关闭窗口
        //这里判断了窗口是否初始化。如果没有初始化的话，将0作为参数传入，PostMessage会把消息发送给调用线程上的所有窗口，
        //但即使不做判断，发送了，由于没有指定窗口，没有窗口会响应这个WM.CLOSE消息，也没有太大影响。
        if (_window != IntPtr.Zero)
        {
            PostMessage(_window, (uint) Win32.WM.CLOSE, IntPtr.Zero, IntPtr.Zero);
        }
        //设置字段，并在消息循环中判断该字段实现关闭窗口，确保线程安全
        _requestClose = true;
    }

    [Conditional("DEBUG")]
    private void WriteLog(string message)
    {
        Console.WriteLine($"[SplashScreen] {message}");
    }

    #region 原生 Win32 实现

    /// <summary>
    /// 窗口句柄。此字段将在 <see cref="ShowCore"/> 构造了窗口后赋值并不再接受新值。
    /// </summary>
    private IntPtr _window;

    /// <summary>
    /// 窗口的外接矩形。此字段将在 <see cref="ShowCore"/> 构造了窗口后或者 DPI 改变后赋值并更新。
    /// 外接矩形的存储仅仅是为了计算得到 <see cref="_windowSize"/> 字段的值。
    /// </summary>
    private Win32.RECT _windowRectangle;

    /// <summary>
    /// 窗口的尺寸。此字段将在每次 <see cref="_windowSize"/> 更新后赋值。
    /// 此字段是为了 GDI+ 绘制位图时，参考的绘制尺寸。
    /// </summary>
    private SIZE _windowSize;

    /// <summary>
    /// 获取屏幕的设备上下文（Device Context）句柄。<see cref="UpdateLayeredWindow"/> 方法中需要的参数。
    /// 详见：https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-updatelayeredwindow。
    /// </summary>
    private readonly IntPtr _screenDC = GetDC(IntPtr.Zero);

    /// <summary>
    /// <see cref="_window"/> 的设备上下文（Device Context）句柄。
    /// 所有与绘制和渲染相关的 API 都将用到此句柄。
    /// </summary>
    private IntPtr _windowDC;

    /// <summary>
    /// 仅在内存中使用的设备上下文（Memory Device Context）。
    /// 它将被用来绘制位图，也可以用来设置绘制所需的各种参数。
    /// </summary>
    private IntPtr _memoryDC = IntPtr.Zero;

    /// <summary>
    /// GDI+ 启动图位图。
    /// </summary>
    private IntPtr _splashImage;

    /// <summary>
    /// GDI+ 位图画刷。
    /// </summary>
    private IntPtr _graphics;

    /// <summary>
    /// 启动图位图在内存中的指针。
    /// </summary>
    private IntPtr _memoryBitmap = IntPtr.Zero;

    /// <summary>
    /// GDI+ 启动时生成的一个 Token，需要在关闭 GDI+ （<see cref="Win32.GdiPlus.GdiplusShutdown(UIntPtr)"/>）的时候传入。
    /// </summary>
    private UIntPtr _gdipToken = UIntPtr.Zero;

    /// <summary>
    /// 用于产生消息，防止阻塞在 <see cref="GetMessage"/> 的Timer 
    /// </summary>
    private IntPtr _timer;

    /// <summary>
    /// 显示窗口并绘制一张位图的纯 Win32 实现。
    /// </summary>
    private void ShowCore()
    {
        try
        {
            // 在 Win32 里 hInstance 和 hModule 是相同的东西。这个 hInstance 是当年 win3.1 时代的产物
            var hInstance = GetModuleHandle(IntPtr.Zero);

            // C++ 中一个字段既可以是指针又可以是字符串，而 WindowClassEx 类型就是这样的。
            // 因此，我们使用 StringToIntPtrMarshaler 来封装 _windowClass 字符串，以便可以适应 C++ 的这种机制。
            // 所以，这里的 marshal 是一个字符串，同时也是一个指向此字符串的指针。
            // 但是，这就要求此实例必须被释放。
            using var marshal = new StringToIntPtrMarshaler(_windowClass);

            // 创建窗口：设置窗口类型属性。
            var wndclass = new WindowClassExPtr
            {
                Size = (uint) Marshal.SizeOf(typeof(WindowClassExPtr)),
                Styles = WindowClassStyles.CS_DBLCLKS,
                WindowProc = WindowProc,
                ClassExtraBytes = 0,
                WindowExtraBytes = 0,
                InstanceHandle = hInstance,
                IconHandle = LoadIcon(IntPtr.Zero, SystemIcon.IDI_APPLICATION),
                CursorHandle = LoadCursor(IntPtr.Zero, SystemCursor.IDC_ARROW),
                BackgroundBrushHandle = (IntPtr) SystemColor.COLOR_WINDOW,
                MenuName = IntPtr.Zero,
                ClassName = marshal.GetPtr(),
            };

            // 创建窗口：注册窗口类。
            if (RegisterClassEx(ref wndclass) != 0)
            {
                // 创建窗口：创建窗口。
                // WS_EX_LAYERED：只有 LayeredWindow 才支持不规则窗口（在这里是带透明像素的 png 位图）。
                // WS_OVERLAPPED：可以正确处理多显示器情况下，打开窗口的显示器位置。不要用WS_POPUP，否则始终显示在主显示器
                // 其他参数都是传入的默认值，无需关注。
                _window = CreateWindowEx(WS_EX_LAYERED | WS_EX_TOOLWINDOW, _windowClass, _windowName, WS_OVERLAPPED, (int) CreateWindowFlags.CW_USEDEFAULT, (int) CreateWindowFlags.CW_USEDEFAULT,
                    (int) CreateWindowFlags.CW_USEDEFAULT, (int) CreateWindowFlags.CW_USEDEFAULT, IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);

                if (_window != IntPtr.Zero)
                {
                    // 获取窗口的设备上下文。
                    _windowDC = GetDC(_window);

                    // 准备 GDI+ 启动时所需要的各种参数。
                    var startupInput = new GdiplusStartupInput
                    {
                        GdiplusVersion = 1,
                        DebugEventCallback = IntPtr.Zero,
                        SuppressBackgroundThread = false,
                        SuppressExternalCodecs = false,
                    };

                    // 启动 GDI+，然后为启动图（_splashImage）文件创建 GDI+ 位图。
                    // 这里无需处理位图的异常情况，包括：
                    //  - 文件不存在
                    //  - 文件无法被成功解析
                    // 因为 GDI+ 在这里会完全处理异常情况，并进入到后面的 else 代码中。
                    // 对用户的感知来说，就是启动图窗口中看不见位图。
                    if (GdiplusStartup(out _gdipToken, ref startupInput, out _) == GpStatus.Ok
                        && GdipLoadImageFromFile(_splashFile, out _splashImage) == GpStatus.Ok)
                    {
                        if (_imageWidth == 0)
                        {
                            if (GdipGetImageWidth(_splashImage, out var width) == GpStatus.Ok)
                            {
                                _imageWidth = (int) width;
                            }
                            else
                            {
                                _imageWidth = 1;
                            }
                        }

                        if (_imageHeight == 0)
                        {
                            if (GdipGetImageHeight(_splashImage, out var height) == GpStatus.Ok)
                            {
                                _imageHeight = (int) height;
                            }
                            else
                            {
                                _imageHeight = 1;
                            }
                        }

                        WriteLog($"ImageWidth={_imageWidth} ImageHeight={_imageHeight}");

                        // 设置窗口的位置和尺寸，使之居中于主屏屏幕显示。
                        SetPositionAndSize();

                        // 将窗口显示出来并置于正常状态（非最大化/最小化）。
                        ShowWindow(_window, Win32.ShowWindowCommands.SW_SHOWNORMAL);

                        // 全部创建完毕之后，将位图绘制到窗口上。
                        // 注意此时还没有启动消息循环，我们需要在窗口可见之前绘制完成。
                        DrawImage();

                        OnShowed();

                        //设置一个Timer，每200豪秒产生一个WM_TIMER消息，防止长时间不进入GetMessage
                        //这个Timer会在窗口销毁时被销毁
                        _timer = SetTimer(_window, IntPtr.Zero, 200, null);
                       
                        // 开启消息循环，然后处理消息。消息处理详见后面的 WindowProc 方法。
                        while (GetMessage(out var msg, IntPtr.Zero, 0, 0) != 0)
                        {
                            if (msg.Value == (uint)WM.CLOSE)
                            {
                                WriteLog($"收到 WM_CLOSE 消息");
                                DestroyWindow(_window);
                                break;
                            }

                            if (!_requestClose)
                            {
                                TranslateMessage(ref msg);
                                DispatchMessage(ref msg);
                            }
                            else
                            {
                                //如果请求了关闭窗口,销毁窗口
                                DestroyWindow(_window);
                            }
                        }

                        WriteLog($"循环退出");
                    }
                    else
                    {
                        // TODO: throw GDI+ Error
                    }
                }
                else
                {
                    // 如果窗口创建失败，则引发 Win32 异常。
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
            else
            {
                // 如果注册窗口类失败，则引发 Win32 异常。
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }
        finally
        {
            // 方法结束后，将释放所有的资源，包括窗口、屏幕、GDI+、位图、画刷等。
            ReleaseAllResource();
        }
    }

    /// <summary>
    /// 窗口的消息处理函数。
    /// </summary>
    /// <param name="hWnd">窗口句柄。</param>
    /// <param name="msg">消息。</param>
    /// <param name="wParam">Win32 中是 <see cref="UIntPtr"/> 类型，但 Windows Forms 中是 <see cref="IntPtr"/> 类型，这不重要。</param>
    /// <param name="lParam"></param>
    /// <returns></returns>
    private IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            switch ((WindowsMessages) msg)
            {
                case WindowsMessages.DESTROY:
                    // 当要求退出程序时（通常是主窗口已关闭），则关闭此窗口。
                    PostQuitMessage(0);
                    return IntPtr.Zero;
                case WindowsMessages.DPICHANGED:
                    // 当 DPI 改变后，将重新调整窗口位置并重新绘制位图。
                    SetPositionAndSize(wParam.ToInt32() >> 16);
                    DrawImage();
                    return IntPtr.Zero;
                case WindowsMessages.NCHITTEST:
                    //修改对标题栏的HitTest，防止用户拖拽隐形的标题栏，导致位置移动
                    var result = DefWindowProc(hWnd, msg, wParam, lParam);
                    return result == (IntPtr) Win32.HitTestResult.HTCAPTION ? (IntPtr) Win32.HitTestResult.HTNOWHERE : result;
                default:
                    // 创建窗口过程中有一些创建相关的消息需要处理，交给默认的处理函数处理。
                    return DefWindowProc(hWnd, msg, wParam, lParam);
            }
        }
        catch
        {
            // 这里捕获的任何异常都无法在 ShowCore 的消息循环中捕捉到，而是会被系统处理，这将出现托管代码无法捕获的异常。
            // 所以，请勿在此方法中泄漏任何异常，必须全部捕获。
            DestroyWindow(_window);
            return IntPtr.Zero;
        }
    }

    /// <summary>
    /// 使用 GDI+ 绘制位图。
    /// </summary>
    private void DrawImage()
    {
        // 在每次绘制之前，清除之前每次绘制时所使用的绘制设备上下文和位图。
        if (_memoryDC != IntPtr.Zero)
        {
            DeleteDC(_memoryDC);
        }
        if (_memoryBitmap != IntPtr.Zero)
        {
            DeleteObject(_memoryBitmap);
        }

        // 获取窗口上用于绘制的设备上下文。
        _memoryDC = CreateCompatibleDC(_windowDC);

        // 创建位图。
        _memoryBitmap = CreateCompatibleBitmap(_windowDC, _windowSize.Width, _windowSize.Height);

        // 将位图放到内存设备上下文中。
        SelectObject(_memoryDC, _memoryBitmap);

        // 创建设备画刷，绘制位图。
        if (GdipCreateFromHDC(_memoryDC, out _graphics) == GpStatus.Ok
            && GdipDrawImageRectI(_graphics, _splashImage, 0, 0, _windowSize.Width, _windowSize.Height) == GpStatus.Ok)
        {
            var ptSrc = new Win32.POINT
            {
                X = 0,
                Y = 0,
            };
            var ptDes = new Win32.POINT
            {
                X = _windowRectangle.Left,
                Y = _windowRectangle.Top,
            };
            var blendFunction = new Win32.BlendFunction
            {
                AlphaFormat = AlphaFormat.AC_SRC_ALPHA,
                BlendFlags = 0,
                BlendOp = (byte) AlphaFormat.AC_SRC_OVER,
                SourceConstantAlpha = 255,
            };

            // 更新 Layered Window 的位置、尺寸、形状、内容和透明度。
            if (UpdateLayeredWindow(_window, _screenDC, ref ptDes, ref _windowSize, _memoryDC, ref ptSrc, 0, ref blendFunction, (uint) LayeredWindowAttributeFlag.LWA_ALPHA))
            {
                return;
            }
            else
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }
    }

    /// <summary>
    /// 释放显示窗口过程中所使用到的全部资源。
    /// 一旦释放全部资源，窗口和位图将全部被回收，启动图即不可见。
    /// </summary>
    private void ReleaseAllResource()
    {
        if (_splashImage != null)
        {
            //下面的 GdiplusShutdown 不能保证图片的文件句柄被释放
            GdipDisposeImage(_splashImage);
        }
        if (_gdipToken != null)
        {
            GdiplusShutdown(_gdipToken);
        }
        DeleteObject(_memoryBitmap);
        DeleteDC(_memoryDC);
        ReleaseDC(_window, _windowDC);
        ReleaseDC(IntPtr.Zero, _screenDC);
        KillTimer(_window, _timer);
    }


    /// <summary>
    /// 根据指定的 DPI 值设置窗口的位置和尺寸。
    /// </summary>
    /// <param name="dpi">窗口的 DPI 值。</param>
    private void SetPositionAndSize(int? dpi = null)
    {
        // 获取按像素计算的主屏幕的像素尺寸，以及左上位置坐标。
        var screenLeft = 0;
        var screenTop = 0;
        var screenWidth = GetSystemMetrics(DotNetCampus.Installer.Boost.Win32.SystemMetrics.SM_CXSCREEN);
        var screenHeight = GetSystemMetrics(SystemMetrics.SM_CYSCREEN);

        //尝试获取显示器的信息
        var monitor = MonitorFromWindow(_window, Win32.MonitorFlag.MONITOR_DEFAULTTONULL);
        if (monitor != 0)
        {
            var info = new Win32.MonitorInfo();
            info.Size = (uint) Marshal.SizeOf(info);
            if (GetMonitorInfo(monitor, ref info))
            {
                screenLeft = info.MonitorRect.Left;
                screenTop = info.MonitorRect.Top;
                screenWidth = info.MonitorRect.Right - info.MonitorRect.Left;
                screenHeight = info.MonitorRect.Bottom - info.MonitorRect.Top;

                WriteLog($"MonitorRect LT={info.MonitorRect.Left},{info.MonitorRect.Top} RB={info.MonitorRect.Right},{info.MonitorRect.Bottom}");
            }
        }

        // 获取当前窗口的 DPI，用于更新窗口的位置和缩放位图。
        if (dpi == null)
        {
            var osVersion = Environment.OSVersion.Version;
            if (osVersion > new Version(10, 0, 1607))
            {
                //使用1607新增的API，获取到显示器的DPI
                dpi = (int) GetDpiForWindow(_window);
            }
            else
            {
                dpi = GetDeviceCaps(_windowDC, Win32.DeviceCapIndexes.LOGPIXELSX);
            }
        }

        // 根据用户设置的位图宽高和 DPI 值来计算窗口的大小。
        var windowWidth = _imageWidth * dpi.Value / 96;
        var windowHeight = _imageHeight * dpi.Value / 96;

        // 根据用户设置的 TopMost 来计算窗口位置设置的 Z 顺序。
        Win32.HwndZOrder zOrder = _topmost ? HwndZOrder.HWND_TOPMOST : HwndZOrder.HWND_NOTOPMOST;

        // 设置窗口的位置。
        var x = (screenWidth - windowWidth) / 2 + screenLeft;
        var y = (screenHeight - windowHeight) / 2 + screenTop;

        WriteLog($"screen LT={screenLeft},{screenTop};WH={screenWidth},{screenHeight} DPI={dpi} WindowWH={windowWidth},{windowHeight} WindowXY={x},{y}");

        SetWindowPos(_window, (IntPtr) zOrder, x, y, windowWidth, windowHeight, 0);

        // 获取窗口设置完位置之后的实际位置。
        GetWindowRect(_window, out _windowRectangle);

        // 计算窗口的大小。这将被 GDI+ 绘制部分用来参考绘制尺寸使用。
        _windowSize = new SIZE
        {
            Width = _windowRectangle.Right - _windowRectangle.Left,
            Height = _windowRectangle.Bottom - _windowRectangle.Top
        };
    }

    private void OnShowed()
    {
        Showed?.Invoke(this, new SplashScreenShowedEventArgs(_window));
    }

    public event EventHandler<SplashScreenShowedEventArgs>? Showed;

    #endregion
}

public class SplashScreenShowedEventArgs(IntPtr windowHandler) : EventArgs
{
    public IntPtr SplashScreenWindowHandler => windowHandler;
}

/// <summary>
/// <para>以 Unicode 编码将字符串包装到非托管内存。</para>
/// <para>此类型因为包含非托管资源，所以必须可被释放。</para>
/// </summary>
file class StringToIntPtrMarshaler : IDisposable
{
    /// <summary>
    /// 字符串在非托管内存中的指针。
    /// </summary>
    private readonly IntPtr _ptr;

    /// <summary>
    /// 以 Unicode 编码将字符串包装到非托管内存。
    /// </summary>
    /// <param name="string">托管字符串。</param>
    public StringToIntPtrMarshaler(string @string) => _ptr = Marshal.StringToHGlobalUni(@string);

    /// <summary>
    /// 获取字符串的非托管内存地址。
    /// </summary>
    public IntPtr GetPtr() => _ptr;

    /// <summary>
    /// 用于释放此非托管字符串。
    /// </summary>
    private bool _isDisposed = false;

    /// <summary>
    /// 释放此非托管字符串。
    /// </summary>
    ~StringToIntPtrMarshaler() => Dispose(false);

    /// <summary>
    /// 释放此非托管字符串。
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 释放此非托管字符串。
    /// </summary>
    private void Dispose(bool _)
    {
        if (!_isDisposed)
        {
            Marshal.FreeHGlobal(_ptr);
        }
        _isDisposed = true;
    }
}


public static partial class Win32
{
    /// <summary>
    /// 在 Win32 函数使用的 Size 类，使用 int 表示数据
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public partial struct Size : IEquatable<Size>
    {

        public Size(int width, int height)
        {
            this.Width = width;
            this.Height = height;
        }

        public bool Equals(Size other)
        {
            return (this.Width == other.Width) && (this.Height == other.Height);
        }

        public override bool Equals(object? obj)
        {
            return obj is Size && this.Equals((Size) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            { return ((int) this.Width * 397) ^ (int) this.Height; }
        }

        public int Width;
        public int Height;

        public static bool operator ==(Size left, Size right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Size left, Size right)
        {
            return !(left == right);
        }

        public void Offset(int width, int height)
        {
            Width += width;
            Height += height;
        }

        public void Set(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public override string ToString()
        {
            var culture = CultureInfo.CurrentCulture;
            return $"{{ Width = {Width.ToString(culture)}, Height = {Height.ToString(culture)} }}";
        }

        public bool IsEmpty => this.Width == 0 && this.Height == 0;
    }

    public static class Kernel32
    {
        public const string LibraryName = "kernel32";


        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern IntPtr GetModuleHandle(IntPtr modulePtr);
    }
}
