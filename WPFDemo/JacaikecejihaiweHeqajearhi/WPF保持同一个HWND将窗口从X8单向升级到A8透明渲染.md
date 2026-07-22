# WPF 保持同一个 HWND 将窗口从 X8 单向升级到 A8 透明渲染

本文将告诉大家如何让一个普通不透明的 WPF 窗口先使用 X8 渲染目标运行，在业务第一次需要透明时保持原 HWND 不变，单向升级到带 Alpha 的 A8 渲染模式。

<!--more-->
<!-- 发布 -->
<!-- 博客 -->

本文内容由人类主导 AI 辅助编写

## 背景

`X8` 和 `A8` 对应 WPF 渲染目标常见的两种颜色格式：

- `X8R8G8B8`（通常称 **X8**）：每个像素仅保留 RGB，不保留可用于叠加的 Alpha；
- `A8R8G8B8`（通常称 **A8**）：每个像素保留 Alpha 通道（ARGB）。

窗口内容完全不透明时，WPF 更倾向于使用 X8；需要每像素透明/半透明时才需要走 A8。

在 WPF 里面实现透明窗口，最容易想到的是设置 `AllowsTransparency="True"`。但是这个属性不是运行过程中随意切换的普通视觉属性，它会影响 WPF 创建窗口和选择呈现路径的方式。

