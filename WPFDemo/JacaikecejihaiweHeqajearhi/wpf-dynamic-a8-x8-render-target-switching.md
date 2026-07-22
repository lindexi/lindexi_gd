# WPF 同一 HWND 动态切换 A8 与 X8 渲染目标

## 结论

本文直接采用以下工程定义：

- `D3DFMT_A8R8G8B8` 等价于 WPF 窗口使用逐像素 Alpha 合成；
- `D3DFMT_X8R8G8B8` 等价于 WPF 窗口使用不透明合成。

目标不是让窗口视觉上暂时变得不透明，而是在业务不需要透明时真正关闭 Alpha 合成：

```text
透明模式：D3DFMT_A8R8G8B8 + WS_EX_LAYERED + 每像素 Alpha 合成
不透明模式：D3DFMT_X8R8G8B8 + 无 WS_EX_LAYERED + 普通不透明合成
```

这样做是为了降低稳定运行阶段的 DWM 合成开销，尤其是 4K 大窗口场景。切换本身是低频操作，可以接受反射、channel 同步和 render target 重建的一次性成本。

关键结论如下：

1. 透明 clear color 可以让当前 `CSlaveHWndRenderTarget` 增加 `NeedDestinationAlpha`，从 X8 单向升级到 A8。
2. 把 clear color 恢复为不透明不会清除 `NeedDestinationAlpha`，因此不能仅靠背景色、移除 `WindowChrome` 或清除 `WS_EX_LAYERED` 回到 X8。
3. A8 回到 X8 时，必须销毁当前原生 `CSlaveHWndRenderTarget`，再为同一个 HWND 创建新的 UCE target。
4. WPF 已经有完整的内部 release/create 生命周期。上层业务可以通过反射调用 `MediaContext.UnregisterICompositionTarget` 和 `RegisterICompositionTarget` 复用它。
5. 不应反射修改 `HwndTarget.UsesPerPixelOpacity`。将它设为 `true` 会进入 `ApplicationManagedLayer + UpdateLayeredWindow`，也就是需要排除的透明窗口呈现路径。这里排除的不只是 `AllowsTransparency` 这个公开属性，而是它背后的整条呈现方式。

推荐路线是：

> **保持 `HwndTarget.UsesPerPixelOpacity=false` 和现有 HWND 不变。X8→A8 时通过完整 `WindowChrome` 的透明 clear color 升级目标，再启用 `WS_EX_LAYERED`；A8→X8 时先撤销 layered 样式、DWM frame 和透明 clear color，然后通过反射注销并重新注册现有 `HwndTarget`，让 WPF 删除旧 UCE target 并创建新的 X8 target。**

本文不使用 `SetLayeredWindowAttributes(..., LWA_ALPHA)`。透明度始终来自窗口内容中每个像素独立的 Alpha。

---

## 1. 为什么必须重建 UCE target

### 1.1 X8 到 A8 是现有源码支持的单向升级

`HwndTarget.BackgroundColor` 改变时会向 MIL 发送 `SetClearColor`：

