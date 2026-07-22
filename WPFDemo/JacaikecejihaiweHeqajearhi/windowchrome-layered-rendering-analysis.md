# WindowChrome、WS_EX_LAYERED 与 WPF 透明渲染分析

## 结论摘要

针对以下配置：

```csharp
WindowChrome.SetWindowChrome(this, new WindowChrome
{
    GlassFrameThickness = WindowChrome.GlassFrameCompleteThickness,
    CaptionHeight = 0,
    CornerRadius = new CornerRadius(),
    ResizeBorderThickness = new Thickness(),
    UseAeroCaptionButtons = false,
});
```

结论如下：

1. **它确实会改变窗口的渲染与合成条件，但不会把窗口切换到 `AllowsTransparency=true` 所使用的 WPF 逐像素分层窗口路径。**
   - 整个窗口被当作客户区。
   - DWM frame 被扩展到整个窗口。
   - `HwndTarget` 的清屏色被改成透明色。
   - MIL/WpfGfx 因此会要求一个带 Alpha 通道的渲染目标；硬件路径通常从 `D3DFMT_X8R8G8B8` 变为 `D3DFMT_A8R8G8B8`，MIL 将后者按预乘 Alpha 的 `PBGRA32bpp` 解释。
   - 在 `AllowsTransparency=false` 时，窗口的 MIL layer type 仍然是 `NotLayered`，正常呈现路径仍是 D3D9/GDI，而不是 `UpdateLayeredWindow`。

2. **`GlassFrameCompleteThickness` 才是这段代码里建立透明合成条件的核心。**
   - 它令 WPF 调用 `DwmExtendFrameIntoClientArea`，四边 margin 为负值，表示把 glass 扩展到整个窗口。
   - 同时 WPF 把底层 `HwndTarget.BackgroundColor` 设为透明，让未被不透明 WPF 内容覆盖的位置保留 Alpha。
   - 因此，不设置 `AllowsTransparency=true` 也可以出现 DWM glass/背景透出的效果。这是 DWM 扩展 frame 路径，不是 WPF 的 `UpdateLayeredWindow` 路径。

3. **仅手工添加 `WS_EX_LAYERED` 并不会让 WPF 自动启用逐像素 Alpha。**
   - `AllowsTransparency=false` 时，`HwndTarget` 会在 `WM_STYLECHANGING` 中主动清除外部添加的 `WS_EX_LAYERED`。
   - 即使该位暂时存在，下一次 `UpdateWindowSettings` 也会因为 WPF 的透明标志仍是 `Opaque` 而再次移除它。
   - 但是，运行时通过 `HwndSource.AddHook` 添加的公共 hook 实际位于内部 `HwndTarget` hook 之前。公共 hook 可以修改 `WM_STYLECHANGING` 的 `STYLESTRUCT.styleNew`，重新加入 `WS_EX_LAYERED`，并以 `handled=true` 阻止内部 hook 再次清位。
   - 后续 `UpdateWindowSettings` 即使再次尝试移除该位，也会同步触发同一个 `WM_STYLECHANGING` hook，因此该位可以稳定保留。不过 WPF 随后仍按自己的 `Opaque` 标志向 MIL 发送 `NotLayered`；保留 HWND 样式不等于切换 WPF 的呈现模式。

4. **没有设置类似 DXGI `AlphaMode.Premultiplied` 的交换链属性。**
   - 此处 WPF 使用的是 D3D9 `IDirect3DSwapChain9`/`D3DPRESENT_PARAMETERS`，仓库中这条路径没有 `DXGI_SWAP_CHAIN_DESC` 或 `DXGI_ALPHA_MODE_PREMULTIPLIED`。
   - 但 WPF 内部确实使用预乘 Alpha：`A8R8G8B8` 被解释成 `PBGRA32bpp`，常规 SrcOver 混合使用 `ONE` 与 `INVSRCALPHA`。
   - 这属于 WPF/MIL 的像素格式和混合约定，**不等于**给 DXGI 交换链设置 `AlphaMode.Premultiplied`。

5. **这段 `WindowChrome` 配置本身不保证视觉上一定透明。**
   - `WindowChromeWorker` 的源码明确说明，glass 只有在没有被其他内容覆盖时才可见，而且 `Window.Background` 需要单独处理。
   - 传统 WPF 主题通常给 `Window.Background` 设置不透明的 `SystemColors.WindowBrush`；这种背景会覆盖透明清屏色。
   - 如果窗口模板、`Window.Background` 或根元素背景是透明的，DWM glass 才能在相应区域显现。

