# MFPlay GUI 原型验证（2026-10-23）

> 当前正式集成收尾见 [FINAL-REGRESSION.md](FINAL-REGRESSION.md) 的“当前集成收尾”：两个发布目录已从当前源码刷新；最终软件 WPF D3DImage 与 HWND 聚焦测试均 EXIT 0，默认合成屏幕取样仍 EXIT 1。正式 D3D 运行日志为 backend=MFPlay，无 MediaEngine 实例创建/事件记录。本页下文为此前原型/GUI 验证历史，不替代当前正式集成结果。原机实验原型可播放，但本次新正式发布尚未在原机运行。

## 收尾复核：最终结果（覆盖下文历史失败结论）

本轮只验证并更新记录，未修改架构或产品代码。

### 构建与最终发布

- `WPFMediaFoundationPlayer.slnx` Release build：退出码 0，0 警告 / 0 错误。
- 已从当前源码重新发布 win-x64 self-contained Release 到 `publish/mfplay-gui-fix`，IncludeSelfTestMedia=true，发布退出码 0。
- 最终程序为 `publish/mfplay-gui-fix/MediaFoundation.PlayerDemo.exe`；下述两次新沙箱执行均直接推送该发布目录，测试后未再变更源码或二进制。

### 原型实际 PASS 与测试覆盖

已读取 `results/mfplay-protocol-final/mfplay-d3dimage.log`，第 37–67、99–104 行实际记录：

- first-30-frames：actualFrames=30。
- pause：actualFrames=31。
- seek-resume：actualFrames=43，position=00:00:02.4186034。
- media-progress-without-ui-drawing：actualFrames=58。
- close-reopen：actualFrames=10。
- PROTOTYPE RESULT PASS；callback released live=0，sink shutdown HRESULT=0。

又用最终 `mfplay-gui-fix` 发布程序执行 `--mfplay-d3dimage --self-test self-test.mp4`，证据为 `results/mfplay-gui-fix-protocol-verified/mfplay-d3dimage.log`：上述五个 stage 再次全部 PASS，帧计数依次为 30 / 31 / 43 / 58 / 10，seek position=00:00:02.4214226，最终 RESULT PASS，callback live=0 / sink shutdown HRESULT=0。

准确覆盖范围（按 MfPlayPrototype.RunAsync 的断言）：

- first-30-frames 仅要求媒体已打开且原型计数 >=30，不是 WPF 已显示 30 帧的断言。
- pause 等待 Paused，再间隔 500ms Refresh 验证时钟漂移 <=100ms；漂移检查没有独立 PASS 行，但 RESULT PASS 证明未抛错。
- 暂停时连续请求 Seek(6s)、Seek(1s)、Seek(2s)，700ms 后断言仍为 Paused；再 Play，要求 Playing、计数增加至少 10、位置 >2.3s。日志确认 6s 和最终 2s 的 STEP_COMPLETE / SCRUB_TIME / POSITION_SET；不保证每个中间请求都执行，也没有逐一验证 1s、暂停后的最终位置容差、seek 后 WPF 画面或音画同步。
- 停止 UI 绘制时要求计数再增加至少 15，验证媒体推进不依赖该绘制分支；不等于后台 WPF 仍呈现。
- close-reopen 调用内部 Open，先释放旧 session 再创建新 session，要求 opened 且计数 >=10；不是关闭整个原型窗口再由 GUI 菜单打开。

### HWND 聚焦回归（不扫描倍速）

从源码查得参数为 `--self-test --seek-regression self-test.mp4`，不是 `--hwnd-regression`。执行结果在 `results/mfplay-gui-fix-hwnd-focused/self-test.log`，03:38:14–03:39:00 UTC，EXIT 0。

实际 PASS：未打开状态模式切换；D3DImage 暂停位置恢复（非呈现证明）；恢复 HWND 后 Source/AutoPlay/暂停意图保持；Playing/Stopped 切换及快速请求隔离；deferred Open 锁定与 Close 解锁；模板、画刷和按钮样式；轨道选择、时长、640x360；既有视觉断言；控件播放时钟推进、暂停及位置保持；进度条 seek、公开 API seek；暂停连续 seek(6/2/5s) 保留最终 5s 目标且稳定；恢复真实 PLAYING 与时钟推进；close-reopen 后时钟推进。最终明确记录 `PASS focused HWND pause/continuous-seek/resume/close-reopen (rate sweep not requested)`。未运行后续整套倍率时钟扫描；倍速弹出框的既有布局检查不等于倍率播放测试。HasAudio=true 不等于可听声音通过。

### 实际截图检查

已用图片分析检查以下真实生成文件：

- `results/mfplay-protocol-final/mfplay-d3dimage-wpf.png`。
- `results/mfplay-gui-fix-protocol-verified/mfplay-d3dimage-wpf.png`。

两张原型截图均可见浅灰/淡黄色模糊区域，无可辨识具体场景；五个按钮清晰位于画面上方，状态栏位于下方，未见明显重叠。状态仍显示 `Stopped | 00:00:00 | actualFrames=1`。不能确认该画面是否为预期视频内容，更不能以日志计数证明 WPF 连续显示。控件在独立行，不能充当视频内 overlay 的证据。

还检查了 `results/mfplay-gui-fix-hwnd-focused/`：

- `visual-normal.png`：有可辨识的视频内容（含片名《迷雾》）；右下角远程控制通知遮挡部分控件，不能据此确认完整右侧布局。
- `visual-narrow.png`：视频、时间、播放/音量/倍速/循环/全屏控件可见，未见相互覆盖或窗口边缘裁切。
- `visual-rate.png`：0.5/1/1.5/2 菜单全部在截图内，向窗口下方弹出但未被截图裁切。

