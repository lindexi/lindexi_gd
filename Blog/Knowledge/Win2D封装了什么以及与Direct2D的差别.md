# Win2D 封装了什么，以及它和 Direct2D 的差别

本文结论来自对本仓库源码、IDL 和文档的对照，而不是对 Win2D 宣传文案的复述。

一句话先说清楚：**这个仓库里的 Win2D，不是“把 Direct2D 的 COM 接口逐个投影成 WinRT”。** 它是一层面向 C# / C++ / VB 和 XAML 的即时模式 2D API。底层确实以 Direct2D 为绘制核心，但真正包进去的是一整条 DirectX + 系统图形栈。

仓库自己的定位见 `README.md` 和 `winrt/docsrc/Introduction.aml`：

> Win2D is an easy-to-use Windows Runtime API for immediate mode 2D graphics rendering with GPU acceleration. It utilizes the power of Direct2D, and integrates seamlessly with XAML.

`winrt/docsrc/Interop.aml` 又补了一句：Win2D 是 Direct2D 之上的一层，并且**双向互通**，不是把 D2D 藏死。

---

## 1. 仓库结构已经说明它不是“只包 D2D”

核心实现在 `winrt/lib/`，按能力拆目录，而不是按 `ID2D1*` 接口拆目录：

| 目录 | 对应能力 |
|---|---|
| `drawing/` | 设备、绘制会话、swapchain、sprite batch |
| `geometry/`、`brushes/` | 几何与笔刷 |
| `images/` | 位图、RenderTarget、WIC 编解码 |
| `text/` | DirectWrite 文本 |
| `effects/` | D2D Effect + WinRT Effects 契约 |
| `xaml/` | XAML 控件与 SurfaceImageSource |
| `composition/` | Windows.UI.Composition 互操作 |
| `svg/` | SVG |
| `printing/` | 打印 |
| `directx/` | WinRT Direct3D 类型别名 |

如果只是 Direct2D 语言绑定，不会需要 `text/`、`xaml/`、`composition/`、`printing/` 这些独立子系统。

---

## 2. 它实际封装了什么

从 `winrt/lib/pch.h`、`winrt/lib/drawing/CanvasDevice.cpp`、`winrt/docsrc/Interop.aml` 可以把栈拆开。

### 2.1 GPU 底座：D3D11 + DXGI

`CanvasDevice::CreateNew()` 的创建顺序非常明确：

1. `D2D1CreateFactory(D2D1_FACTORY_TYPE_MULTI_THREADED)` 得到 `ID2D1Factory2`
2. `D3D11CreateDevice(..., D3D11_CREATE_DEVICE_BGRA_SUPPORT)` 得到 `ID3D11Device`
   - 先硬件，失败再 WARP
3. 取 `IDXGIDevice3`
4. `d2dFactory->CreateDevice(dxgiDevice)` 得到 `ID2D1Device1`

也就是说，**Win2D 设备不是“一个 D2D 对象”**，而是：

```text
ID3D11Device → IDXGIDevice3 → ID2D1Device1
```

对应代码在 `winrt/lib/drawing/CanvasDevice.cpp`：

- `DefaultDeviceAdapter::CreateD2DFactory()`
- `DefaultDeviceAdapter::TryCreateD3DDevice()`
- `CanvasDevice::MakeD3D11Device()`
- `CanvasDevice::CreateNew()`

对外，`CanvasDevice` 还实现了 WinRT 的 `IDirect3DDevice`，并能从已有 `IDirect3DDevice` 反建。见 `winrt/lib/drawing/CanvasDevice.abi.idl` 中的 `CreateFromDirect3D11Device`。

`CanvasSwapChain` 对应的是 `IDXGISwapChain1`，不是 D2D 类型。见 `winrt/docsrc/Interop.aml`。

### 2.2 2D 绘制核心：Direct2D

这部分才是大家常说的“Win2D ≈ D2D”。官方 interop 表写得很死：

