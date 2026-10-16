# MediaFoundation.Core

独立 `net10.0-windows` 库，不引用 WPF。使用 Windows MFPlay 的原生音频输出、视频 HWND 渲染器和播放时钟，不使用 SourceReader 视频帧模拟播放。CsWin32 0.3.183 以 PrivateAssets=all 引用；生成 SDK 接口、GUID、事件结构及 callback Vtbl。支持 x86/x64 宿主，不要求宿主启用 unsafe。

## 接入约定

- 命名空间 `MediaFoundation.Core`，主入口 `public sealed MediaPlaybackSession : IDisposable`。
- 在 WPF Dispatcher STA 且 `SynchronizationContext.Current` 已安装时构造，传入 HwndHost 的有效子窗口句柄。所有调用、属性访问与 Dispose 均在该线程。宿主必须持续泵消息，不可同步等待 MediaOpened。
- `Open(Uri)` 请求异步解析；`MediaOpened` 后才调用播放控制。不会自动播放。可在 MediaOpened 中调用 Play。
- Native callback 仅复制事件并 Post 到构造上下文；不在原生回调线程调用用户代码，不同步等待 UI。原生回调使用生成 Vtbl，IUnknown 引用计数管理 GCHandle。回调持有会话弱引用；仍要求宿主显式 Dispose 释放原生管线。
- 再次 Open 创建独立管线，旧通知失效，旧媒体由 Shutdown/Release 释放。Dispose 丢弃排队通知，不再触发用户事件。先 Dispose 再销毁 HWND。
- State 为原生确认状态，不是命令发出状态。Stop 在停止和归零定位完成后确认 Stopped。连续 Seek 合并，原生定位串行执行。不可定位源（例如直播）可能拒绝 Seek/Stop 归零，保留原生错误，不伪装成功。
- UI timer 调用 Refresh() 后读取 Position；没有 PositionChanged 事件，也没有内部计时器。Position 为最近采样/定位的位置，Duration 为零表示未知。VideoWidth/Height 是原生像素尺寸，不是 WPF DIP。
- HWND 改变大小或宿主要求重绘时调用 ResizeVideo()。纯音频也必须提供有效 HWND。
- Volume、IsMuted、PlaybackRate、IsLooping 在 Open 之间保留。PlaybackRate 必须为有限正数；原生不支持时同步抛 NotSupportedException，异步速率拒绝通过 Failed.Error 通知。非 1 倍速是否输出音频由 Windows MF 管线和媒体能力决定。
- EOF 触发 Ended、PlaybackEnded；循环开启时，随后异步 Seek(0)/Play。处理器可关闭循环、重新打开或释放；库不继续操作旧管线。
- 同步参数、线程与原生命令错误抛异常；异步失败通过 State=Failed 与 Failed.Error 报告并关闭失败管线。Open 中的即时原生失败也通过 Failed 报告。事件处理器异常属于宿主异常，不应抛出。
- 支持 Windows 已安装的 MF 解码器及协议。Windows N/精简系统可能需要 Media Feature Pack；编译成功不证明设备、编解码器和真实声音已验证。

## 诊断

通过 System.Diagnostics.Trace 输出 `MediaFoundation.Core` 原生事件（类型、HRESULT、状态）、异步失败及不可用时长诊断；宿主可安装 TraceListener。错误文本位于 Strings.resx，可添加区域资源。公开 API XML 文档随程序集生成。

## 公开契约

构造：`MediaPlaybackSession(nint videoWindowHandle)`。

方法：`void Open(Uri source)`、`void Play()`、`void Pause()`、`void Stop()`、`void Seek(TimeSpan position)`、`void Refresh()`、`void ResizeVideo()`、`void Dispose()`。

可读写属性：`double Volume`、`bool IsMuted`、`double PlaybackRate`、`bool IsLooping`。

只读属性：`TimeSpan Position`、`TimeSpan Duration`、`int VideoWidth`、`int VideoHeight`、`bool HasVideo`、`bool HasAudio`、`PlaybackState State`、`Uri? Source`。

事件：`EventHandler? StateChanged`、`EventHandler? MediaOpened`、`EventHandler? PlaybackEnded`、`EventHandler<PlaybackFailedEventArgs>? Failed`。

`PlaybackState`：Closed、Opening、Stopped、Playing、Paused、Ended、Failed。

`PlaybackFailedEventArgs(Exception error)`，只读 `Exception Error`。

## 验证边界

本目录通过 Release 构建验证生成 interop 与公开契约。真实窗口、音频设备、音画同步、媒体切换、循环和生命周期运行测试由演示宿主执行，不能以编译通过替代。建议使用带音轨 MP4、纯音频、纯视频、损坏文件、网络失败源，并覆盖播放时 Open、连续 Seek、Stop/Play、回调内 Dispose 和两架构。