`AllowsTransparency=True` 还会带来明显的性能损耗与额外路径开销。更详细的底层原因可参阅： [WPF 从最底层源代码了解 AllowsTransparency 性能差的原因](https://blog.lindexi.com/post/WPF-%E4%BB%8E%E6%9C%80%E5%BA%95%E5%B1%82%E6%BA%90%E4%BB%A3%E7%A0%81%E4%BA%86%E8%A7%A3-AllowsTransparency-%E6%80%A7%E8%83%BD%E5%B7%AE%E7%9A%84%E5%8E%9F%E5%9B%A0.html)

如果从窗口启动开始就设置 `AllowsTransparency="True"`，WPF 会让窗口进入应用管理的分层窗口呈现路径。对于只在某个功能开启之后才需要透明的窗口来说，这意味着窗口在绝大部分不需要透明的时间里，也要提前承担透明窗口路径的成本。

我这里的需求稍微特别一些：

- 窗口默认完全不透明，希望先按普通窗口运行；
- 透明功能可能很晚才会开启；
- 外部模块已经持有 HWND，不能通过关闭旧窗口、创建新窗口的方式切换；
- 第一次开启透明之后，可以接受本次窗口生命周期内不再恢复真正的 X8；
- 不希望主动选择或依赖 `AllowsTransparency=true` 所对应的应用管理型分层窗口呈现路径。

这一路径的核心收益是启动阶段保持普通不透明窗口，从而先减少 DWM Alpha 合成压力与持续渲染开销。

基于这些约束，可以把问题简化为只处理一次 X8 到 A8 的升级，而不处理更复杂的 A8 到 X8 降级。

结合 WPF 源码行为，当前场景只有 X8 → A8 的单向升级路线是可复用的，所以不得已不做 A8 → X8 回退。

最终生命周期如下：

```text
窗口启动：X8 + 普通不透明窗口
首次请求透明：安装完整 WindowChrome，触发目标 Alpha 需求
升级完成：A8 + WS_EX_LAYERED + 每像素 Alpha 内容
后续运行：允许改变内容透明度，但不再恢复 X8
```

本文对应的完整演示项目放在文末，大家可以自行拉取代码运行。项目使用 .NET 6 WPF 编写。

## 先说能力边界

本文基于 WPF 已有的透明 clear color 与重建行为，并配合 Win32 扩展窗口样式实现互操作方案。

它适合以下场景：

- 默认阶段需要保留普通不透明窗口的运行方式；
- 首次透明后允许窗口永久停留在带 Alpha 的模式；
- 必须保留同一个窗口对象（同一 HWND），不能关闭并重建窗口；

它不适合以下场景：

- 需要频繁在 X8 和 A8 之间来回切换；
- 关闭透明功能后必须恢复真正的 X8 render target；
- 只能依赖 WPF 公开透明窗口契约，不能接受 WPF 与 Win32 的互操作技巧；
- 无法对目标设备进行兼容性和稳定性验证。

这套做法只强调窗口生命周期约束和可观察状态的建立：完整 `WindowChrome`、`HwndTarget` 清屏色透明、`WS_EX_LAYERED` 保持存在。

## 为什么可以只做单向升级

普通 WPF 顶层窗口在不需要目标 Alpha 时，硬件渲染路径通常可以选择：

```text
D3DFMT_X8R8G8B8
```

这里的 X8 表示目标不保留可供合成使用的 Alpha。对于完全不透明的窗口，这是自然且直接的选择。

从 WPF 源码可见，透明 clear color 会把当前窗口的清屏透明度诉求传递给渲染层；当不透明需求变为需要 Alpha 时，WPF 会按该诉求重建 render target 并切到 A8。

当安装完整 glass 的 `WindowChrome` 时，WPF 会把 DWM frame 扩展到整个窗口，同时把 `HwndSource.CompositionTarget.BackgroundColor` 设置为透明。这个透明 clear color 会向 WPF 渲染层表达“目标需要 Alpha”的需求。

渲染层发现现有目标不满足 `NeedDestinationAlpha` 后，会释放并重建底层 render target，目标格式可从 X8 变为：

```text
D3DFMT_A8R8G8B8
```

这个方向是关键：从“不需要 Alpha”变成“需要 Alpha”时，WPF 已有触发目标重建的路径。

反方向则不同。后续把所有内容重新绘制成不透明，不代表 WPF 会忘记已经记录过的 `NeedDestinationAlpha`，也不代表底层目标会自动恢复成 X8。

因此本文接受一个限制：

> 同一个 HWND 一旦完成 A8 升级，本次窗口生命周期内不再尝试回到 X8 模式。

接受这个限制之后，就不需要通过反射访问 `MediaContext`、channel 或 `HwndTarget` 的内部成员，也不需要自行注销和重新注册 UCE target。

## 最重要的是调用顺序

这个方案不是简单地给窗口加一个 `WS_EX_LAYERED` 就完成了。为了尽量避免窗口样式已经改变，而透明渲染目标和业务内容尚未准备完成的中间状态，需要严格按照以下顺序执行：

```text
保持 AllowsTransparency=false
等待 HWND 创建
安装 WM_STYLECHANGING hook
准备透明业务内容
安装完整 WindowChrome
等待一次公开的渲染边界
确认托管端 clear color 已透明
开启 WS_EX_LAYERED 保留逻辑
添加 WS_EX_LAYERED
验证样式没有被 WPF 清除
```

这里有两个顺序特别容易写反。

第一个是安装 `HwndSource` hook。通过 hook 可以持续兜底窗口样式写入过程，尽量避免 `WS_EX_LAYERED` 在状态切换中被移除。

第二个是先通过 `WindowChrome` 建立目标 Alpha 需求，再添加 `WS_EX_LAYERED`，保证 DWM 与 WPF 渲染状态按同一方向过渡。

## 窗口启动时保持完全不透明

项目的窗口明确保持 `AllowsTransparency="False"`，而且没有在 XAML 中预先安装完整 `WindowChrome`：

```xml
<Window
    AllowsTransparency="False"
    WindowStyle="None"
    ResizeMode="CanMinimize"
    Style="{StaticResource PerformanceWindowStyle}">
```

`WindowStyle` 和 `ResizeMode` 建议满足以下条件之一：

- `WindowStyle="None"` 且 `ResizeMode="CanMinimize"`
- `WindowStyle="None"` 且 `ResizeMode="NoResize"`

默认背景画刷的 Alpha 是 `FF`：

```xml
<SolidColorBrush
    x:Key="WindowOpaqueBackgroundBrush"
    Color="#FF0A1020" />
```

窗口和根布局都使用这个不透明画刷。这样做不是单纯为了视觉效果，而是为了保证透明功能开启之前，窗口内容不依赖透明合成。

还要避免一开始就在 XAML 里面安装完整 glass 的 `WindowChrome`。如果窗口创建后立刻获得透明 clear color，那么 WPF 可能很早就产生目标 Alpha 需求，失去“透明功能开启之前保留 X8”的意义。

## 用状态机表达不可逆生命周期

控制器没有提供 `DisableTransparency` 或 `RestoreX8` 方法，而是只公开一次单向升级操作。内部状态如下：

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

这些状态不是为了让代码看起来更复杂，而是为了明确每个阶段允许发生什么：

- `OpaqueX8` 表示尚未开始升级，可以接受第一次请求；
- `Upgrading` 表示正在跨越 WPF 渲染和 Win32 样式边界；
- `AlphaModeApplied` 表示上层配置已经完成，重复调用直接返回；
- `Faulted` 表示升级过程部分失败，不能继续按照初始 X8 状态盲目重试；
- `Closed` 表示窗口生命周期结束，不能再访问 HWND。

失败后不自动重试非常重要。第一次尝试有可能已经触发 WPF 创建 A8 目标，只是在设置或验证 `WS_EX_LAYERED` 时失败。此时窗口已经不能再被可靠地当成最初的 X8 状态处理。

## 让多个透明请求复用同一个任务

真实业务中，可能有多个模块几乎同时请求开启透明。如果每个调用方都分别安装 `WindowChrome`、等待渲染并修改窗口样式，就会引入竞争。

控制器使用 `_upgradeTask` 缓存第一次升级任务：

```csharp
if (State == AlphaUpgradeState.AlphaModeApplied)
{
    return Task.CompletedTask;
}

if (_upgradeTask is not null)
{
    return _upgradeTask;
}

_upgradeTask = UpgradeCoreAsync(prepareTransparentContent);
return _upgradeTask;
```

第一次请求创建任务，后续请求等待同一个任务。这样可以让透明请求具备幂等性，并保证窗口级资源只由一个控制器管理。

## 在 HWND 创建之后初始化控制器

控制器订阅窗口的 `SourceInitialized` 和 `Closed` 事件：

```csharp
_window.SourceInitialized += OnSourceInitialized;
_window.Closed += OnWindowClosed;
```

`SourceInitialized` 发生之后，窗口才拥有可用的 HWND。控制器会取得 HWND 和对应的 `HwndSource`，并检查窗口生命周期中句柄没有发生变化：

```csharp
IntPtr hwnd = new WindowInteropHelper(_window).Handle;
HwndSource source = HwndSource.FromHwnd(hwnd)
    ?? throw new InvalidOperationException(
        "Cannot obtain the HwndSource for the Window.");

if (_hwnd != IntPtr.Zero && _hwnd != hwnd)
{
    throw new InvalidOperationException(
        "The Window HWND changed during its lifetime.");
}
```

这段检查对应了本文最核心的业务约束：升级前后必须保留同一个 HWND。

所有视觉树、`WindowChrome`、`HwndSource` hook 和窗口样式操作都必须发生在窗口所属的 Dispatcher 线程。控制器在入口使用 `VerifyAccess` 尽早拒绝错误线程调用：

```csharp
_window.Dispatcher.VerifyAccess();
```

## 使用 WM_STYLECHANGING 保住 WS_EX_LAYERED

控制器先通过 `HwndSource.AddHook` 安装消息 hook。等真正准备添加分层窗口样式时，再把 `_keepLayeredStyle` 设置为 `true`。

hook 只关心扩展样式变化：

```csharp
if (_keepLayeredStyle &&
    message == WmStyleChanging &&
    wParam.ToInt64() == GwlExStyle)
{
    StyleStruct style = Marshal.PtrToStructure<StyleStruct>(lParam);
    style.StyleNew |= WsExLayered;
    Marshal.StructureToPtr(style, lParam, false);
    handled = true;
}
```

当 WPF 或其他逻辑准备修改 `GWL_EXSTYLE` 时，这段代码会在 WPF 后续处理之前截获消息，在新的样式值里面强制保留 `WS_EX_LAYERED`，并通过 `handled = true` 阻止后续 WPF 消息处理再次修改这个值。

这里的 hook 会拦截开启保留状态之后的所有 `GWL_EXSTYLE` 类型 `WM_STYLECHANGING`，不只是“准备清除 `WS_EX_LAYERED`”这一种变化。标记消息已处理也可能跳过 WPF 原本针对这条消息执行的其他内部逻辑，因此需要重点覆盖窗口状态切换、DPI 变化和显示设备重建等场景进行验证。

`STYLESTRUCT` 里面的样式值始终是两个 32 位 `DWORD`，不会因为进程运行在 64 位环境就变成 64 位：

```csharp
[StructLayout(LayoutKind.Sequential)]
private struct StyleStruct
{
    public uint StyleOld;
    public uint StyleNew;
}
```

升级完成之后也不能立即移除 hook。WPF 可能在窗口缩放、DPI 变化、最大化或其他窗口状态更新中再次调整扩展样式，因此控制器会一直保留 hook，直到窗口关闭。

## 通过完整 WindowChrome 请求目标 Alpha

准备透明业务内容之后，控制器安装覆盖整个窗口的 glass frame：

```csharp
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
```

这里的重点不是自定义标题栏本身，而是 `GlassFrameCompleteThickness`。它让 `WindowChrome` 把 DWM frame 扩展到完整窗口，并使组合目标的背景 clear color 进入透明状态。

安装之后，控制器还会检查托管端可观察的 clear color：

```csharp
HwndTarget? target = _source?.CompositionTarget;
if (target is null || target.BackgroundColor.A != 0)
{
    throw new NotSupportedException(
        "WindowChrome did not establish a transparent clear color.");
}
```

这个检查可以确认透明 clear color 已经进入托管端渲染链，并触发了对应的目标 Alpha 诉求；这是从业务层最容易观测到的里程碑。

## 等待一个公开 API 能做到的渲染边界

`WindowChrome` 修改 clear color 之后，变化还需要通过 WPF 的 channel 到达渲染线程。示例先等待 Dispatcher 的 Render 优先级，再使窗口失效，最后等待 ContextIdle：

```csharp
await _window.Dispatcher.InvokeAsync(
    static () => { },
    DispatcherPriority.Render,
    cancellationToken);

_window.InvalidateVisual();

await _window.Dispatcher.InvokeAsync(
    static () => { },
    DispatcherPriority.ContextIdle,
    cancellationToken);
```

这里没有等待 `CompositionTarget.Rendering` 事件。因为窗口隐藏、最小化或当前没有活动呈现时，这个事件可能长时间不触发，升级任务就可能一直挂住。

这段等待只是公开 API 能提供的渲染边界，不等价于 WPF 内部 `MediaContext.CompleteRender()` 的严格 channel 同步。

## 最后才添加 WS_EX_LAYERED

确认 HWND 和透明 clear color 之后，控制器才开启样式保留并添加 `WS_EX_LAYERED`：

```csharp
_keepLayeredStyle = true;
SetExtendedStyle(GetExtendedStyle() | WsExLayered);
VerifyLayeredStyle();
```

`GetWindowLongPtr` 和 `SetWindowLongPtr` 返回零不一定代表失败。示例会在调用之前清空 Win32 last error，再结合 `Marshal.GetLastWin32Error()` 判断：

```csharp
SetLastError(0);
IntPtr value = GetWindowLongPtr(_hwnd, GwlExStyle);
int error = Marshal.GetLastWin32Error();

if (value == IntPtr.Zero && error != 0)
{
    throw new Win32Exception(error);
}
```

这是 Win32 互操作里面很容易忽略的细节。如果把所有零返回都当成失败，可能会误报；如果完全不检查 last error，又可能吞掉真实错误。

这段实现偏保守，正式产品可根据场景决定是否保留所有 `last error` 分支。

## 用不透明安全帧遮住切换过程

示例项目没有让用户直接看到升级的中间状态，而是在开始升级之前显示一个完全不透明的 `TransitionShield`：

```csharp
TransitionShield.Visibility = Visibility.Visible;
_viewModel.MarkUpgrading();
WindowShell.Background = Brushes.Transparent;
```

随后才让控制器准备透明内容：

```csharp
await _alphaController.UpgradeToTransparentAsync(
    PrepareTransparentContent);
```

透明内容准备方法会把窗口背景和根布局改成透明：

```csharp
private void PrepareTransparentContent()
{
    Background = Brushes.Transparent;
    RootSurface.Background = Brushes.Transparent;
}
```

升级完成并跨过一个 `ContextIdle` 后，再隐藏安全帧：

```csharp
await Dispatcher.InvokeAsync(
    static () => { },
    DispatcherPriority.ContextIdle);

TransitionShield.Visibility = Visibility.Collapsed;
```

这样做的作用是让窗口在准备 A8 目标、透明内容和 Win32 样式期间，仍然输出完全不透明的像素。即使内部切换跨越多个阶段，用户也不会提前看到桌面穿透或未准备完成的透明内容。

## 窗口关闭时取消尚未完成的升级

升级过程包含异步 Dispatcher 等待。如果窗口在等待期间关闭，后续代码不能继续访问已经失效的 HWND。

控制器使用一个和窗口生命周期绑定的 `CancellationTokenSource`：

```csharp
private readonly CancellationTokenSource
    _lifetimeCancellationTokenSource = new();
```

窗口关闭或控制器释放时取消等待，并移除消息 hook：

```csharp
_disposed = true;
_keepLayeredStyle = false;
_lifetimeCancellationTokenSource.Cancel();
RemoveHook();
State = AlphaUpgradeState.Closed;
```

升级过程会在安装 `WindowChrome` 前后、等待渲染后和修改样式前检查取消或对象状态。这不是为了让取消逻辑看起来完整，而是为了避免窗口生命周期结束后继续调用 Win32 API。

## MVVM 只管理业务状态，不持有 HwndSource

示例项目使用 `MainWindowViewModel` 展示当前升级状态和渲染采样数据，但没有把 `Window`、`HwndSource` 或 `WindowChrome` 放进 ViewModel。

ViewModel 只表达以下业务状态：

```csharp
internal enum TransparencyDemoState
{
    OpaqueX8,
    Upgrading,
    AlphaModeApplied,
    Faulted,
}
```

窗口代码负责把业务请求转交给 `OneWayAlphaWindowController`。这种划分让界面状态仍然可以通过数据绑定更新，同时把直接依赖 WPF 窗口和 Win32 生命周期的操作留在 View 层服务中。

如果在更大的 MVVM 项目里面接入，可以使用附加行为或窗口服务做转发；如果项目不按 MVVM 组织，也可以把这一层下沉到窗口服务。

## 演示项目如何采样呈现数据

为了方便比较升级前后的表现，示例窗口持续播放旋转、缩放、脉冲、渐变等动画，并订阅 `CompositionTarget.Rendering`：

```csharp
CompositionTarget.Rendering += OnCompositionTargetRendering;
```

每次回调累加帧数，并大约每秒计算一次 FPS 和平均帧时间：

```csharp
long sampledFrameCount =
    _renderedFrameCount - _lastRenderingSampleFrameCount;

double framesPerSecond =
    sampledFrameCount / sampleDuration.TotalSeconds;

double averageFrameTimeMilliseconds = framesPerSecond > 0
    ? TimeSpan.FromSeconds(1 / framesPerSecond).TotalMilliseconds
    : 0;
```

这里显示的是 `CompositionTarget.Rendering` 回调采样，适合帮助观察切换前后是否出现明显卡顿，也可同时采用其他图形诊断工具（如 GPUView、WPA、PresentMon 等）做更深入分析。

真正评估收益时，建议分别采样：

```text
阶段 A：窗口启动后、尚未请求透明的稳定 X8 阶段
阶段 B：首次升级后的稳定 A8 阶段
阶段 C：X8 到 A8 的一次性切换过程
```

建议关注以下数据：

- 应用进程 GPU 使用率；
- DWM GPU 时间；
- present 次数和稳定帧率；
- 大尺寸窗口下的显存带宽压力；
- 静态窗口和持续动画窗口的差异；
- 切换耗时、UI stall、黑帧和闪烁；
- 硬件渲染、软件回退、远程桌面和设备重建场景。

## 不要混用其他 layered window 能力

本文实现的是 WPF 内容自己的每像素 Alpha，不要额外调用：

```text
SetLayeredWindowAttributes(..., LWA_ALPHA)
```

这个 API 设置的是整窗常量 Alpha，常用于窗口淡入淡出等整体透明动画，不是窗口内容像素级 Alpha 混合场景，不能代替 A8 render target。

反射修改 `HwndTarget.UsesPerPixelOpacity` 会改变 WPF 的内部透明路径，且会与本方案目标路径冲突。

本方案的设计前提是整个窗口生命周期保持：

```text
AllowsTransparency=false
HwndSource.UsesPerPixelOpacity=false
```

## 升级后如何恢复不透明外观

进入 `AlphaModeApplied` 之后，业务仍然可以把窗口背景和所有内容重新绘制为 Alpha = 255（即完全不透明）。这样用户看到的窗口会恢复为不透的外观。

但这只是输出仍走 A8 目标并全部绘制成不透明像素，不代表返回了 X8。

如果业务真的需要重新获得 X8，在这篇单向升级方案里只有一个选择：

- 关闭当前窗口，创建一个新的默认不透明窗口，并迁移业务状态。

## 建议验证清单

至少需要在产品支持的环境中验证以下内容：

- 窗口启动时没有 `WS_EX_LAYERED`；
- 升级前窗口背景和根视觉树完全不透明；
- 升级后 `WS_EX_LAYERED` 仍然存在；
- 实际读取并记录 `HwndSource.UsesPerPixelOpacity`，确认它仍然为 `false`；
- 透明区域可以正确透出，不透明区域没有意外变淡；
- 重复请求不会重复安装 hook 或重复执行升级；
- resize、最大化、最小化和恢复之后样式仍然存在；
- DPI 和显示器切换之后透明效果正常；
- 窗口关闭时不会继续访问 HWND；
- 软件渲染、锁屏、远程桌面和显示设备重建之后行为可接受。

如果需要验证真实渲染目标格式，可以在自编译 WPF 中观察 `NeedDestinationAlpha` 和目标重建过程，或者使用图形诊断工具确认目标从 `D3DFMT_X8R8G8B8` 变成 `D3DFMT_A8R8G8B8`。

## 代码实现

本文代码放在 [github](https://github.com/lindexi/lindexi_gd/tree/6a0a196819cd1ade5d5ae4aaff2a6b0c374cb5a7/WPFDemo/JacaikecejihaiweHeqajearhi) 和 [gitee](https://gitee.com/lindexi/lindexi_gd/tree/6a0a196819cd1ade5d5ae4aaff2a6b0c374cb5a7/WPFDemo/JacaikecejihaiweHeqajearhi) 上，可以使用如下命令行拉取代码。我整个代码仓库比较庞大，使用以下命令行可以进行部分拉取，拉取速度比较快

先创建一个空文件夹，接着使用命令行 cd 命令进入此空文件夹，在命令行里面输入以下代码，即可获取到本文的代码

```text
git init
git remote add origin https://gitee.com/lindexi/lindexi_gd.git
git pull origin 6a0a196819cd1ade5d5ae4aaff2a6b0c374cb5a7
```

以上使用的是国内的 gitee 的源，如果 gitee 不能访问，请替换为 github 的源。请在命令行继续输入以下代码，将 gitee 源换成 github 源进行拉取代码。如果依然拉取不到代码，可以发邮件向我要代码

```text
git remote remove origin
git remote add origin https://github.com/lindexi/lindexi_gd.git
git pull origin 6a0a196819cd1ade5d5ae4aaff2a6b0c374cb5a7
```

获取代码之后，进入 `WPFDemo/JacaikecejihaiweHeqajearhi` 文件夹，即可获取到源代码

更多技术博客，请参阅 [博客导航](https://blog.lindexi.com/post/%E5%8D%9A%E5%AE%A2%E5%AF%BC%E8%88%AA.html )