- [`HwndTarget.cs:2348-2385`](../src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/InterOp/HwndTarget.cs#L2348-L2385)

透明 clear color 到达渲染线程后，`CSlaveHWndRenderTarget::ProcessSetClearColor` 会把 `NeedDestinationAlpha` 加入 `m_UCETargetFlags`：

- [`hwndtarget.cpp:656-677`](../src/Microsoft.DotNet.Wpf/src/WpfGfx/core/uce/hwndtarget.cpp#L656-L677)

flags 改变会释放现有底层 render target，并在下一次渲染时按新 flags 创建目标：

- [`hwndtarget.cpp:759-837`](../src/Microsoft.DotNet.Wpf/src/WpfGfx/core/uce/hwndtarget.cpp#L759-L837)
- [`hwndtarget.cpp:842-889`](../src/Microsoft.DotNet.Wpf/src/WpfGfx/core/uce/hwndtarget.cpp#L842-L889)

硬件路径根据该标志选择格式：

- 有 `NeedDestinationAlpha`：`D3DFMT_A8R8G8B8`；
- 没有 `NeedDestinationAlpha`：`D3DFMT_X8R8G8B8`。

参见 [`d3ddevicemanager.cpp:605-628`](../src/Microsoft.DotNet.Wpf/src/WpfGfx/core/hw/d3ddevicemanager.cpp#L605-L628)。

因此 X8→A8 不需要销毁整个 UCE composition target。透明 clear color 已经能够完成升级。

### 1.2 A8 到 X8 没有对称的 clear-color 路径

`ProcessSetClearColor` 没有在 clear color Alpha 恢复为 1 时清除 `NeedDestinationAlpha`。

执行：

```text
Colors.Transparent -> SystemColors.WindowColor
```

只会改变清屏颜色和重绘内容。当前 `CSlaveHWndRenderTarget` 仍保留 `NeedDestinationAlpha`，后续重建的底层 D3D target 仍然会选择 A8。

所以以下状态不满足业务要求：

```text
D3DFMT_A8R8G8B8 + 所有输出像素 Alpha=1
```

真正需要的是：

```text
销毁带 NeedDestinationAlpha 的 CSlaveHWndRenderTarget
创建不带 NeedDestinationAlpha 的新 CSlaveHWndRenderTarget
选择 D3DFMT_X8R8G8B8
```

### 1.3 新 UCE target 会从不带 Alpha 要求的初始状态开始

`CSlaveHWndRenderTarget` 构造时把 `m_UCETargetFlags` 和 `m_RenderTargetFlags` 初始化为 `MilRTInitialization::Null`，clear color Alpha 初始为 1：

- [`hwndtarget.cpp:42-70`](../src/Microsoft.DotNet.Wpf/src/WpfGfx/core/uce/hwndtarget.cpp#L42-L70)

`ProcessCreate` 随后才接收托管端发送的初始 flags：

- [`hwndtarget.cpp:393-419`](../src/Microsoft.DotNet.Wpf/src/WpfGfx/core/uce/hwndtarget.cpp#L393-L419)

托管端 `HwndInitialize` 的初始命令不包含 `NeedDestinationAlpha`，并且初始 clear color Alpha 为 1：

- [`exports.cs:2164-2260`](../src/Microsoft.DotNet.Wpf/src/Common/Graphics/exports.cs#L2164-L2260)

因此，在重新创建 UCE target 之前，只要托管 `HwndTarget.BackgroundColor` 已恢复为不透明色，新 target 就不会重新获得 `NeedDestinationAlpha`，硬件路径将选择 X8。

---

## 2. 为什么反射注销和重新注册能够保持 HWND

### 2.1 注销会结束旧原生 target 的生命周期

`MediaContext.UnregisterICompositionTarget` 最终调用 `HwndTarget.ReleaseUCEResources`：

- [`MediaContext.cs:1497-1541`](../src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/Media/MediaContext.cs#L1497-L1541)
- [`HwndTarget.cs:808-843`](../src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/InterOp/HwndTarget.cs#L808-L843)

`ReleaseUCEResources` 会：

1. 清除 composition target root；
2. 释放 `_compositionTarget` 在主 channel 上的引用；
3. 释放它在 out-of-band channel 上的引用；
4. 释放 world transform、visual root 和其他 UCE 资源。

`MultiChannelResource.ReleaseOnChannel` 在资源不再被 channel 使用时移除对应 handle：

- [`exports.cs:1482-1580`](../src/Microsoft.DotNet.Wpf/src/Common/Graphics/exports.cs#L1482-L1580)

原生侧处理 `TYPE_HWNDRENDERTARGET` 删除命令时，会把 target 从 render target manager 移除并删除资源 handle：

- [`composition.cpp:1362-1436`](../src/Microsoft.DotNet.Wpf/src/WpfGfx/core/uce/composition.cpp#L1362-L1436)
- [`composition.cpp:2423-2473`](../src/Microsoft.DotNet.Wpf/src/WpfGfx/core/uce/composition.cpp#L2423-L2473)

`CSlaveHWndRenderTarget` 析构时释放底层 render target：

- [`hwndtarget.cpp:63-70`](../src/Microsoft.DotNet.Wpf/src/WpfGfx/core/uce/hwndtarget.cpp#L63-L70)

这会结束旧原生对象的生命周期，旧对象累计的 `NeedDestinationAlpha` 随之消失。

### 2.2 重新注册会为同一个 HWND 创建新 target

`MediaContext.RegisterICompositionTarget` 最终调用 `HwndTarget.CreateUCEResources`：

- [`MediaContext.cs:1456-1495`](../src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/Media/MediaContext.cs#L1456-L1495)
- [`HwndTarget.cs:711-806`](../src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/InterOp/HwndTarget.cs#L711-L806)

它会重新创建 `TYPE_HWNDRENDERTARGET`，并再次把原来的 `_hWnd` 传给 `HwndInitialize`。原生 resource factory 为该类型创建新的 `CSlaveHWndRenderTarget`：

- [`generated_resource_factory.cpp:145-165`](../src/Microsoft.DotNet.Wpf/src/WpfGfx/core/uce/generated_resource_factory.cpp#L145-L165)

此过程不会销毁或替换 HWND。窗口句柄、owner、外部句柄引用和原生窗口生命周期保持不变；变化的只有 HWND 对应的 WPF/MIL UCE render target。

---

## 3. 不要修改 `HwndTarget.UsesPerPixelOpacity`

`HwndTarget.UsesPerPixelOpacity` 的 internal setter 会调用 `UpdateWindowSettings`：

- [`HwndTarget.cs:2448-2475`](../src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/InterOp/HwndTarget.cs#L2448-L2475)

当它为 `true` 时，WPF 会发送：

- `MILTransparencyFlags.PerPixelAlpha`；
- `MILWindowLayerType.ApplicationManagedLayer`。

参见 [`HwndTarget.cs:2147-2248`](../src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/InterOp/HwndTarget.cs#L2147-L2248)。

`ApplicationManagedLayer` 会令 WpfGfx 选择 `PresentUsingUpdateLayeredWindow`：

- [`api_factory.cpp:518-568`](../src/Microsoft.DotNet.Wpf/src/WpfGfx/core/api/api_factory.cpp#L518-L568)

这正是透明窗口公开入口背后的呈现路径。仅仅不调用公开属性、改为反射 internal setter，并不会改变其渲染性能特征。

因此本文排除的是这整条路径，而不只是排除某个公开属性名称。动态切换过程中应始终保持：

```text
HwndTarget.UsesPerPixelOpacity = false
```

本文透明模式使用的是：

```text
WindowChrome 透明 clear color
+ NeedDestinationAlpha
+ D3DFMT_A8R8G8B8
+ 外部保留的 WS_EX_LAYERED
```

---

## 4. 需要反射的内部成员

`HwndSource.CompositionTarget` 是公开属性，可以直接获得现有 `HwndTarget`：

- [`HwndSource.cs:691-711`](../src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/InterOp/HwndSource.cs#L691-L711)

需要从 PresentationCore 反射以下成员：

| 类型 | 成员 | 用途 |
|---|---|---|
| `MediaContext` | `From(Dispatcher)` | 获得窗口 Dispatcher 对应的 media context |
| `MediaContext` | `CompleteRender()` | 等待已有渲染和主 channel 命令完成 |
| `MediaContext` | `GetChannels()` | 获得主 channel 和 out-of-band channel |
| `MediaContext` | `UnregisterICompositionTarget(...)` | 释放现有 `HwndTarget` 的 UCE 资源 |
| `MediaContext` | `RegisterICompositionTarget(...)` | 为同一个 `HwndTarget` 重新创建 UCE 资源 |
| `MediaContext` | `PostRender()` | 安排完整重绘 |
| `HwndTarget` | `UpdateWindowSettings(bool)` | 重建前停用、重建后重新启用 target |
| `ChannelSet` | `Channel` / `OutOfBandChannel` | 取得两个 channel |
| `Channel` | `CloseBatch()` / `Commit()` / `SyncFlush()` | 确保删除或创建命令已经执行 |

相关源码：

- [`MediaContext.cs:1361-1375`](../src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/Media/MediaContext.cs#L1361-L1375)
- [`MediaContext.cs:1456-1541`](../src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/Media/MediaContext.cs#L1456-L1541)
- [`MediaContext.cs:2200-2279`](../src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/Media/MediaContext.cs#L2200-L2279)
- [`MediaContext.cs:2582-2592`](../src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/Media/MediaContext.cs#L2582-L2592)
- [`exports.cs:300-320`](../src/Microsoft.DotNet.Wpf/src/Common/Graphics/exports.cs#L300-L320)
- [`exports.cs:360-420`](../src/Microsoft.DotNet.Wpf/src/Common/Graphics/exports.cs#L360-L420)

反射结果应在应用初始化时一次性解析并缓存。切换时不应重复扫描类型和成员。

---

## 5. 反射封装示例

以下示例只负责销毁旧 UCE target，并为同一个 HWND 创建新的不透明 target。调用它之前，业务层必须先撤销 `WS_EX_LAYERED`、透明 clear color 和 DWM glass。

代码只使用 .NET Framework 4.7.2 与现代 .NET 都具备的反射 API。

```csharp
using System;
using System.Reflection;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

internal sealed class WpfHwndTargetRecreator
{
    private const BindingFlags StaticNonPublic =
        BindingFlags.Static | BindingFlags.NonPublic;

    private const BindingFlags InstanceNonPublic =
        BindingFlags.Instance | BindingFlags.NonPublic;

    private readonly Type _mediaContextType;
    private readonly Type _compositionTargetInterfaceType;
    private readonly MethodInfo _fromMethod;
    private readonly MethodInfo _completeRenderMethod;
    private readonly MethodInfo _getChannelsMethod;
    private readonly MethodInfo _registerMethod;
    private readonly MethodInfo _unregisterMethod;
    private readonly MethodInfo _postRenderMethod;
    private readonly MethodInfo _updateWindowSettingsMethod;
    private readonly FieldInfo _channelField;
    private readonly FieldInfo _outOfBandChannelField;
    private readonly MethodInfo _closeBatchMethod;
    private readonly MethodInfo _commitMethod;
    private readonly MethodInfo _syncFlushMethod;

    public WpfHwndTargetRecreator()
    {
        Assembly presentationCore = typeof(CompositionTarget).Assembly;

        _mediaContextType = GetRequiredType(
            presentationCore,
            "System.Windows.Media.MediaContext");

        _compositionTargetInterfaceType = GetRequiredType(
            presentationCore,
            "System.Windows.Media.ICompositionTarget");

        _fromMethod = GetRequiredMethod(
            _mediaContextType,
            "From",
            StaticNonPublic,
            new[] { typeof(Dispatcher) });

        _completeRenderMethod = GetRequiredMethod(
            _mediaContextType,
            "CompleteRender",
            InstanceNonPublic,
            Type.EmptyTypes);

        _getChannelsMethod = GetRequiredMethod(
            _mediaContextType,
            "GetChannels",
            InstanceNonPublic,
            Type.EmptyTypes);

        _registerMethod = GetRequiredMethod(
            _mediaContextType,
            "RegisterICompositionTarget",
            StaticNonPublic,
            new[] { typeof(Dispatcher), _compositionTargetInterfaceType });

        _unregisterMethod = GetRequiredMethod(
            _mediaContextType,
            "UnregisterICompositionTarget",
            StaticNonPublic,
            new[] { typeof(Dispatcher), _compositionTargetInterfaceType });

        _postRenderMethod = GetRequiredMethod(
            _mediaContextType,
            "PostRender",
            InstanceNonPublic,
            Type.EmptyTypes);

        _updateWindowSettingsMethod = GetRequiredMethod(
            typeof(HwndTarget),
            "UpdateWindowSettings",
            InstanceNonPublic,
            new[] { typeof(bool) });

        Type channelSetType = _getChannelsMethod.ReturnType;
        _channelField = GetRequiredField(channelSetType, "Channel");
        _outOfBandChannelField = GetRequiredField(
            channelSetType,
            "OutOfBandChannel");

        Type channelType = _channelField.FieldType;
        _closeBatchMethod = GetRequiredMethod(
            channelType,
            "CloseBatch",
            InstanceNonPublic,
            Type.EmptyTypes);

        _commitMethod = GetRequiredMethod(
            channelType,
            "Commit",
            InstanceNonPublic,
            Type.EmptyTypes);

        _syncFlushMethod = GetRequiredMethod(
            channelType,
            "SyncFlush",
            InstanceNonPublic,
            Type.EmptyTypes);
    }

    public void RecreateOpaqueTarget(HwndSource source)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        Dispatcher dispatcher = source.Dispatcher;
        dispatcher.VerifyAccess();

        HwndTarget target = source.CompositionTarget;
        if (target == null)
        {
            throw new InvalidOperationException(
                "The HwndSource does not have an active HwndTarget.");
        }

        object mediaContext = InvokeRequired(
            _fromMethod,
            null,
            new object[] { dispatcher });

        InvokeVoid(_completeRenderMethod, mediaContext, null);

        object channelSet = InvokeRequired(
            _getChannelsMethod,
            mediaContext,
            null);

        object channel = GetRequiredValue(_channelField, channelSet);
        object outOfBandChannel = GetRequiredValue(
            _outOfBandChannelField,
            channelSet);

        bool isRegistered = true;

        try
        {
            InvokeVoid(
                _updateWindowSettingsMethod,
                target,
                new object[] { false });

            InvokeVoid(
                _unregisterMethod,
                null,
                new object[] { dispatcher, target });

            isRegistered = false;

            // ReleaseUCEResources 先向主 channel 写入释放命令，
            // 再向 out-of-band channel 写入释放命令。
            Flush(channel);
            Flush(outOfBandChannel);

            InvokeVoid(
                _registerMethod,
                null,
                new object[] { dispatcher, target });

            isRegistered = true;

            // CreateUCEResources 先通过 out-of-band channel 创建资源，
            // 再通过主 channel 初始化 HWND target 和视觉树。
            Flush(outOfBandChannel);
            Flush(channel);
        }
        finally
        {
            if (!isRegistered)
            {
                InvokeVoid(
                    _registerMethod,
                    null,
                    new object[] { dispatcher, target });

                Flush(outOfBandChannel);
                Flush(channel);
            }

            InvokeVoid(
                _updateWindowSettingsMethod,
                target,
                new object[] { true });

            InvokeVoid(_postRenderMethod, mediaContext, null);
            InvokeVoid(_completeRenderMethod, mediaContext, null);
        }
    }

    private void Flush(object channel)
    {
        InvokeVoid(_closeBatchMethod, channel, null);
        InvokeVoid(_commitMethod, channel, null);
        InvokeVoid(_syncFlushMethod, channel, null);
    }

    private static Type GetRequiredType(
        Assembly assembly,
        string typeName)
    {
        Type type = assembly.GetType(typeName, false);
        if (type == null)
        {
            throw new NotSupportedException(
                $"The current WPF runtime does not contain {typeName}.");
        }

        return type;
    }

    private static MethodInfo GetRequiredMethod(
        Type type,
        string methodName,
        BindingFlags bindingFlags,
        Type[] parameterTypes)
    {
        MethodInfo method = type.GetMethod(
            methodName,
            bindingFlags,
            null,
            parameterTypes,
            null);

        if (method == null)
        {
            throw new NotSupportedException(
                $"The current WPF runtime does not contain " +
                $"{type.FullName}.{methodName}.");
        }

        return method;
    }

    private static FieldInfo GetRequiredField(
        Type type,
        string fieldName)
    {
        FieldInfo field = type.GetField(
            fieldName,
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic);

        if (field == null)
        {
            throw new NotSupportedException(
                $"The current WPF runtime does not contain " +
                $"{type.FullName}.{fieldName}.");
        }

        return field;
    }

    private static object GetRequiredValue(
        FieldInfo field,
        object instance)
    {
        object value = field.GetValue(instance);
        if (value == null)
        {
            throw new InvalidOperationException(
                $"{field.DeclaringType?.FullName}.{field.Name} is null.");
        }

        return value;
    }

    private static object InvokeRequired(
        MethodInfo method,
        object instance,
        object[] arguments)
    {
        object value = method.Invoke(instance, arguments);
        if (value == null)
        {
            throw new InvalidOperationException(
                $"{method.DeclaringType?.FullName}.{method.Name} returned null.");
        }

        return value;
    }

    private static void InvokeVoid(
        MethodInfo method,
        object instance,
        object[] arguments)
    {
        method.Invoke(instance, arguments);
    }
}
```

这个示例重点表达正确的内部调用顺序，不应直接视为已经覆盖所有异常场景的通用库。产品实现还应：

- 解包 `TargetInvocationException` 并保留内部异常；
- 在恢复失败时同时记录原始异常和恢复异常；
- 防止重复注册或重复注销；
- 在 Dispatcher shutdown、channel 断开或窗口关闭时取消切换；
- 通过状态机禁止两个切换同时执行。

---

## 6. X8→A8 的开启顺序

X8 到 A8 不需要执行 UCE target 注销和重新注册。推荐顺序如下。

### 6.1 安装并启用样式 hook

先通过 `HwndSource.AddHook` 安装 `WM_STYLECHANGING` hook，并把内部状态切换为“保留 `WS_EX_LAYERED`”。

hook 必须先安装，因为 `SetWindowLongPtr` 会同步触发 `WM_STYLECHANGING`；若没有 hook，WPF 内部 `HwndTarget` 会清除外部添加的 `WS_EX_LAYERED`。

具体 hook 实现见：

- [`windowchrome-layered-rendering-analysis.md`](windowchrome-layered-rendering-analysis.md)

### 6.2 准备 A8 target

1. 把业务内容切换为需要的透明像素；
2. 安装完整 glass 的 `WindowChrome`；
3. 确保 `Window.Background`、模板和根元素没有不透明画刷覆盖需要透明的区域；
4. 等待一次渲染，或反射调用 `MediaContext.CompleteRender()`。

完整 glass 会把 `CompositionTarget.BackgroundColor` 设为透明并扩展 DWM frame：

- [`WindowChromeWorker.cs:932-1010`](../src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Shell/WindowChromeWorker.cs#L932-L1010)

透明 clear color 到达渲染线程后，目标会增加 `NeedDestinationAlpha` 并重建为 A8。

### 6.3 最后添加 `WS_EX_LAYERED`

确认 A8 target 已准备好后，再调用 `SetWindowLongPtr` 添加 `WS_EX_LAYERED`。

最终状态应为：

```text
HwndTarget.UsesPerPixelOpacity = false
NeedDestinationAlpha = true
D3DFMT_A8R8G8B8
WS_EX_LAYERED = true
clear color = Transparent
DWM frame 已扩展
```

---

## 7. A8→X8 的关闭顺序

A8 到 X8 是核心路径。顺序不能反过来，也不能省略 UCE target 重建。

### 7.1 先恢复不透明窗口状态

1. 把业务内容切换为完全不透明的安全帧；
2. 停止 hook 对 `WS_EX_LAYERED` 的强制保留；
3. 调用 `SetWindowLongPtr` 清除 `WS_EX_LAYERED`；
4. 确认样式位已经清除；
5. 调用 `WindowChrome.SetWindowChrome(window, null)`；
6. 等待渲染完成。

移除 `WindowChrome` 会：

- 把 `CompositionTarget.BackgroundColor` 恢复为 `SystemColors.WindowColor`；
- 使用零 margins 撤销 DWM frame 扩展。

参见 [`WindowChromeWorker.cs:1140-1159`](../src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Shell/WindowChromeWorker.cs#L1140-L1159)。

仅把 `GlassFrameThickness` 改成零不够。`_UpdateFrameState` 的非 glass 分支不会恢复 clear color，也不会提交零 margins：

- [`WindowChromeWorker.cs:711-736`](../src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Shell/WindowChromeWorker.cs#L711-L736)

如果不透明模式仍需要自定义非客户区，可以先把 `WindowChrome` 设为 `null` 完成恢复和 target 重建，再安装一个 `GlassFrameThickness=0` 的非透明 `WindowChrome`。

### 7.2 在 Dispatcher 空闲阶段重建 target

不要从以下位置执行：

- `CompositionTarget.Rendering`；
- 布局回调；
- 窗口消息 hook 的同步处理过程；
- 正在关闭窗口或 Dispatcher shutdown 的过程。

`MediaContext.Render` 会直接枚举 `_registeredICompositionTargets`：

- [`MediaContext.cs:1990-2044`](../src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/Media/MediaContext.cs#L1990-L2044)

因此应在窗口 Dispatcher 上，以 `DispatcherPriority.ContextIdle` 安排重建：

```csharp
await window.Dispatcher.InvokeAsync(
    () => recreator.RecreateOpaqueTarget(hwndSource),
    DispatcherPriority.ContextIdle);
```

### 7.3 重建内部顺序

`RecreateOpaqueTarget` 内部执行：

```text
CompleteRender
UpdateWindowSettings(false)
UnregisterICompositionTarget
flush 主 channel
flush out-of-band channel
RegisterICompositionTarget
flush out-of-band channel
flush 主 channel
UpdateWindowSettings(true)
PostRender
CompleteRender
```

`UpdateWindowSettings(false)` 会通过 out-of-band channel 停止 render target，并同步等待渲染线程处理：

- [`HwndTarget.cs:2108-2134`](../src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/InterOp/HwndTarget.cs#L2108-L2134)
- [`HwndTarget.cs:2258-2280`](../src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/InterOp/HwndTarget.cs#L2258-L2280)

旧 target 的删除命令完成后才能注册新 target。否则删除和创建命令可能交错，无法保证旧 `NeedDestinationAlpha` 已经随原生对象销毁。

最终状态应为：

```text
HwndTarget.UsesPerPixelOpacity = false
NeedDestinationAlpha = false
D3DFMT_X8R8G8B8
WS_EX_LAYERED = false
clear color alpha = 1
DWM glass margins = 0
```

---

## 8. 业务层状态机

不要让调用方直接随意组合样式、`WindowChrome` 和 UCE target 操作。应提供单一状态机：

```text
OpaqueX8
PreparingAlpha
TransparentA8
PreparingOpaque
Faulted
```

建议约束：

- `OpaqueX8` 才能进入 `PreparingAlpha`；
- `TransparentA8` 才能进入 `PreparingOpaque`；
- 切换期间拒绝新的切换请求，或只记录最终期望状态；
- 任意失败进入 `Faulted`，完成恢复和重新验证后才能继续；
- 窗口关闭后永久停止状态机；
- 缓存 HWND，并在每次切换前确认 `HwndSource.Handle` 没有变化；
- 每次 A8→X8 后验证 `WS_EX_LAYERED` 已清除；
- 每次 X8→A8 后验证 hook 确实保留了 `WS_EX_LAYERED`。

切换频率低时，没有必要为合并或节流反射调用做复杂优化。首先保证状态一致和异常可恢复。

---

## 9. 反射风险与最小框架修改

这条路线依赖 PresentationCore 内部实现，不属于公开兼容性契约。应用启动时应一次性验证：

- `MediaContext` 和 `ICompositionTarget` 类型存在；
- 所有目标方法的名称和参数签名匹配；
- `ChannelSet.Channel` 和 `OutOfBandChannel` 字段存在；
- `Channel.CloseBatch`、`Commit` 和 `SyncFlush` 存在；
- 当前 WPF 版本允许同一个未 disposed 的 `HwndTarget` 被注销后重新注册。

任何成员不匹配时，应禁用动态切换并记录准确的 WPF 程序集版本，不要模糊匹配重载。

如果可以维护自编译 WPF，最小修改不是暴露 `UsesPerPixelOpacity`，而是在 `HwndTarget` 或 `MediaContext` 内新增一个协调方法，例如：

```text
RecreateUCEResourcesForOpaqueTarget
```

该方法内部复用本文的停用、注销、flush、注册、启用和异常恢复顺序。这样上层业务只需调用一个稳定入口，也无需反射 `ChannelSet` 和 `Channel`。

---

## 10. 性能验证

源码能够证明目标格式和资源生命周期变化，但不能单靠静态分析量化 4K 场景收益。需要对 A8 与 X8 的稳定运行阶段分别采样。

建议记录：

- DWM 进程的 GPU engine 使用率和 GPU 时间；
- 应用进程 GPU 使用率；
- present 次数、present technique 和帧率；
- 4K 大窗口下的显存带宽压力；
- 静态窗口与持续动画窗口；
- 单个窗口与多个透明窗口；
- A8 和 X8 模式下的功耗；
- 切换瞬间的 UI stall、黑帧、闪烁和完整重绘时间。

源码调试时应观察：

- `CSlaveHWndRenderTarget::m_UCETargetFlags`；
- `NeedDestinationAlpha`；
- `ChooseTargetFormat` 的输出；
- 实际创建的 D3D9 target format；
- A8→X8 时是否析构旧 `CSlaveHWndRenderTarget` 并创建了新实例。

至少测试：

- 多次 X8→A8→X8 往返；
- 最大化、最小化、隐藏和恢复；
- 多显示器与不同 DPI；
- 4K 显示器；
- DWM composition 状态变化；
- 锁屏、远程桌面和显示设备重建；
- 软件渲染回退；
- 切换期间关闭窗口；
- Dispatcher shutdown；
- 反射成员缺失或调用异常时的恢复。

---

## 11. 最终实施路线

### 开启透明：X8→A8

```text
安装并启用 WM_STYLECHANGING hook
准备透明业务内容
安装完整 WindowChrome
等待透明 clear color 触发 NeedDestinationAlpha
确认 A8 target 已建立
添加 WS_EX_LAYERED
```

### 关闭透明：A8→X8

```text
准备完全不透明的安全帧
停止 hook 保留 WS_EX_LAYERED
清除 WS_EX_LAYERED
移除 WindowChrome，恢复不透明 clear color 和零 DWM margins
等待当前渲染完成
停用 HwndTarget
反射注销 HwndTarget
flush 两个 channel，确认旧 CSlaveHWndRenderTarget 已删除
反射重新注册同一个 HwndTarget
flush 创建命令
重新启用 HwndTarget
完整重绘
确认新 target 为 D3DFMT_X8R8G8B8
```

这条路线保留同一个 HWND，不进入 `UpdateLayeredWindow`，并在不透明阶段真正移除目标 Alpha，而不是只把 A8 内容绘制成不透明。
