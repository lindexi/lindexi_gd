# WPF Media Foundation Player

独立产品入口为本目录的 `WPFMediaFoundationPlayer.slnx`，与仓库根目录的 demo 解决方案分离，不引用其项目或共享源码。需要支持 .NET 10 的 SDK、Windows WPF 开发环境，以及 Windows Media Foundation。Windows N 版本需要相应媒体组件。

## 三项目结构

- `MediaFoundation.Core`：不依赖 WPF 的 MFPlay 音视频管线，负责媒体打开、播放时钟、音频输出、视频渲染、异步事件和原生资源生命周期。CsWin32 生成所需互操作声明。
- `MediaFoundation.Controls`：程序集和 C# 命名空间名称，提供播放器入口 `MediaPlayer` 和位于其上层的开箱即用控件 `MediaPlayerControl`；项目仍位于 `MediaFoundation.Wpf` 目录，项目文件名仍为 `MediaFoundation.Wpf.csproj`。
- `MediaFoundation.PlayerDemo`：简单窗口承载播放器控件，通过应用资源定制画刷和按钮样式；同时提供可执行的集成自测。

目标框架为 `net10.0-windows`，支持 `win-x86`、`win-x64`。演示输出为控制台 EXE，不是 WinExe，便于沙箱等待进程结束并获取退出码。

## 构建和运行

在仓库根目录执行：

```text
dotnet build WPFMediaFoundationPlayer/WPFMediaFoundationPlayer.slnx -c Release
dotnet publish WPFMediaFoundationPlayer/MediaFoundation.PlayerDemo/MediaFoundation.PlayerDemo.csproj -c Release -r win-x64 --self-contained true
dotnet publish WPFMediaFoundationPlayer/MediaFoundation.PlayerDemo/MediaFoundation.PlayerDemo.csproj -c Release -r win-x86 --self-contained true
```

启动发布的 `MediaFoundation.PlayerDemo.exe`，默认显示未打开状态，通过打开文件按钮或拖放选择媒体，也可显式传入媒体文件路径。默认构建、发布不包含测试媒体。显式设置 `-p:IncludeSelfTestMedia=true` 时，使用仓库已有 `Video/视频.mp4`，复制为输出及发布目录下的 `self-test.mp4`。`--self-test`、`--scaling-test` 可省略媒体参数以使用该文件；显式相对测试路径以 EXE 所在目录解析，普通启动的相对路径仍以当前目录解析。缺少显式路径且未包含测试媒体时返回退出码 2。支持的媒体格式和协议取决于 Windows 安装的 Media Foundation 组件及解码器。

## 播放器入口

`MediaFoundation.Controls.MediaPlayer` 实现 `INotifyPropertyChanged` 和 `IDisposable`。在所属 WPF Dispatcher 创建和调用，并将 `VideoView` 放入可见的内容容器。无需自行管理 HWND。打开是异步操作，通过 `MediaOpened` 或 `Failed` 通知；打开期间调用 Play 会延迟到媒体就绪。

- 操作：`Open(Uri)`、`Play()`、`Pause()`、`Stop()`、`Seek(TimeSpan)`、`Close()`、`Dispose()`。
- 设置：`Volume`（0 到 1）、`IsMuted`、`PlaybackRate`、`IsLooping`。
- 状态：`Source`、`State`、`Position`、`Duration`、`VideoWidth`、`VideoHeight`、`HasVideo`、`HasAudio`、`CanPlay`、`CanPause`、`CanSeek`、`LastError`。
- 事件：`PropertyChanged`、`MediaOpened`、`PlaybackEnded`、`Failed`。

Close 后可再次 Open，Dispose 后不可重用。媒体定位和倍速以原生媒体能力为准，不支持时报告错误，不伪装成功。详细线程、事件和生命周期契约见两个类库内的 README 及公开 API XML 注释。

## 默认缩放策略

D3DImage 默认使用 A / `VideoScalingMode.MixerOutput`：mixer 输出目标尺寸 RGB32，sample 与 staging 使用目标尺寸，以减少小窗口播放 4K 时的复制带宽；不保证所有设备均支持，不支持时明确报告失败，不静默回退。B / `PresentationSurface` 保留为显式可选策略，在展示复制时执行 GPU LINEAR 缩放。默认呈现为现有 MFPlay / EVR D3DImage（D3D9Ex 表面），不是新后端，也不使用 MediaEngine。

`new MediaPlayer()`、`new MediaPlayerControl()`、Core 无参 `CreateD3DImageSession()` 和诊断 `CreateD3DImagePrototype(...)` 的未配置策略均为 A。枚举数值保持 `MixerOutput=0`、`PresentationSurface=1`，因此 `default(VideoScalingMode)` 也为 A。在 Open/Source 前设置 `ScalingMode`，或 Close 后修改再重新打开。

`new MediaPlayer()`、`new MediaPlayerControl()` 和 Demo 未配置呈现模式时均使用 `VideoRenderingMode.D3DImage`。枚举数值仍为 `Hwnd=0`、`D3DImage=1`，因此 `default(VideoRenderingMode)` 仍是 Hwnd，不代表播放器默认配置。Open 前可显式设置 `RenderingMode = VideoRenderingMode.Hwnd`，菜单也保留 HWND。