6. **运行时透明切换依靠 `WS_EX_LAYERED` 样式位本身，不依靠 `SetLayeredWindowAttributes`。**
   - 仓库可以证明 `WindowChrome` 已建立完整 DWM frame、透明 clear color 和带 Alpha 的 D3D9/MIL 目标。
   - 仓库也可以证明标准 WPF 会清除外部添加的 `WS_EX_LAYERED`，而不会把它解释为 `AllowsTransparency`。
   - 在处理 `WM_NCCALCSIZE`、让客户区覆盖整个窗口，并准备好带 Alpha 的窗口内容之后，可以通过添加或移除 `WS_EX_LAYERED` 切换透明合成状态。
   - `SetLayeredWindowAttributes(..., LWA_ALPHA)` 设置的是整窗常量 Alpha，属于另一种 layered-window 功能，不是本文所讨论的切换机制。

---

## 1. `SetWindowChrome` 实际做了什么

### 1.1 附加属性会创建并驱动 `WindowChromeWorker`

`WindowChrome.SetWindowChrome` 只是设置附加依赖属性。属性变化后，WPF 为每个 `Window` 创建或复用一个 `WindowChromeWorker`，并调用 `SetWindowChrome`：

- [`WindowChrome.cs:52-102`](../src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Shell/WindowChrome.cs#L52-L102)
- [`WindowChromeWorker.cs:74-97`](../src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Shell/WindowChromeWorker.cs#L74-L97)

当 HWND 已经存在时，worker 会：

- 给 `HwndSource` 添加窗口消息钩子；
- 修正模板；
- 更新 system menu；
- 更新 glass/frame 状态；
- 调用带 `SWP_FRAMECHANGED` 的 `SetWindowPos`，强制 Windows 重新计算非客户区。

参见 [`WindowChromeWorker.cs:230-257`](../src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Shell/WindowChromeWorker.cs#L230-L257)。

### 1.2 整个窗口变为客户区

`WindowChromeWorker` 接管 `WM_NCCALCSIZE`。当 `NonClientFrameEdges` 保持默认的 `None` 时，它不缩小传入的窗口矩形，却把消息标记为已处理。结果是提议的窗口矩形直接成为客户区，系统标准标题栏和边框不再占据独立的非客户区。

参见 [`WindowChromeWorker.cs:399-449`](../src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Shell/WindowChromeWorker.cs#L399-L449)。

这与 `AllowsTransparency=true` 的一个几何效果相似：`HwndSource` 的逐像素透明路径同样通过处理 `WM_NCCALCSIZE`，让客户区覆盖整个窗口。参见 [`HwndSource.cs:1240-1317`](../src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/InterOp/HwndSource.cs#L1240-L1317)。

但是，两者只有“客户区覆盖全窗口”这一点相似；它们后续采用的窗口呈现机制不同。

### 1.3 `GlassFrameCompleteThickness` 扩展完整 DWM frame

`GlassFrameCompleteThickness` 就是四边均为 `-1` 的 `Thickness`：

- [`WindowChrome.cs:47-48`](../src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Shell/WindowChrome.cs#L47-L48)

只要 DWM composition 开启，并且 `GlassFrameThickness` 不是全零，worker 就认为 glass 已启用，清除窗口 HRGN，然后调用 `_ExtendGlassFrame`：

- [`WindowChromeWorker.cs:711-735`](../src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Shell/WindowChromeWorker.cs#L711-L735)

`_ExtendGlassFrame` 做了两项与渲染直接相关的工作：

1. 将 `HwndSource.CompositionTarget.BackgroundColor` 设为 `Colors.Transparent`；
2. 将换算后的四边 margin 传给 `DwmExtendFrameIntoClientArea`。

参见 [`WindowChromeWorker.cs:932-1010`](../src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Shell/WindowChromeWorker.cs#L932-L1010)。源码注释还明确指出：这只是在 Win32 层让 glass 可见，前提是没有别的内容覆盖它，`Window.Background` 必须独立处理。

如果 DWM composition 不可用，worker 会把底层背景恢复为 `SystemColors.WindowColor`，不会得到这条透明路径。

### 1.4 其余四个属性主要影响输入与窗口形状

| 属性 | 在该取值下的直接效果 | 是否直接选择 Alpha/交换链模式 |
|---|---|---|
| `CaptionHeight = 0` | 自定义命中测试中没有标题栏拖动区域 | 否 |
| `ResizeBorderThickness = 0` | 自定义命中测试中没有窗口缩放边缘 | 否 |
| `UseAeroCaptionButtons = false` | `WM_NCHITTEST` 不再先交给 `DwmDefWindowProc` 处理系统标题按钮 | 否 |
| `CornerRadius = 0` | 在非 glass 回退路径中创建矩形 HRGN；glass 开启时 HRGN 会被清除 | 否 |

命中测试逻辑见 [`WindowChromeWorker.cs:477-531`](../src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Shell/WindowChromeWorker.cs#L477-L531) 和 [`WindowChromeWorker.cs:1024-1066`](../src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Shell/WindowChromeWorker.cs#L1024-L1066)。圆角区域逻辑见 [`WindowChromeWorker.cs:739-889`](../src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Shell/WindowChromeWorker.cs#L739-L889)。

`WindowChromeWorker` 没有在这里添加 `WS_EX_LAYERED`。它对普通窗口 style 的修改只用于抑制系统标题栏重绘等辅助操作。

---

## 2. 透明清屏色如何改变 WpfGfx 渲染目标

### 2.1 `BackgroundColor=Transparent` 会进入 MIL，而不只是托管属性

`HwndTarget.BackgroundColor` 变化时，会向 DUCE/MIL 发送 `SetClearColor` 并安排重新渲染：

- [`HwndTarget.cs:2348-2385`](../src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/InterOp/HwndTarget.cs#L2348-L2385)

渲染线程收到透明 clear color 后，`CSlaveHWndRenderTarget::ProcessSetClearColor` 会检测到 Alpha 小于 1，并添加 `MilRTInitialization::NeedDestinationAlpha`。改变初始化标志还会导致现有 render target 被释放并按新标志重建：

- [`hwndtarget.cpp:656-677`](../src/Microsoft.DotNet.Wpf/src/WpfGfx/core/uce/hwndtarget.cpp#L656-L677)
- [`hwndtarget.cpp:759-837`](../src/Microsoft.DotNet.Wpf/src/WpfGfx/core/uce/hwndtarget.cpp#L759-L837)

`NeedDestinationAlpha` 的定义就是要求 back buffer 至少具有 8 位 Alpha 通道：

- [`wgx_core_types.h:801-805`](../src/Microsoft.DotNet.Wpf/src/WpfGfx/include/processed/wgx_core_types.h#L801-L805)

因此，这个 `WindowChrome` 设置并非只改非客户区；它还可能造成真实的渲染目标重建和像素格式变化。

### 2.2 硬件与软件目标都会选择带 Alpha 的格式

硬件 D3D9 路径中：

- 有 `NeedDestinationAlpha` 时选择 `D3DFMT_A8R8G8B8`；
- 否则选择 `D3DFMT_X8R8G8B8`。

参见 [`d3ddevicemanager.cpp:605-628`](../src/Microsoft.DotNet.Wpf/src/WpfGfx/core/hw/d3ddevicemanager.cpp#L605-L628)。

创建硬件显示目标时，WPF 调用 `D3DFormatToPixelFormat(..., TRUE)`，把 `D3DFMT_A8R8G8B8` 解释成 `MilPixelFormat::PBGRA32bpp`：

- [`hwdisplayrt.cpp:184-197`](../src/Microsoft.DotNet.Wpf/src/WpfGfx/core/hw/hwdisplayrt.cpp#L184-L197)
- [`pixelformatutils.cpp:244-270`](../src/Microsoft.DotNet.Wpf/src/WpfGfx/common/shared/pixelformatutils.cpp#L244-L270)

软件 HWND render target 同样根据 `NeedDestinationAlpha` 在 `PBGRA32bpp` 与 `BGR32bpp` 之间选择：

- [`swhwndrt.cpp:310-336`](../src/Microsoft.DotNet.Wpf/src/WpfGfx/core/sw/swlib/swhwndrt.cpp#L310-L336)

所以可以准确地说：**这条 WindowChrome 全 glass 路径会促使 WPF 使用能够保存预乘 Alpha 的目标格式。**

但这仍然不能推出“WPF 设置了交换链 `AlphaMode.Premultiplied`”，因为 D3D9 根本没有 DXGI 的 `AlphaMode` 字段。

### 2.3 内部混合也采用预乘 Alpha 约定

WPF 的常规硬件 SrcOver 混合状态明确标注 source 和 destination 使用 premultiplied alpha，并配置为：

- source blend：`D3DBLEND_ONE`
- destination blend：`D3DBLEND_INVSRCALPHA`

参见 [`d3drenderstate.cpp:244-252`](../src/Microsoft.DotNet.Wpf/src/WpfGfx/core/hw/d3drenderstate.cpp#L244-L252)。

这说明 WPF 的内部渲染数学确实与预乘 Alpha 一致，但这是 MIL/D3D9 的像素格式和 blend state，而不是窗口交换链的 DXGI AlphaMode。

---

## 3. `AllowsTransparency=true` 的标准路径

`Window.AllowsTransparency` 在创建 HWND 前被复制到 `HwndSourceParameters.UsesPerPixelOpacity`：

- [`Window.cs:2617-2628`](../src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Window.cs#L2617-L2628)

`HwndSource` 随后：

1. 添加 `WS_EX_LAYERED`；
2. 创建 `HwndTarget`；
3. 将 `HwndTarget.UsesPerPixelOpacity` 设为 `true`；
4. 将 `HwndTarget.BackgroundColor` 设为透明。

参见 [`HwndSource.cs:245-277`](../src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/InterOp/HwndSource.cs#L245-L277)。

`HwndTarget.UpdateWindowSettings` 再把这组状态编码为：

- `MILTransparencyFlags.PerPixelAlpha`；
- `MILWindowLayerType.ApplicationManagedLayer`。

参见 [`HwndTarget.cs:2147-2248`](../src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/InterOp/HwndTarget.cs#L2147-L2248)。

原生 WpfGfx 对 `ApplicationManagedLayer` 的处理是：

- 选择 `PresentUsingUpdateLayeredWindow`，见 [`api_factory.cpp:552-568`](../src/Microsoft.DotNet.Wpf/src/WpfGfx/core/api/api_factory.cpp#L552-L568)；
- 使用 `UpdateLayeredWindow` 或 `UpdateLayeredWindowIndirect`，见 [`swpresentgdi.cpp:768-810`](../src/Microsoft.DotNet.Wpf/src/WpfGfx/core/sw/swlib/swpresentgdi.cpp#L768-L810)；
- 使用 `PBGRA32bpp` 作为 32 位呈现格式，见 [`swpresentgdi.cpp:1262-1275`](../src/Microsoft.DotNet.Wpf/src/WpfGfx/core/sw/swlib/swpresentgdi.cpp#L1262-L1275)；
- 设置 `BLENDFUNCTION.AlphaFormat = AC_SRC_ALPHA`，见 [`mildc.cpp:260-277`](../src/Microsoft.DotNet.Wpf/src/WpfGfx/core/common/mildc.cpp#L260-L277)。

这才是 WPF 支持的逐像素分层窗口路径，也是最接近“把预乘 Alpha 交给桌面进行逐像素混合”的实现。

---

## 4. 为什么手工设置 `WS_EX_LAYERED` 不是 `AllowsTransparency` 的替代品

### 4.1 WPF 明确阻止外部把普通顶层窗口改成 layered window

`HwndTarget` 处理 `WM_STYLECHANGING` 时检查 `UsesPerPixelOpacity`：

- 为 `true`：强制保留 `WS_EX_LAYERED`；
- 为 `false`：强制清除 `WS_EX_LAYERED`。

源码注释直接说明，这是为了阻止外部程序把顶层 WPF 窗口改成 system-layered window：

- [`HwndTarget.cs:1144-1171`](../src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/InterOp/HwndTarget.cs#L1144-L1171)

所以在标准 `SetWindowLongPtr(GWL_EXSTYLE, ...)` 调用中，Windows 发出 `WM_STYLECHANGING` 后，WPF 通常会从 `styleNew` 中删除该位。

这里需要修正一个容易产生的误区：**运行时通过 `HwndSource.AddHook` 添加的公共 hook 不是在 `HwndTarget` 之后执行，而是在它之前执行。**

`HwndSource.AddHook` 在首次添加公共 hook 时调用 `HwndWrapper.AddHook`，后者使用 `Insert(0, hook)` 将其放到 hook 列表首部：

- [`HwndSource.cs:360-370`](../src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/InterOp/HwndSource.cs#L360-L370)
- [`HwndWrapper.cs:221-230`](../src/Microsoft.DotNet.Wpf/src/Shared/MS/Win32/HwndWrapper.cs#L221-L230)

`HwndWrapper.WndProc` 按列表顺序调用 hook，并在任一 hook 设置 `handled=true` 后立即停止：

- [`HwndWrapper.cs:235-255`](../src/Microsoft.DotNet.Wpf/src/Shared/MS/Win32/HwndWrapper.cs#L235-L255)

因此，自定义 hook 中修改 `STYLESTRUCT.styleNew` 并设置 `handled=true` 的实际逻辑是：

1. 在 `HwndTarget` 处理消息之前，把 `WS_EX_LAYERED` 加回 `styleNew`；
2. 阻止后续内部 hook 收到本次 `WM_STYLECHANGING`；
3. 让发起 `SetWindowLongPtr` 的同一次 Win32 样式修改最终采用被修改后的 `styleNew`。

它不是“等 WPF 清理完成后再添加一次”，但从 Win32 样式修改的结果看，同样可以在一次同步调用内稳定保留该位。

### 4.2 下一次同步窗口状态时还会再清理一次

`UpdateWindowSettings` 会重新读取 HWND 的扩展样式。如果它看到 `WS_EX_LAYERED`，但 WPF 自己的透明 flags 仍为 `Opaque`，就调用 `SetWindowLong` 移除该位：

- [`HwndTarget.cs:2147-2185`](../src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/InterOp/HwndTarget.cs#L2147-L2185)

这个 `SetWindowLong` 同样会同步触发 `WM_STYLECHANGING`。只要前述公共 hook 仍然安装并处于“保留 layered 位”的状态，它就可以再次把 `WS_EX_LAYERED` 加回 `styleNew`，使 WPF 的移除尝试在 HWND 层面不生效。

但是 `UpdateWindowSettings` 返回后不会重新读取实际扩展样式，而是直接用 WPF 自己的透明 flags 重新计算局部变量 `isLayered`：

- [`HwndTarget.cs:2203-2248`](../src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/InterOp/HwndTarget.cs#L2203-L2248)

因此普通的 `AllowsTransparency=false` 窗口仍会向 MIL 发送 `NotLayered + Opaque`，而不会因为 HWND 上的样式位存活就自动变成 `ApplicationManagedLayer` 或 `SystemManagedLayer`。最终状态可以明确分裂为：

- User32 看到 HWND 带有 `WS_EX_LAYERED`；
- WPF/MIL 仍把窗口作为 `NotLayered + Opaque` 管理。

### 4.3 `SystemManagedLayer` 与逐像素 Alpha 也不是一回事

WpfGfx 架构中保留了 `SystemManagedLayer`：

- 它明确不支持 `PerPixelAlpha`，见 [`hwndtarget.cpp:577-592`](../src/Microsoft.DotNet.Wpf/src/WpfGfx/core/uce/hwndtarget.cpp#L577-L592)；
- 它使用 `SetLayeredWindowAttributes` 提供 constant alpha 或 color key，见 [`desktophwndrt.cpp:1038-1069`](../src/Microsoft.DotNet.Wpf/src/WpfGfx/core/meta/desktophwndrt.cpp#L1038-L1069)；
- 为了让 User32 redirection 工作，呈现方式被强制为经 DC 的 `BitBlt`，见 [`api_factory.cpp:562-568`](../src/Microsoft.DotNet.Wpf/src/WpfGfx/core/api/api_factory.cpp#L562-L568)。

当前托管 `HwndTarget` 中用于 constant alpha/color key 的字段和代码已被注释。因此，单独塞入 `WS_EX_LAYERED` 不会自然进入一条受支持的 system-managed 透明配置。

---

## 5. 如何解释“WindowChrome + WS_EX_LAYERED 后窗口透明”的实际观察

必须区分以下两种情况。

### 情况 A：`WS_EX_LAYERED` 实际上已经被 WPF 清除

这是标准代码最预期的结果。此时观察到的透明来自：

1. `WindowChrome` 把整个窗口变成客户区；
2. `DwmExtendFrameIntoClientArea` 把 DWM frame 扩展到整个窗口；
3. `HwndTarget` 透明清屏并使用带 Alpha 的 render target；
4. 窗口模板或内容没有用不透明背景覆盖相应区域。

在这种情况下，`WS_EX_LAYERED` 并没有参与最终呈现，只是设置调用与 WindowChrome 的 DWM glass/背景透出效果在时间上相关。

### 情况 B：`WS_EX_LAYERED` 通过自定义 hook 持续保留

`HwndSource` 的公共消息 hook 会在内部 `HwndTarget` hook 之前运行。如果自定义 hook 修改 `STYLESTRUCT.styleNew` 并把 `WM_STYLECHANGING` 标为已处理，内部 hook 就没有机会清除该位。公共 hook 的注册、分发和提前终止行为可见：

- [`HwndSource.cs:360-370`](../src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/InterOp/HwndSource.cs#L360-L370)
- [`HwndSource.cs:1619-1640`](../src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/InterOp/HwndSource.cs#L1619-L1640)
- [`HwndWrapper.cs:221-230`](../src/Microsoft.DotNet.Wpf/src/Shared/MS/Win32/HwndWrapper.cs#L221-L230)
- [`HwndWrapper.cs:235-255`](../src/Microsoft.DotNet.Wpf/src/Shared/MS/Win32/HwndWrapper.cs#L235-L255)

如果确实绕过了 WPF 的修正，那么窗口同时具备：

- 一个由 WPF 按普通 `NotLayered` 窗口管理的 alpha-capable D3D9 render target；
- 完整扩展的 DWM glass frame；
- 一个 WPF 状态机并未认可的 `WS_EX_LAYERED` 样式位。

`HwndTarget.DoPaint` 中还有一个有限的兼容分支：当 HWND 带 `WS_EX_LAYERED`，但 `GetLayeredWindowAttributes` 失败时，代码把它称为“special layered, non-redirected window”，并在收到空的 paint rect 时强制整窗失效。参见 [`HwndTarget.cs:1347-1395`](../src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/InterOp/HwndTarget.cs#L1347-L1395)。这个分支只是在修复 paint 驱动问题，并没有设置 `PerPixelAlpha` 或切换到 `UpdateLayeredWindow`。

此时最终效果取决于 User32、DWM 和具体 Windows 版本如何处理这个非标准组合。WPF 仓库可以证明前述三个条件，但不能证明 Windows compositor 对该组合在所有版本上的精确行为。它也不能被解释为 WPF 已经切换到了 `AllowsTransparency` 的 `UpdateLayeredWindow` 路径。

因此，更准确的表述是：

> `WindowChrome` 已经创建了“全窗口 DWM frame + 透明 clear color + alpha-capable render target”的基础；如果 `WS_EX_LAYERED` 被强行保留下来，User32/DWM 可能以不同方式处理该 HWND 的重定向或合成，从而改变最终透明外观。但具体规则不在 WPF 仓库中。这是绕过 WPF 状态管理后的系统行为，不是 WPF 设置了逐像素透明模式。

---

## 6. 能否在运行时切换透明

可以。这里讨论的是在窗口已经具备 Alpha 渲染条件时，通过切换 `WS_EX_LAYERED` 改变窗口的 layered 合成状态，而不是给整个窗口再乘一个常量透明度。

### 6.1 切换依赖的条件

这条路径需要同时具备以下条件：

- `WM_NCCALCSIZE` 已被处理，使客户区覆盖需要参与透明合成的整个窗口区域；
- `WindowChrome` 的完整 glass 配置已经把底层 `HwndTarget.BackgroundColor` 设为透明；
- MIL/WpfGfx 已经创建能够保存 Alpha 的 `A8R8G8B8/PBGRA32` 渲染目标；
- 窗口背景、模板和根元素没有用不透明内容覆盖需要透明的像素；
- 自定义 `WM_STYLECHANGING` hook 能阻止 WPF 把外部添加的 `WS_EX_LAYERED` 清除。

在这些条件已经成立后，`WS_EX_LAYERED` 就是运行时切换所需的样式开关：添加该位启用 layered-window 能力，移除该位恢复普通窗口样式。透明度来自窗口现有像素中的 Alpha，而不是来自额外设置的整窗常量 Alpha。

`SetLayeredWindowAttributes(..., LWA_ALPHA)` 不参与这条路径。该 API 会为整个窗口设置统一的 constant alpha；它描述的是另一种视觉效果，既不是启用这里已有 Alpha 像素的前提，也不应被当作本节透明切换的一部分。

### 6.2 推荐的开启顺序

应在 HWND 创建完成后执行，例如 `Window.SourceInitialized` 之后：

1. 获取 `HwndSource` 和 HWND；
2. 先通过 `HwndSource.AddHook` 安装 `WM_STYLECHANGING` hook；
3. 确认 `WM_NCCALCSIZE` 已由 `WindowChromeWorker` 或自定义窗口过程处理，使客户区覆盖整个窗口；
4. 将内部“保留 layered 位”的状态设为 `true`；
5. 调用 `SetWindowLongPtr` 添加 `WS_EX_LAYERED`；
6. 必要时使用带 `SWP_FRAMECHANGED` 的 `SetWindowPos` 让系统重新计算非客户区，并请求窗口重绘。

顺序不能反过来。若先设置 `WS_EX_LAYERED` 再安装 hook，本次 `SetWindowLongPtr` 同步发送的 `WM_STYLECHANGING` 会先被 `HwndTarget` 清位。

核心逻辑可以写成：

```csharp
private bool _keepLayeredStyle;

private IntPtr WindowMessageHook(
    IntPtr hwnd,
    int message,
    IntPtr wParam,
    IntPtr lParam,
    ref bool handled)
{
    if (_keepLayeredStyle &&
        message == WM_STYLECHANGING &&
        wParam.ToInt64() == GWL_EXSTYLE)
    {
        var style = Marshal.PtrToStructure<STYLESTRUCT>(lParam);
        style.styleNew |= WS_EX_LAYERED;
        Marshal.StructureToPtr(style, lParam, false);
        handled = true;
    }

    return IntPtr.Zero;
}

private void EnableLayeredTransparency()
{
    _keepLayeredStyle = true;
    SetExtendedStyle(_hwnd, GetExtendedStyle(_hwnd) | WS_EX_LAYERED);
}
```

这里的 `SetExtendedStyle` 应按进程位数封装 `SetWindowLongPtr`，并保留原有扩展样式的其他位。`WM_NCCALCSIZE` 的处理已经由本文的 `WindowChrome` 配置建立；如果脱离 `WindowChrome` 单独实现，则必须在窗口过程中补上等价处理。实际代码还应检查 HWND 生命周期、原生 API 返回值和 UI 线程访问。

### 6.3 运行时切换

开启透明时设置 `WS_EX_LAYERED`，关闭透明时清除 `WS_EX_LAYERED`。这里切换的是窗口是否使用该 layered 样式，不是调整一个 `0` 到 `255` 的全局 Alpha 值。

```csharp
private void DisableLayeredTransparency()
{
    _keepLayeredStyle = false;
    SetExtendedStyle(_hwnd, GetExtendedStyle(_hwnd) & ~WS_EX_LAYERED);
}
```

样式修改应在窗口所属 Dispatcher 线程执行，并避免在布局、绘制或 HWND 销毁过程中切换。切换后如出现非客户区或画面没有立即更新，应使用 `SWP_FRAMECHANGED` 触发重新计算，并使窗口失效重绘。

### 6.4 推荐的关闭顺序

关闭时推荐：

1. 将“保留 layered 位”的状态设为 `false`；
2. 调用 `SetWindowLongPtr` 清除 `WS_EX_LAYERED`；
3. 确认样式已清除；
4. 如果后续不再开启透明，再移除公共 hook；
5. 必要时使用 `SWP_FRAMECHANGED` 并请求窗口重绘。

必须先停止 hook 对该位的强制保留，再清除样式。否则清除操作触发的 `WM_STYLECHANGING` 仍会把该位加回去。

### 6.5 这条路径的能力边界

这套方法可以通过 `WS_EX_LAYERED` 切换窗口透明合成状态，但不能据此认为 WPF 已正式切换到 `ApplicationManagedLayer` 或 `SystemManagedLayer` 呈现路径：

- 开启时 HWND 实际带有 `WS_EX_LAYERED`；
- 透明效果使用窗口现有的 Alpha 像素，不依赖 `SetLayeredWindowAttributes`；
- WPF/MIL 仍收到 `NotLayered + Opaque`；
- MIL 不会因为外部样式位自动改用 `SystemManagedLayer` 所要求的 DC `BitBlt` 路径。

因此，它是可以工作的 Win32 互操作技巧，但不是 WPF 公共契约保证的呈现模式。至少应验证：

- 硬件和软件渲染层级；
- 窗口缩放、最小化、隐藏再显示；
- DPI 和显示器切换；
- 锁屏、远程桌面和显示设备重建；
- 不同受支持 Windows 版本。

另外，不应在同一个现有 HWND 上尝试把这条路径直接切换为 WPF 的 `UpdateLayeredWindow` 路径。WPF 的 `ApplicationManagedLayer` 由创建 `HwndSource` 时的 `UsesPerPixelOpacity` 决定，不能仅靠清除并重新设置 `WS_EX_LAYERED` 在运行时启用。若以后需要 WPF 正式支持的逐像素 Alpha 路径，应先撤销当前样式状态，并重新创建一个从 HWND 创建阶段就启用 `AllowsTransparency=true` 或 `UsesPerPixelOpacity` 的窗口。

---

## 7. 三种主要路径对比

| 场景 | `HwndTarget.UsesPerPixelOpacity` | MIL layer type | MIL transparency flag | 目标格式 | 呈现方式 | 桌面透明机制 |
|---|---:|---|---|---|---|---|
| 普通 WPF 窗口 | `false` | `NotLayered` | `Opaque` | 通常 `X8R8G8B8` / `BGR32` | D3D9 Present 或 GDI | 普通 DWM 重定向窗口 |
| 本文的完整 `WindowChrome` glass | `false` | `NotLayered` | `Opaque` | 因透明 clear color 可变为 `A8R8G8B8` / `PBGRA32` | 仍为 D3D9 Present 或 GDI | DWM 扩展 frame；未被不透明 WPF 内容覆盖处可显示 glass/背景 |
| `AllowsTransparency=true` | `true` | `ApplicationManagedLayer` | `PerPixelAlpha` | `PBGRA32` | `UpdateLayeredWindow(Indirect)` | User32 按 `AC_SRC_ALPHA` 做逐像素 layered 合成 |
| 架构中的 system-managed layered | `false` | `SystemManagedLayer` | constant alpha/color key，不支持 per-pixel | 非逐像素 Alpha 呈现 | DC `BitBlt` + `SetLayeredWindowAttributes` | 常量透明度或色键 |
| Hook 保留 `WS_EX_LAYERED`、`AllowsTransparency=false` | 仍为 `false` | `NotLayered` | `Opaque` | 取决于 clear color；本文配置为带 Alpha 目标 | WPF 仍按普通窗口呈现 | 通过切换 `WS_EX_LAYERED` 使已有 Alpha 像素参与透明合成；WPF/MIL 状态不知情 |

表中最容易混淆的是第二行：**render target 有 Alpha 通道，不代表窗口被标记为 WPF per-pixel layered window。** `NeedDestinationAlpha` 与 `MILTransparencyFlags.PerPixelAlpha` 是两个不同层次的状态。

---

## 8. 是否等价于 `AlphaMode.Premultiplied`

### 不等价的部分

- WPF 的窗口硬件呈现代码使用 D3D9 `D3DPRESENT_PARAMETERS`，例如 [`d3ddevicemanager.cpp:1424-1474`](../src/Microsoft.DotNet.Wpf/src/WpfGfx/core/hw/d3ddevicemanager.cpp#L1424-L1474)。
- D3D9 的 swap chain 描述里没有 DXGI `AlphaMode`。
- `WindowChrome` 没有设置任何叫作 `AlphaMode` 的交换链属性，也没有把窗口切换到 DirectComposition/DXGI flip-model composition swap chain。

### 概念上相似的部分

- 透明 clear color 使目标需要 Alpha 通道；
- `A8R8G8B8` 被 MIL 解释为 `PBGRA32bpp`；
- WPF 的 SrcOver blend 使用预乘 Alpha 公式；
- `AllowsTransparency=true` 时，`UpdateLayeredWindow` 使用 `AC_SRC_ALPHA` 消费这类预乘像素。

所以最准确的答案是：

> **没有设置 DXGI `AlphaMode.Premultiplied`；但 WindowChrome 的完整 glass 设置会让 WPF 使用能够保存预乘 Alpha 的 D3D9/MIL render target。只有 `AllowsTransparency=true` 的 application-managed layered 路径才明确通过 `UpdateLayeredWindow + AC_SRC_ALPHA` 把逐像素 Alpha 交给桌面。**

---

## 9. 建议的运行时验证

为了判断实际程序属于“情况 A”还是“情况 B”，应在设置样式后以及下一次 resize/render 后分别检查：

1. `GetWindowLongPtr(hwnd, GWL_EXSTYLE) & WS_EX_LAYERED` 是否仍非零；
2. `HwndSource.FromHwnd(hwnd).UsesPerPixelOpacity` 是否仍为 `false`；
3. 在 `HwndTarget.HandleMessage` 的 `WM_STYLECHANGING` 分支观察 `styleNew` 是否被清位；
4. 在 `HwndTarget.UpdateWindowSettings` 观察 `_usesPerPixelOpacity`、`flags` 和最终发送的 `MILWindowLayerType`；
5. 在 `WindowChromeWorker._ExtendGlassFrame` 确认透明 clear color 和完整 DWM margins 已应用。
6. 确认窗口内容实际输出了预期 Alpha，且没有被不透明背景覆盖；
7. 分别在切换 `WS_EX_LAYERED`、resize、最小化恢复和显示器切换后重新检查 HWND 样式与视觉结果。

如果最终 `WS_EX_LAYERED` 为零，而窗口仍透明，则可以直接确认透明来自 WindowChrome/DWM glass，而不是 layered window。如果该位持续存在但 `UsesPerPixelOpacity=false`，则程序已经进入 WPF 不保证的混合状态。

---

## 最终回答

- **这组 WindowChrome 设置会改变渲染：**它使窗口客户区覆盖全窗口，把 DWM frame 扩展到全窗口，将底层清屏色设为透明，并可能把 WpfGfx 渲染目标重建为带 Alpha 的 `A8R8G8B8/PBGRA32`。
- **它不会启用 WPF 的逐像素 layered-window 模式：**`MILTransparencyFlags` 仍为 `Opaque`，MIL layer type 仍为 `NotLayered`，正常呈现仍走 D3D9/GDI。
- **`WS_EX_LAYERED` 不是 `AllowsTransparency` 的等价替代：**标准 WPF 会主动清除外部添加的该样式。若它被绕过并持续存在，透明效果属于 User32/DWM 对非标准状态组合的处理，而不是 WPF 显式开启的 per-pixel alpha。
- **可以运行时切换透明：**在 `WM_NCCALCSIZE` 已使客户区覆盖整个窗口、WindowChrome 已建立带 Alpha 的渲染条件后，用公共 hook 保留 `WS_EX_LAYERED`，再通过添加或移除该样式位切换透明合成状态。`SetLayeredWindowAttributes(..., LWA_ALPHA)` 是另一种整窗常量 Alpha 机制，与这里的切换无关。
- **没有 DXGI `AlphaMode.Premultiplied`：**只有内部 D3D9/MIL 像素格式和 blend 采用预乘 Alpha 约定。明确的逐像素桌面合成只出现在 `AllowsTransparency=true` 对应的 `ApplicationManagedLayer + UpdateLayeredWindow + AC_SRC_ALPHA` 路径。
- **若实际效果只在添加 `WS_EX_LAYERED` 后出现：**先确认该样式位是否真的存活。若存活，则最后的视觉变化属于 WPF 源码之外的 User32/DWM 行为，不能据此推导 WPF 改了交换链 AlphaMode。