| Win2D 类型 | 底层原生类型 |
|---|---|
| `CanvasDevice` | `ID2D1Device1` |
| `CanvasDrawingSession` | `ID2D1DeviceContext1` |
| `CanvasBitmap` | 无 `D2D1_BITMAP_OPTIONS_TARGET` 的 `ID2D1Bitmap1` |
| `CanvasRenderTarget` | 带 `D2D1_BITMAP_OPTIONS_TARGET` 的 `ID2D1Bitmap1` |
| `CanvasGeometry` | `ID2D1Geometry` 及其派生类型 |
| `CanvasCachedGeometry` | `ID2D1GeometryRealization` |
| `CanvasCommandList` | `ID2D1CommandList` |
| `CanvasSolidColorBrush` | `ID2D1SolidColorBrush` |
| `CanvasLinearGradientBrush` | `ID2D1LinearGradientBrush` |
| `CanvasRadialGradientBrush` | `ID2D1RadialGradientBrush` |
| `CanvasImageBrush` | `ID2D1BitmapBrush1` 或 `ID2D1ImageBrush` |
| `CanvasStrokeStyle` | `ID2D1StrokeStyle1` |
| `CanvasGradientMesh` | `ID2D1GradientMesh` |
| `CanvasSvgDocument` | `ID2D1SvgDocument` |
| `CanvasSpriteBatch` | 依赖 `ID2D1DeviceContext3` / `ID2D1SpriteBatch` |

高级功能会继续 QI 到更新的 D2D 接口，例如：

- `ID2D1DeviceContext2`：GradientMesh
- `ID2D1DeviceContext3`：SpriteBatch
- `ID2D1DeviceContext5` / `ID2D1Factory5`：部分新 effect、颜色管理
- `ID2D1SvgDocument`：SVG

`CanvasDrawingSession` 文档写明：GPU 真正干活发生在 session close，对应 D2D 的批处理 / `Flush`。见 `winrt/docsrc/CanvasDrawingSession.xml`。

### 2.3 文本：DirectWrite，不是 Direct2D

官方 interop 表把文本类型映射到 DirectWrite：

| Win2D 类型 | 底层原生类型 |
|---|---|
| `CanvasTextFormat` | `IDWriteTextFormat1` |
| `CanvasTextLayout` | `IDWriteTextLayout3` |
| `CanvasFontFace` | `IDWriteFontFaceReference` |
| `CanvasFontSet` | `IDWriteFontSet` |
| `CanvasTextRenderingParameters` | `IDWriteRenderingParams3` |
| `CanvasTypography` | `IDWriteTypography` |
| `CanvasNumberSubstitution` | `IDWriteNumberSubstitution` |

`winrt/lib/text/CustomFontManager.cpp` 直接调用 `DWriteCreateFactory`。

D2D 只负责把 glyph run 画出去；排版、字体枚举、度量、自定义 font loader 是另一套 API。`winrt/lib/pch.h` 同时包含了 `dwrite_2.h` 和 `dwrite_3.h`。

### 2.4 图像编解码：WIC

`winrt/lib/images/WicAdapter.h` 创建 `IWICImagingFactory2`。

位图加载 / 保存先走 WIC，再变成 `ID2D1Bitmap1`。这不是 Direct2D 自己的编解码能力。`CanvasDevice` 内部也有 `CreateBitmapFromWicResource(IWICBitmapSource*, ...)`。

### 2.5 特效：D2D Effect + WinRT Effects 契约

`winrt/lib/effects/ICanvasEffect.abi.idl` 里，`ICanvasEffect` 同时要求：

- `Windows.Graphics.Effects.IGraphicsEffect`
- `Microsoft.Graphics.Canvas.ICanvasImage`

底层包装的是 `ID2D1Effect`。`PixelShaderEffect` 还依赖 `d3dcompiler`，把自定义像素着色器做成 D2D custom effect。

也就是说特效层既接 D2D，也接 Composition 那套 `IGraphicsEffect` 图。仓库里大量 `winrt/lib/effects/generated/*` 就是把 D2D 内置 effect 生成 WinRT 类型。

### 2.6 显示 / UI：XAML 和 Composition

