# WPF 窗口从 X8 单向升级到 A8 的上层业务实现

## 结论

如果业务只要求窗口默认以不透明模式高性能运行，并允许在第一次需要透明后永久停留在带 Alpha 的渲染模式，那么不需要实现 A8→X8 的 UCE target 重建。

可以采用以下单向生命周期：

```text
窗口启动：D3DFMT_X8R8G8B8 + 无 WS_EX_LAYERED + 普通不透明合成
首次启用透明：透明 clear color 触发 NeedDestinationAlpha
升级完成：D3DFMT_A8R8G8B8 + WS_EX_LAYERED + 每像素 Alpha 内容
此后：允许改变透明内容，但不再尝试恢复 X8
```

上层业务需要遵守的核心顺序是：

```text
保持 AllowsTransparency=false
等待 HWND 创建
安装 WM_STYLECHANGING hook
准备透明业务内容
安装完整 WindowChrome
等待一次渲染，使透明 clear color 触发 X8→A8
最后添加 WS_EX_LAYERED
```

这条路线的优点是：

- 默认阶段仍使用不带目标 Alpha 的 X8 渲染目标；
- 不创建新的窗口，HWND 保持不变；
- 不使用 `AllowsTransparency=true`；
- 不进入 `ApplicationManagedLayer + UpdateLayeredWindow`；
- 不需要反射 `MediaContext`、channel 或 `HwndTarget` 内部成员；
- 不需要处理 A8→X8 的复杂资源重建和异常恢复。

代价是：

> **同一个 HWND 一旦升级为 A8，本次窗口生命周期内就不再回到 X8。即使后来把所有内容绘制为不透明，也只是“A8 目标输出不透明像素”，不是恢复为 X8。**

本文基于以下两份分析：

- [`wpf-dynamic-a8-x8-render-target-switching.md`](wpf-dynamic-a8-x8-render-target-switching.md)
- [`windowchrome-layered-rendering-analysis.md`](windowchrome-layered-rendering-analysis.md)

---

## 1. 适用场景与能力边界

这套实现适合以下业务：

- 窗口大部分时间不透明，只有某个功能开启后才需要透明；
- 首次透明可能发生得很晚，因此希望此前一直保留 X8 的稳定运行性能；
- 开启透明后，业务可以接受该窗口在剩余生命周期内保持 A8；
- 必须保留原 HWND，例如外部模块已经持有窗口句柄；
- 不希望使用 `AllowsTransparency=true` 的软件呈现倾向与 `UpdateLayeredWindow` 路径。

不适合以下业务：

- 需要频繁在 X8 与 A8 之间往返；
- 开启透明后还必须恢复真正的 X8 render target；
- 要求完全依赖 WPF 公共透明窗口契约；
- 无法针对支持的 Windows、显卡驱动和渲染层级做专项验证。

需要特别注意：强制保留 `WS_EX_LAYERED`，同时让 WPF/MIL 继续把窗口作为 `NotLayered + Opaque` 管理，是 Win32 与 WPF 的互操作技巧，不是 WPF 公开保证的呈现模式。产品必须针对自己的运行环境验证。

---

## 2. 为什么单向升级不需要反射

普通 WPF 顶层窗口通常从不需要目标 Alpha 的状态开始。硬件路径会选择：

```text
D3DFMT_X8R8G8B8
```

安装完整 glass 的 `WindowChrome` 后，`WindowChromeWorker` 会：

1. 把客户区扩展到整个窗口；
2. 调用 `DwmExtendFrameIntoClientArea` 扩展完整 DWM frame；
3. 把 `HwndSource.CompositionTarget.BackgroundColor` 改为透明。

透明 clear color 到达 WpfGfx 后，会给现有 `CSlaveHWndRenderTarget` 增加 `NeedDestinationAlpha`。WPF 随后释放底层 render target，并按新标志创建：

```text
D3DFMT_A8R8G8B8
```

这个过程本来就是 WPF 支持的单向升级路径，因此 X8→A8 不需要注销和重新注册 `HwndTarget`。

问题只出现在反向切换：恢复不透明 clear color 不会清除已经记住的 `NeedDestinationAlpha`。本文接受这个限制，所以完全不需要反射 WPF 内部成员。

---

## 3. 业务状态模型

即使只做单向升级，也不应让多个业务模块分别修改 `WindowChrome` 和 HWND 样式。建议集中到一个控制器，并使用以下状态：

