# 播放诊断

普通启动即由 Demo 订阅 Trace（Core/Wpf 不安装全局监听器）。日志路径：

`%LOCALAPPDATA%\WPFMediaFoundationPlayer\Logs\session-yyyyMMdd-HHmmssfff-PID-GUID.log`

每次进程会话独立文件，启动保留最近 19 个旧文件；当前文件合计最多 20 个（并发进程或删除失败时可能暂时超出）。后台有界队列 4096 条，约 500ms flush，退出最多等待 3 秒。写盘/队列失败通过 stderr 和资源化 UI 提示，不向播放抛出异常。

更多菜单支持打开日志文件夹、复制诊断信息；播放和模式切换失败显示 HRESULT 与完整日志路径。日志中的媒体位置替换为类型/匿名 ID；异常保留 ToString 的堆栈/异常链结构，媒体 URI 和 Windows 路径脱敏。日志文件自身路径单独记录于文件头。

定位字段：UTC、thread、Startup/process/architecture/OS/runtime、WPF tier/processMode、Adapter name/vendor/device/LUID/driverVersion/queryHRESULT、stage/HRESULT、D3D creation flags/fallback、SharedTexture size/format/bind/misc、MediaEngine event、播放/seek/rate/position、D3DImage first frame/progress/front buffer、Switch begin/restore/complete/failed。

高频帧调用成功不记录，MediaEngine TIMEUPDATE 不记录；呈现和播放器进度最多每 5 秒一次，即使无首帧也保留进度状态。驱动版本查询不支持时记录查询 HRESULT，不猜测版本。

## MFPlay 自定义 EVR：有声无画面，切换 HWND 后正常

**现场表现**：部分机器上，D3DImage 模式有声音、进度正常，但始终没有画面；同一媒体切到 MFPlay/HWND 后正常。已知一例为 x86 播放器、Windows 10 Build 19043、Intel HD Graphics 5500。该配置是现场记录，不是显卡或驱动缺陷的结论。此问题也不同于 `MEDIAITEM_CREATED` 阶段的 `0x800700EA`：这里媒体项创建和设置均已成功，视频轨已选中。

**关键证据**：首帧超时快照显示 `frontBufferAvailable=True`、`lockAttempts=464`、`lockFailures=0`，时钟读取成功且播放位置已超过 5 秒；同时 `inputPending=True`、`inputNotifications=1`、`outputAttempts=2`、`outputSamples=0`、`ready=0 free=0`、`stagingFrames=0`。因上游没有帧，`backBufferBinds=0` 和 `dirtyRects=0` 不能归因于 WPF 绑定失败。启动阶段的 `0xC00D6D72`（`MF_E_TRANSFORM_NEED_MORE_INPUT`）本身可以是正常结果，真正可疑的是输入已挂起但输出样本池为空，mixer 无法继续取帧。

**修复**：`MediaFoundation.Core/Playback/EVRPrototypePresenter.cs` 中，EVR 服务重绑会释放旧输出类型及样本池。旧版调度器在没有输出类型时直接返回；若重绑后先收到输入、没有再次收到 `MFVP_MESSAGE_INVALIDATEMEDIATYPE`，就可能一直没有首帧。新版在 mixer 可用、有输入待处理且没有输出类型时主动重新协商并重建样本池；若 mixer 暂无可用类型，按 250 ms 间隔重试，而不是每次调度都重新协商。输出类型提交期间避免重入。此修改不切换 HWND、不更换解码器，也不改变显卡选择。瘦身发布移除内嵌 MP4 和 x64 探针与画面恢复机制无关。

**复核**：故障机反馈新版可以播放，但尚未收到恢复后的完整日志，不能断言现场一定走了重绑恢复分支。可查看同一会话中的 `EVR services release/init`（按 `epoch` 对照）、`EVR output recovery deferred/complete`、`EVR prototype actualFrames=1` 和 `first-frame copied`。`recovery complete` 后出现首帧记录，才直接证明恢复路径重建了样本池并提交了画面。若仍无画面，让用户保持 D3DImage 播放并回传自动生成的 `*.diagnostics-*.txt`；优先对照 `hasMixer`、`hasOutputType`、`free`、`ready`、`stagingFrames` 和 `copiedFrames`，不必手动切模式或发送原视频。

## 自测和本次验证边界

`MediaFoundation.PlayerDemo.exe --diagnostics-test` 使用与普通启动相同的初始化路径，验证启动、完整异常类型/内层 HRESULT、日志完整路径及媒体脱敏；成功生成 `diagnostics-result.log`。现有 `--self-test self-test.mp4 --d3dimage` 可继续用于真实播放与 D3D 故障记录。

本次 Release 构建与 x64 自包含发布成功。沙箱首次运行未拉取到预期结果文件；修正自测读取日志的共享模式后，重试被远端连接重置/超时阻断。未取得运行成功证据，不能声称诊断自测或真实播放通过，也未声称原 D3D 根因已修复。