以上均为自动生成静态截图，不冒充鼠标操作、持续呈现、声音或实际倍速验证。

### GUI 入口验证边界

当前 GUI 入口实际调用 `RenderingModeMenu.OpenPrototypeAsync` → `MfPlayPrototype.RunAsync(..., false, owner)`；没有名为 `MfPlayPrototype.Open` 的可直接调用入口。既有自测只覆盖 RenderingMode 切换及命令行原型，没有自动打开 GUI 原型并断言 Window.Owner / 主窗口关闭联动的测试。代码可见 owner 赋值，但本轮未将静态检查写成运行 PASS，也未扩展新测试架构。菜单点击、无源文件对话框、owned Window 归属/关闭仍未实测。

---

以下为前轮历史记录，失败与未完成项不代表上述最终发布程序的新回归结果。

## 用户日志真实链路（UTC）

原始文件：`C:\Users\lindexi\AppData\Local\WPFMediaFoundationPlayer\Logs\session-20261023-025350413-63844-6e894bb5d90049debdac7688de099ce9.log`。

- 02:53:53.714：MKV 本地访问成功，bytes=4507855237，accessHRESULT=0。
- 02:53:54.043：MFPlay MEDIAITEM_SET；音频、视频均存在且选中。
- 02:53:54.181：MFPlay PLAY 成功。
- 02:53:56.576：正式 RenderingMode 从 Hwnd 切到 D3DImage；此前播放位置 2.2515889 秒。
- 02:53:56.713：MediaEngine CreateInstance(frame-server) 成功。
- 02:53:56.716：MFCreateFile 和 IMFMediaEngineEx 查询成功。
- 02:53:56.717：SetSourceFromByteStream 成功；随后 LOADSTART。
- 02:53:56.886：异步 ERROR。
- 02:53:56.887：IMFMediaEngineNotify.ERROR，mediaError=4 / MF_MEDIA_ENGINE_ERR_SRC_NOT_SUPPORTED，HRESULT=0xC00D5212 / MF_E_TOPO_CODEC_NOT_FOUND。

失败位于异步媒体拓扑建立阶段，不是文件访问、设备创建或同步 SetSource 失败。日志本身不能定位具体缺失/不兼容的轨道解码器，也不能证明原型能播放此电影。

## GUI 和生命周期

更多 → 打开 MFPlay + EVR / D3DImage 原型（实验）。使用当前 Source；无源时使用 Controls.Strings 的共享 FileFilter。直接在同一 App 打开 owned Window，不启动新进程、不改变正式 RenderingMode。主窗口关闭会关闭原型；原型关闭停止 timer、解绑 D3DImage、释放 session。GUI 使用统一 PlaybackDiagnostics 会话日志，记录完整本地源路径及窗口/open/close stage。独立 mfplay-d3dimage.log 仅用于自测。

发布程序：`WPFMediaFoundationPlayer/publish/mfplay-gui-fix/MediaFoundation.PlayerDemo.exe`。

## 协议修正和边界

FLUSH 重置 preroll；OnClockStart/Restart 不再清除已经完成的预卷。每次 FLUSH 后首样本不受时钟等待约束；后续样本仍按真实时钟和时间戳调度。mixer 成功输出后通过 SDK EC_PROCESSING_LATENCY 通知系统关联时间差，使用同步通知期间有效的 100ns latency 指针。不添加猜测的 EC_SAMPLE_NEEDED 或 EC_SAMPLE_LATENCY，不修改媒体时钟、不加延迟绕过问题。

这些修改没有解决 seek-resume；不能据此宣称协议已完整实现。PROCESSINPUTNOTIFY 仍设置 input，由 UI Tick 拉取 mixer；前缓冲不可锁时整个 Tick 被跳过，仍需解耦 presenter 调度与 D3DImage 可用性。没有在线文档获取手段，本轮未完成外部 EVR 文档逐项核验。

## Windows 沙箱实际执行

均为真实发布 EXE，参数 `--mfplay-d3dimage --self-test self-test.mp4`，普通 MP4。

- results/mfplay-gui-fix-1：first-30-frames 超时，12 帧后 TryLock=false / frontBufferAvailable=False。
- results/mfplay-gui-fix-2：first-30-frames PASS，pause PASS，暂停漂移检查通过；seek-resume 超时。FLUSH → OnClockStart offset=20000000 → frames=33，timestamp=20400000，clock=20000000，running=True，preroll=True，ready=3/free=0。未见 POSITION_SET，close-reopen 测试未到达。
- 上项 mfplay-d3dimage-wpf.png 经图片分析：视频内容可见，顶部按钮与视频上缘局部重叠；底部状态在黑色区域，不能算完整 overlay 验证。
- 最终版本 results/mfplay-gui-fix-final：first-30-frames 超时，16 帧后 TryLock=false / frontBufferAvailable=False。最终版本未到达 pause/seek/close-reopen。

默认 HWND 回归：results/mfplay-gui-fix-hwnd/self-test.log。02:59:56–02:59:58 UTC，播放、暂停、暂停保持、连续 seek、seek 后真实时钟推进通过；03:00:04 整套测试失败于 2 倍速时钟比率 2.442，EXIT 1。音频设备不可用，不代表声音验证通过。

GUI 新入口的点击、无源文件选择和 owned Window 关闭链路尚未运行自动化实测；仅编译验证及代码检查。原型截图来自命令行同一窗口实现，不冒充菜单点击实测。

最终 Release publish 成功；播放器解决方案 Release build 成功，0 警告 / 0 错误。未修改 TFM、SDK、NuGet 版本、长路径处理。未在宿主运行用户电影，未修复或验证 MKV。