```csharp
internal enum AlphaUpgradeState
{
    OpaqueX8,
    Upgrading,
    AlphaModeApplied,
    Faulted,
    Closed,
}
```

状态约束如下：

- `OpaqueX8`：允许调用一次升级；
- `Upgrading`：拒绝并发升级；
- `AlphaModeApplied`：透明内容、完整 `WindowChrome` 和 `WS_EX_LAYERED` 已按顺序应用；重复调用直接返回；
- `Faulted`：不自动重试，避免在 HWND、WPF 和业务内容状态不一致时继续操作；
- `Closed`：窗口关闭后永久停止。

控制器只公开 `UpgradeToTransparentAsync`，不要提供名为 `DisableTransparency`、`RestoreOpaque` 或 `DowngradeToX8` 的方法。业务如果暂时不想显示透明区域，只需要把内容重新绘制为不透明；这不会也不应该被描述为恢复 X8。

---

## 4. 窗口与 XAML 的准备

### 4.1 不要设置 `AllowsTransparency=true`

窗口必须保持：

```xml
<Window
    ...
    AllowsTransparency="False">
</Window>
```

`AllowsTransparency=true` 会在 HWND 创建阶段设置 `HwndTarget.UsesPerPixelOpacity=true`，进入 `ApplicationManagedLayer + UpdateLayeredWindow`。这不是本文要使用的路径。

也不建议在 XAML 中预先安装完整 `WindowChrome`。如果窗口一创建就拥有透明 clear color，WPF 可能很早就从 X8 升级到 A8，失去“默认阶段使用 X8”的意义。

### 4.2 默认内容必须完全不透明

升级前，窗口背景、模板和根元素应输出不透明像素。例如：

```xml
<Window
    x:Class="Demo.MainWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    AllowsTransparency="False"
    Background="#FF202020">
    <Grid x:Name="RootLayout" Background="#FF202020">
        <!-- 业务内容 -->
    </Grid>
</Window>
```

这里使用 Alpha 为 `FF` 的颜色，是为了确保默认画面不依赖透明合成。

### 4.3 升级时再准备透明像素

业务需要透明时，应先把需要透出的区域改为 `Transparent` 或具有小于 255 Alpha 的画刷。例如可以把根背景改为透明，再由内部卡片绘制不透明区域：

```xml
<Grid x:Name="RootLayout" Background="Transparent">
    <Border
        Margin="24"
        Background="#E6202020"
        CornerRadius="16">
        <!-- 业务内容 -->
    </Border>
</Grid>
```

如果 `Window.Background`、窗口模板或根元素仍使用不透明画刷，透明 clear color 会被覆盖，最终视觉上仍不会透出桌面内容。

---

## 5. 控制器实现

下面的控制器负责：

- 在 `SourceInitialized` 之后取得并缓存 HWND；
- 安装 `WM_STYLECHANGING` hook；
- 安装完整 glass 的 `WindowChrome`；
- 等待 WPF 完成一次渲染；
- 添加并验证 `WS_EX_LAYERED`；
- 防止并发和重复升级；
- 在窗口关闭时移除 hook。

代码只使用 .NET Framework 4.7.2 与现代 .NET WPF 都具备的 API。

这里的 `AlphaModeApplied` 只表示上层可观察的配置已经完成：托管端 clear color 为透明、完整 `WindowChrome` 已安装、`WS_EX_LAYERED` 已存在。它不表示公开 API 已严格确认渲染线程完成 A8 target 重建；实际格式仍需按第 11.2 节验证。