`CanvasControl` 并不自己开窗口画。它创建 `CanvasImageSource`，底层是 XAML 的 `SurfaceImageSource`，通过：

```text
ISurfaceImageSourceNativeWithD2D::BeginDraw()
```

拿到一个已经绑在图集表面（atlased surface）上的 `ID2D1DeviceContext`。见 `winrt/lib/xaml/CanvasImageSourceDrawingSessionAdapter.cpp`。

这一层自己处理了 D2D 不会替你处理的问题：

- 错误线程时，把 XAML 返回的无意义 `E_FAIL` 翻成 `RPC_E_WRONG_THREAD`
- 图集 offset / DPI，让调用方以为自己在画 `(0,0)`
- 必须 `Clear`，因为 XAML 不保证表面内容
- `EndDraw`

另外还有：

- `CanvasVirtualImageSource` → `VirtualSurfaceImageSource`
- `CanvasAnimatedControl` → 独立游戏循环线程
- `CanvasComposition` → `ICompositorInterop` / `ICompositionGraphicsDeviceInterop`
- `RecreatableDeviceManager` → 自动处理 device lost、DPI、共享设备

`CanvasComposition.cpp` 会把 `CanvasDevice` 里的 `ID2D1Device1` 交给 Composition 创建 `CompositionGraphicsDevice`。

### 2.7 其它系统图形 API

- 墨迹：`CanvasDrawingSession` 通过 `IInkD2DRenderer` / `InkD2DRenderer` 画 stylus ink
- 打印：`CanvasPrintDocument` 实现 `IPrintDocumentSource`、`IPrintDocumentPageSource`，并使用 `IPrintDocumentPackageTarget`
- 数学：`DirectXMath`

`winrt/lib/pch.h` 把这条栈写在一起：

- `d2d1_2.h` / `d2d1_3.h`
- `d3d11.h`
- `dwrite_2.h` / `dwrite_3.h`
- `dxgi1_3.h`
- `d3dcompiler.h`
- `wincodec.h`
- `inkrenderer.h`
- XAML dxinterop
- Composition interop

---

## 3. 和“直接封装 Direct2D”的差别

如果只做“Direct2D 的 WinRT 投影”，大概会得到一堆 `ID2D1Xxx` 的一对一包装。这个仓库刻意没这么做。

### 3.1 范围不同

Direct2D 只管 2D 栅格化。

一个能在 C# / XAML 里画出来的 GPU 2D 栈，还必须有：

- D3D11 设备怎么建、怎么丢、怎么共享
- DXGI swapchain
- DirectWrite 文本
- WIC 编解码
- XAML SurfaceImageSource / Composition 接入

Win2D 包的是**整条可用路径**，D2D 只是中间那截。

### 3.2 API 形状不同

`winrt/lib/drawing/CanvasDrawingSession.abi.idl` 写得很直白：D2D 有 `DrawBitmap` 和 `DrawImage` 两套，前者更快但只能画 bitmap。Win2D 对外只留一个 `DrawImage`：

- 能走 `DrawBitmap` 就走快路径
- 否则退回 `DrawImage`
- 必要时还自动插 opacity effect

同类设计还有：

- `WithColor` / `WithBrush` 大量重载，C# 不用先建 brush
- 默认文字抗锯齿改成 `GRAYSCALE`，`CanvasDrawingSession.cpp` 注释写明：`Win2D wants a different text antialiasing default vs. native D2D`
- DPI / DIP 转换，以及 `ICanvasResourceCreator` / `ICanvasResourceCreatorWithDpi`
- `IClosable` 生命周期，而不是自己管 COM 引用计数

所以它不是薄壳，是**重新设计过的 WinRT API**，底层碰巧用 D2D 实现。

### 3.3 运行时策略不同

这些是应用框架，不是 D2D 接口映射：

- `GetSharedDevice` 会从池里拿设备，发现已经 lost 就重建
- XAML 控件会自己抓 `CreateResources` / `Update` / `Draw` 里的 device lost，再抛 `CreateResourcesReason.NewDevice`
- `DeviceContextPool` 复用 `ID2D1DeviceContext1`
- `ResourceWrapper` 维护“一个原生对象对应一个 WinRT 包装”