Demo 从源码 `dotnet run --project WPFMediaFoundationPlayer/MediaFoundation.PlayerDemo -- <媒体路径>` 启动时默认 D3DImage + A；`--d3dimage` 保留为显式选择，`--hwnd` 显式选择 HWND，同时指定时 HWND 优先并记录冲突日志（独立 `--mfplay-d3dimage` 诊断入口不受影响）。历史 HWND 自测应显式传 `--hwnd`，不要把未指定模式的新运行当成 HWND；`--mixer-output` 显式选择 A，`--presentation-surface` 显式选择 B（同时指定时 A 优先）。“更多”菜单同样保留 A/B 选择。已有 `--scaling-test` 对 A/B 均显式设置策略，不将 B 用例依赖于默认值。历史验证记录中的“默认 B”仅描述当时版本，不代表当前默认。

### 默认呈现切换验证

本次 Windows 沙箱验证使用当前源码 Release 构建，非历史发布目录：

- `--diagnostics-test`：默认 Player / Control / DP metadata 为 D3DImage、枚举数值兼容、显式 HWND 转发与 MixerOutput 默认断言均通过；结果在 `results/default-d3d9-diagnostics-final`。
- `--self-test --seek-regression --software-wpf`：未传渲染模式，退出 0；日志确认 `backend=MFPlay mode=D3DImage`、`scaling=MixerOutput`，覆盖视频像素、暂停、连续定位、恢复、关闭重开。结果在 `results/default-d3d9-focused-software-final`。这是 WPF `SoftwareOnly` 合成测试，不能据此宣称真实硬件 WPF 合成性能；视频仍走 MFPlay/EVR D3D9Ex。
- `--self-test --seek-regression --hwnd --d3dimage --software-wpf`：退出 0；日志确认冲突时 HWND 优先及 `backend=MFPlay mode=Hwnd`，聚焦播放/定位/重开通过；结果在 `results/default-d3d9-explicit-hwnd`。菜单测试会有意访问两种模式。

旧延迟 Open 测试已显式指定 HWND，不再依赖播放器默认值。以上为聚焦验证，未执行完整倍速扫描或真实可听音频验证。

## 控件和定制

`MediaFoundation.Controls.MediaPlayerControl` 提供可绑定的 Source、AutoPlay，以及只读 Player。默认模板提供打开文件、播放暂停、停止、拖动定位、时间、音量、静音、倍速、循环、全屏、加载与错误提示，支持文件拖放和键盘操作。进度拖动期间暂停位置跟随，释放时提交定位。

画面与操作栏分行，避免 WPF/原生 HWND 的 airspace 问题。全屏不重新挂载视频 HWND，而是修改并恢复当前窗口属性；宿主可处理 FullscreenRequested 接管全屏策略。

在控件、父容器或应用资源中覆盖以下键即可定制，无需复制整个模板：

- 画刷：`PlayerBackgroundBrush`、`PlayerForegroundBrush`、`PlayerAccentBrush`、`PlayerControlBackgroundBrush`、`PlayerErrorBrush`。
- 基础控件样式：`PlayerButtonStyle`、`PlayerSliderStyle`、`PlayerComboBoxStyle`。
- 文本：`Themes/Strings.xaml` 中的 `Player*Text` 资源。

默认模板使用 DynamicResource。演示项目的 App.xaml 提供实际覆盖示例，自测会检查资源与模板。控件在所属窗口关闭时释放，也可显式 Dispose；暂时 Unloaded 不释放，永久移除且不再使用时由宿主释放。

## 已执行的集成验证

发布后在 Windows 沙箱直接运行 EXE 的 `--self-test`，不通过脚本启动。日志见 `results` 中各目录的 `self-test.log`。

| 最终运行 | 退出码 | callback 分配/释放 | 非预期 Failed |
| --- | ---: | ---: | ---: |
| win-x86，passed-x86-first | 0 | 13/13 | 0 |
| win-x86，passed-x86-repeat | 0 | 13/13 | 0 |
| win-x64，passed-x64 | 0 | 13/13 | 0 |

上述运行覆盖媒体打开、音视频轨信息、时长和尺寸、播放位置推进、暂停保持、定位、停止归零、原生音量和静音读回、2 倍速时钟推进、真实 EOF 与循环、重复打开关闭、错误文件与恢复、资源覆盖、模板部件、全屏恢复、80 次 resize，以及 Dispose 后无迟到通知。2 倍速实测时钟比率约 2.055、2.334、2.298；短期测量受调度影响，不作为性能保证。

8 次生命周期循环中，句柄分别为 699→715、699→712、673→692。callback 平衡与短期句柄检查不等于证明所有托管和原生内存完全无泄漏。

### 必须区分的验证边界

核心使用原生音频输出，不是无声的视频帧模拟。自测另执行 WASAPI loopback，但沙箱获取默认音频端点返回 `0x80070490`，因此未取得回采样本或 RMS 能量。**实际声音输出与音画同步未验证，不能以音轨存在或命令成功替代。** 需要在具有音频端点的 Windows 环境再次运行同一自测，并检查回采结果。

已验证的是上述测试媒体与沙箱环境，不代表所有编解码器、网络协议、直播源和设备组合均通过。MFPlay 渲染更新限于 Playing/Paused 状态；Stopped 状态曾观察到原生渲染器返回 `0xC00D3E85`，未过滤或吞掉该错误，停止状态不请求视频更新。最终两架构及 x86 重复运行均未产生非预期失败。