```csharp
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using System.Windows.Threading;

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

    private HwndSource _source;
    private IntPtr _hwnd;
    private bool _keepLayeredStyle;
    private bool _hookInstalled;
    private bool _disposed;

    public OneWayAlphaWindowController(Window window)
    {
        if (window == null)
        {
            throw new ArgumentNullException(nameof(window));
        }

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

    public AlphaUpgradeState State { get; private set; }

    public async Task UpgradeToTransparentAsync(Action prepareTransparentContent)
    {
        if (prepareTransparentContent == null)
        {
            throw new ArgumentNullException(nameof(prepareTransparentContent));
        }

        ThrowIfDisposed();
        _window.Dispatcher.VerifyAccess();

        if (State == AlphaUpgradeState.AlphaModeApplied)
        {
            return;
        }

        if (State != AlphaUpgradeState.OpaqueX8)
        {
            throw new InvalidOperationException(
                $"Cannot upgrade the window while its state is {State}.");
        }

        EnsureHwndInitialized();
        State = AlphaUpgradeState.Upgrading;

        try
        {
            InstallHook();

            prepareTransparentContent();
            InstallFullGlassWindowChrome();

            await WaitForRenderAsync();
            ThrowIfDisposed();
            VerifyHwnd();
            VerifyTransparentClearColor();

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

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _keepLayeredStyle = false;

        _window.SourceInitialized -= OnSourceInitialized;
        _window.Closed -= OnWindowClosed;
        RemoveHook();

        State = AlphaUpgradeState.Closed;
    }

    private void OnSourceInitialized(object sender, EventArgs e)
    {
        InitializeHwnd();
    }

    private void OnWindowClosed(object sender, EventArgs e)
    {
        Dispose();
    }

    private void InitializeHwnd()
    {
        _window.Dispatcher.VerifyAccess();

        IntPtr hwnd = new WindowInteropHelper(_window).Handle;
        if (hwnd == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "The Window does not have an HWND yet.");
        }

        HwndSource source = HwndSource.FromHwnd(hwnd) as HwndSource;
        if (source == null)
        {
            throw new InvalidOperationException(
                "Cannot obtain the HwndSource for the Window.");
        }

        if (_hwnd != IntPtr.Zero && _hwnd != hwnd)
        {
            throw new InvalidOperationException(
                "The Window HWND changed during its lifetime.");
        }

        _hwnd = hwnd;
        _source = source;
    }

    private void EnsureHwndInitialized()
    {
        if (_hwnd == IntPtr.Zero || _source == null)
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

        _source.AddHook(_windowMessageHook);
        _hookInstalled = true;
    }

    private void RemoveHook()
    {
        if (!_hookInstalled || _source == null)
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
            StyleStruct style =
                (StyleStruct)Marshal.PtrToStructure(
                    lParam,
                    typeof(StyleStruct));

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
                GlassFrameThickness =
                    WindowChrome.GlassFrameCompleteThickness,
                CaptionHeight = 0,
                CornerRadius = new CornerRadius(),
                ResizeBorderThickness = new Thickness(),
                UseAeroCaptionButtons = false,
            });
    }

    private async Task WaitForRenderAsync()
    {
        await _window.Dispatcher.InvokeAsync(
            () => { },
            DispatcherPriority.Render);

        _window.InvalidateVisual();
        await _window.Dispatcher.InvokeAsync(
            () => { },
            DispatcherPriority.ContextIdle);
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
            _source == null ||
            _source.IsDisposed ||
            !IsWindow(_hwnd))
        {
            throw new InvalidOperationException(
                "The Window HWND is no longer valid.");
        }
    }

    private void VerifyTransparentClearColor()
    {
        HwndTarget target = _source.CompositionTarget;
        if (target == null || target.BackgroundColor.A != 0)
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
            throw new InvalidOperationException(
                "WPF or the operating system removed WS_EX_LAYERED.");
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(
                nameof(OneWayAlphaWindowController));
        }
    }

    private static IntPtr GetWindowLongPtr(IntPtr hwnd, int index)
    {
        return IntPtr.Size == 8
            ? GetWindowLongPtr64(hwnd, index)
            : new IntPtr(GetWindowLong32(hwnd, index));
    }

    private static IntPtr SetWindowLongPtr(
        IntPtr hwnd,
        int index,
        IntPtr value)
    {
        return IntPtr.Size == 8
            ? SetWindowLongPtr64(hwnd, index, value)
            : new IntPtr(
                SetWindowLong32(hwnd, index, value.ToInt32()));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct StyleStruct
    {
        public uint StyleOld;
        public uint StyleNew;
    }

    [DllImport("kernel32.dll")]
    private static extern void SetLastError(uint errorCode);

    [DllImport(
        "user32.dll",
        EntryPoint = "GetWindowLong",
        SetLastError = true)]
    private static extern int GetWindowLong32(IntPtr hwnd, int index);

    [DllImport(
        "user32.dll",
        EntryPoint = "GetWindowLongPtr",
        SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr64(
        IntPtr hwnd,
        int index);

    [DllImport(
        "user32.dll",
        EntryPoint = "SetWindowLong",
        SetLastError = true)]
    private static extern int SetWindowLong32(
        IntPtr hwnd,
        int index,
        int value);

    [DllImport(
        "user32.dll",
        EntryPoint = "SetWindowLongPtr",
        SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr64(
        IntPtr hwnd,
        int index,
        IntPtr value);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindow(IntPtr hwnd);
}
```