见 `winrt/docsrc/HandlingDeviceLost.aml`、`winrt/lib/xaml/RecreatableDeviceManager.h`、`winrt/lib/utils/ResourceWrapper.h`。

### 3.4 互操作模型不同

`winrt/docsrc/Interop.aml` 和 `winrt/published/Microsoft.Graphics.Canvas.native.h` 提供：

- `GetWrappedResource<T>(wrapper)`：从 Win2D 拿到原生 D2D / DWrite / DXGI 对象
- `GetOrCreate<T>(resource)`：从原生对象拿到或创建 Win2D 包装

官方定位是：大部分时候用 Win2D，需要第三方原生组件或更底层控制时再掉下去。  
如果只投影 D2D，C# 仍然碰不到 DWrite / WIC / XAML 那几段。

---

## 4. 为什么选用 Win2D 这条封装，而不是只包 Direct2D

仓库里的动机不是一句宣传，而是实现结构逼出来的。

### 4.1 Direct2D 在这个产品目标下不够用

目标写在 `README.md`、`Introduction.aml`、`Features.aml`：给 C# / C++ / VB 的 UWP / WinUI 应用一个好用的即时模式 GPU 2D API，并且无缝进 XAML。

只包 `ID2D1*`，C# 开发者仍然得自己：

- 正确创建带 `BGRA_SUPPORT` 的 D3D11 设备
- 从 DXGI 创建 D2D device
- 用 WIC 读图
- 用 DWrite 排版
- 对接 `ISurfaceImageSourceNativeWithD2D` 的图集偏移和线程模型
- 处理 device lost
- 把 effect 接到 `IGraphicsEffect` 才能和 Composition 一起用

这些恰好就是仓库里 `drawing/`、`text/`、`images/`、`xaml/`、`composition/`、`effects/`、`printing/` 分开存在的原因。

### 4.2 WinRT 投影的对象是“应用语义”，不是“COM 接口表”

下面这些类型在 D2D 里没有对应物：

- `CanvasControl`
- `CanvasAnimatedControl`
- `CanvasVirtualControl`
- `CanvasImageSource`
- `CanvasPrintDocument`
- `PixelShaderEffect`
- `CanvasComposition`

它们是产品，不是 wrapper。

### 4.3 D2D 仍然被当成可互操作的实现细节留下

官方 interop 文档的定位很清楚：

- 你可以写一个大部分用 Win2D 的应用，局部掉到原生 DirectX
- 也可以写一个大部分原生 DirectX 的应用，局部用 Win2D 换便利或 C# 支持

这说明作者认为价值在上层便利和 XAML 集成，不在把 D2D 藏起来，也不在做纯投影。

---

## 5. 对照总结

| 问题 | 结论 |
|---|---|
| Win2D 是不是 Direct2D？ | 不是。Direct2D 是它的 2D 栅格化引擎。 |
| Win2D 是不是 Direct2D 的一对一包装？ | 不是。API 形状、默认值和生命周期都被重新设计过。 |
| Win2D 实际包了什么？ | D3D11、DXGI、Direct2D、DirectWrite、WIC、D2D Effects、XAML dxinterop、Composition、Ink、Printing。 |
| 和直接封装 Direct2D 差在哪？ | 范围更宽，面向 WinRT/XAML 应用，而不是面向 COM 接口表。 |
| 为什么这样封装？ | 只包 D2D 仍然无法在 C# / XAML 里独立完成“创建设备、读图、排字、上屏、处理设备丢失”。 |

最后可以压成一句：

- **Direct2D**：原生 COM 的 2D 光栅化引擎
- **Win2D**：WinRT 应用框架。用 D3D11 / DXGI 当设备，D2D 当画笔，DWrite 排字，WIC 编解码，XAML / Composition 负责显示

所以这个仓库选择封装的是“能在 WinUI / UWP 里直接用的 2D 图形运行时”，不是“Direct2D 的语言绑定”。