---

## 6. 在窗口业务中接入

窗口创建控制器，并在业务首次需要透明时调用一次：

```csharp
public partial class MainWindow : Window
{
    private readonly OneWayAlphaWindowController _alphaController;

    public MainWindow()
    {
        InitializeComponent();
        _alphaController = new OneWayAlphaWindowController(this);
    }

    private async void EnableTransparentModeButtonClick(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            await _alphaController.UpgradeToTransparentAsync(
                PrepareTransparentContent);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                exception.Message,
                "无法启用透明模式",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void PrepareTransparentContent()
    {
        Background = Brushes.Transparent;
        RootLayout.Background = Brushes.Transparent;
    }
}
```

`prepareTransparentContent` 会在安装 `WindowChrome` 之前同步执行。它应该一次性完成进入透明模式所需的视觉树修改，避免在已经添加 `WS_EX_LAYERED` 后才逐步撤销不透明背景，从而减少切换瞬间的黑帧或错误画面。

事件处理器必须捕获升级异常。产品代码还应记录完整异常和失败阶段；失败后不应直接自动重试。

如果项目采用 MVVM，ViewModel 只表达“业务已经请求透明模式”。实际的 `WindowChrome`、HWND 和消息 hook 操作仍应放在 View 层服务中，因为这些操作直接依赖 `Window` 和 Win32 生命周期。可以由附加行为、窗口服务或窗口自身代码转发调用，不应把 `HwndSource` 放进 ViewModel。

---

## 7. 实现中的关键细节

### 7.1 hook 必须先于 `SetWindowLongPtr`

`SetWindowLongPtr` 会同步触发 `WM_STYLECHANGING`。普通 WPF 窗口的内部 `HwndTarget` 会尝试清除外部添加的 `WS_EX_LAYERED`。

运行时通过 `HwndSource.AddHook` 添加的公共 hook 位于内部 hook 之前。控制器会修改 `STYLESTRUCT.styleNew`，重新加入 `WS_EX_LAYERED`，并设置 `handled=true`，阻止内部 hook 再次清位。

因此顺序必须是：

```text
AddHook
准备透明内容并安装完整 WindowChrome
等待渲染边界
_keepLayeredStyle = true
SetWindowLongPtr(... WS_EX_LAYERED ...)
```

不能先添加样式，再安装 hook。也不需要在准备 `WindowChrome` 时提前开启样式保留状态；只要在最终 `SetWindowLongPtr` 之前开启即可，避免其他扩展样式变化意外提前带入 `WS_EX_LAYERED`。

### 7.2 先建立 A8，再添加 `WS_EX_LAYERED`

完整 `WindowChrome` 会把 clear color 改为透明，但这个变化需要经过 WPF 的 channel 到达渲染线程。若立即添加 `WS_EX_LAYERED`，可能出现窗口已经进入新的样式状态，而 A8 target 或透明内容尚未准备好的短暂窗口。

示例先安装 `WindowChrome`，再等待 Dispatcher 的 Render 阶段和 `ContextIdle` 阶段，最后才添加样式位。这里不直接等待 `CompositionTarget.Rendering`，因为窗口隐藏、最小化或没有活动呈现时，该事件可能长期不触发。

这只是使用公开 API 建立的渲染边界，不能像 WPF 内部 `MediaContext.CompleteRender()` 那样提供严格的 channel 同步保证。若业务对切换瞬间有严格要求，可以：

- 在升级期间显示一帧完全不透明的安全内容；
- 在非交互时段执行升级；
- 使用自编译 WPF 暴露受支持的内部同步入口；
- 在目标运行环境中测量是否存在闪烁或未准备完成的帧。

控制器还会检查 `HwndTarget.BackgroundColor` 的 Alpha 是否为 0。该检查可以发现 DWM composition 不可用、完整 frame 未建立或 `WindowChrome` 回退为不透明 clear color 的情况，但仍只能证明托管端状态，不能证明渲染线程已经创建 A8。

### 7.3 为什么 hook 在升级后仍然保留

WPF 后续可能在 resize、DPI 变化或窗口状态更新时再次调用 `UpdateWindowSettings`，并尝试移除它不认可的 `WS_EX_LAYERED`。

因此进入 `AlphaModeApplied` 后，控制器仍保持：

```text
_keepLayeredStyle = true
hook 已安装
```

只有窗口关闭时才移除 hook。

### 7.4 不要调用 `SetLayeredWindowAttributes`

本文透明度来自 WPF 内容中每个像素自己的 Alpha。不要调用：

```text
SetLayeredWindowAttributes(..., LWA_ALPHA)
```

该 API 设置的是整窗常量 Alpha，属于另一套 layered-window 能力。它既不能替代 A8 render target，也不是本文单向升级流程的一部分。

### 7.5 不要反射修改 `UsesPerPixelOpacity`

必须始终保持：

```text
HwndTarget.UsesPerPixelOpacity = false
```

将它改为 `true` 会让 WPF 进入 `ApplicationManagedLayer + UpdateLayeredWindow`，改变本文希望保留的呈现路径。绕过公开 API 去调用 internal setter，并不会获得一种更高性能的透明模式。

---

## 8. 关于示例代码兼容性的说明

### 8.1 `STYLESTRUCT` 是两个 32 位样式值

Win32 的 `STYLESTRUCT` 定义包含两个 `DWORD`：

```text
styleOld
styleNew
```

窗口 style 本身是 32 位值，不会因为进程是 64 位就扩展为 64 位。结构必须严格映射为：

```csharp
[StructLayout(LayoutKind.Sequential)]
private struct StyleStruct
{
    public uint StyleOld;
    public uint StyleNew;
}
```

对应的 hook 写成：

```csharp
style.StyleNew |= WsExLayered;
```

### 8.2 `GetWindowLongPtr` 的零返回值

`GetWindowLongPtr` 和 `SetWindowLongPtr` 返回零不一定代表失败，因此示例在调用前把 Win32 last error 清零，并在返回零时检查 `Marshal.GetLastWin32Error()`。

### 8.3 UI 线程要求

以下操作都必须在窗口所属 Dispatcher 线程执行：

- 访问或修改视觉树；
- 安装和移除 `HwndSource` hook；
- 安装 `WindowChrome`；
- 修改窗口扩展样式；
- 调用 `UpgradeToTransparentAsync`。

控制器通过 `Dispatcher.VerifyAccess()` 尽早拒绝错误线程调用。

---

## 9. 更稳妥的产品化调整

前面的示例用于展示完整调用顺序。产品代码建议继续增加以下约束。

### 9.1 明确窗口关闭与升级竞争

示例会在异步等待返回后检查控制器、`HwndSource` 和 HWND 是否仍然有效。产品实现还应使用窗口生命周期取消标记，让等待尽早结束，并在以下位置检查：

- 安装 `WindowChrome` 前；
- 等待渲染后；
- 调用 `SetWindowLongPtr` 前；
- 验证样式前。

对于 .NET Framework 4.7.2，可以使用 `CancellationTokenSource`；窗口关闭时取消等待，并确保取消后不会继续访问 HWND。

### 9.2 记录失败阶段

如果升级失败，至少记录：

- 当前状态；
- HWND；
- Windows 版本；
- PresentationCore、PresentationFramework 版本；
- 当前 `GWL_EXSTYLE`；
- `RenderCapability.Tier`；
- 失败发生在准备内容、安装 `WindowChrome`、等待渲染、设置样式还是验证样式阶段。

不要在 `Faulted` 后盲目重试，因为第一次尝试可能已经让 target 升级为 A8，只是后续样式设置失败。此时状态已经不能再按初始 X8 假设处理。

### 9.3 让透明请求具备幂等性

多个业务模块可能同时请求透明。推荐由窗口级服务缓存同一个升级任务：

```text
第一次请求创建升级任务
后续请求等待同一个任务
成功后全部返回
失败后统一进入 Faulted
```

不要让每个调用方各自安装 `WindowChrome` 或修改 HWND 样式。

### 9.4 保留不透明安全帧

如果切换期间不能出现桌面穿透，可以先显示完全不透明的过渡层：

```text
显示不透明过渡层
准备透明视觉树
安装完整 WindowChrome
等待渲染
添加 WS_EX_LAYERED
下一帧移除过渡层
```

过渡层本身必须在移除前输出完全不透明像素。这样即使 target 已经升级到 A8，用户也不会在业务内容准备完成前看到透明区域。

---

## 10. 升级后的业务行为

进入 `AlphaModeApplied` 后，可以继续改变内容 Alpha：

- 在透明与半透明画刷之间切换；
- 暂时把所有内容绘制为完全不透明；
- 播放透明动画；
- 改变透明区域形状。

但是这些变化都发生在已经带 Alpha 的目标上。

如果业务后来“不再需要透明”，有两个选择：

1. **仅恢复视觉不透明：**把背景和内容改为 Alpha=255，但保持 `WindowChrome`、hook 和 `WS_EX_LAYERED`。实现简单，但底层仍是 A8。
2. **销毁并重新创建窗口：**创建新的默认不透明窗口，并迁移业务状态。新 HWND 可以重新从 X8 开始，但外部句柄引用会变化。

除非实现动态切换文档中的 UCE target 重建方案，否则不要移除 `WindowChrome` 和 `WS_EX_LAYERED` 后宣称已经恢复 X8。

---

## 11. 验证清单

### 11.1 功能验证

至少验证：

- 窗口启动时没有 `WS_EX_LAYERED`；
- 首次升级前窗口内容完全不透明；
- 升级后 `WS_EX_LAYERED` 仍然存在；
- `HwndSource.UsesPerPixelOpacity` 仍为 `false`；
- 透明区域能够正确透出；
- 不透明区域没有意外变淡；
- 重复调用升级不会重复安装 hook 或 `WindowChrome`；
- resize、最大化、最小化和恢复后样式位仍存在；
- DPI 和显示器切换后透明效果仍正确；
- 窗口关闭时不会继续访问 HWND。

### 11.2 渲染目标验证

上层公开 API 不能直接读取实际 D3D9 target format。需要确认 X8→A8 时，可以使用以下方式：

- 在自编译 WPF 中观察 `CSlaveHWndRenderTarget::m_UCETargetFlags`；
- 确认 `NeedDestinationAlpha` 从无变为有；
- 在 `ChooseTargetFormat` 观察输出从 `D3DFMT_X8R8G8B8` 变为 `D3DFMT_A8R8G8B8`；
- 确认底层 render target 因 flags 改变被释放并重建；
- 使用图形诊断工具确认实际目标格式。

因此，`UpgradeToTransparentAsync` 成功不能单独作为“A8 已被严格验证”的证据。它只说明上层配置和 HWND 样式已成功应用；若产品必须以实际 target format 作为成功条件，需要使用自编译 WPF 的同步与诊断入口。

### 11.3 性能验证

应分别采样：

```text
阶段 A：窗口启动后、尚未请求透明的稳定 X8 阶段
阶段 B：首次升级后的稳定 A8 阶段
阶段 C：X8→A8 的一次性切换过程
```

建议记录：

- 应用进程 GPU 使用率；
- DWM GPU 时间；
- present 次数和帧率；
- 4K 大窗口下的显存带宽压力；
- 静态窗口与持续动画窗口；
- 切换耗时、UI stall、黑帧和闪烁；
- 硬件渲染与软件回退；
- 锁屏、远程桌面和显示设备重建后的行为。

业务收益应来自“透明功能启用前保持 X8”，而不是期望升级后的 A8 仍具有 X8 的合成成本。

---

## 12. 最终实施顺序

窗口初始化阶段：

```text
AllowsTransparency=false
不安装完整 WindowChrome
不添加 WS_EX_LAYERED
使用完全不透明的窗口背景和内容
保持默认 X8 render target
```

首次开启透明：

```text
确认状态为 OpaqueX8
确认 HWND 仍有效
安装 WM_STYLECHANGING hook
显示不透明安全帧（可选）
准备透明业务内容
安装完整 WindowChrome
确认托管端 clear color 已变为透明
等待 Dispatcher 的公开渲染边界
开启 hook 的 WS_EX_LAYERED 保留状态
添加 WS_EX_LAYERED
确认 WS_EX_LAYERED 未被 WPF 清除
移除不透明安全帧（可选）
进入 AlphaModeApplied
```

升级完成后：

```text
保留 WindowChrome
保留 WM_STYLECHANGING hook
保留 WS_EX_LAYERED
允许业务继续改变像素 Alpha
不提供 A8→X8 接口
窗口关闭时移除 hook 并停止控制器
```

这套单向升级方案把复杂度限制在 X8→A8：窗口在真正需要透明之前保持默认 X8；首次透明请求通过 WPF 已有的透明 clear-color 路径请求升级为 A8；此后接受目标不可逆，并避免反射、channel 操作和 UCE target 重建。
