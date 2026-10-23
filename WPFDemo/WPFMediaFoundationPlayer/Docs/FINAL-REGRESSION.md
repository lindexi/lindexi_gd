# 最终收敛回归

## D3DImage TryLock 收尾（2026-10-23 09:02，优先于下文历史记录）

### 已确证契约与修复边界

`results/d3dimage-contract/contract.log` 来自实际 PresentationCore 10.0.0.0：LockImpl 在 WaitOne 返回后无条件 `_lockCount++`；强制未就绪实验得到 `FORCED timeout result=False count=1`，一次 Unlock 后 count=0。Unlock 在 dirty 且计数归零时 SubscribeToCommittingBatch。故 TryLock 正常返回 false 仍必须配对 Unlock，但不能写 backbuffer/dirty；调用抛异常时则不进入配对段。这不是普通 Monitor.TryEnter 的契约。正式 MfPlayD3DImagePresenter 与同类 D3DImagePresenter 均已采用 bool writable + try/finally Unlock。旧的 false 直接返回会积累计数、破坏后续提交；不能将历史失败简单归咎于 sandbox GPU。

本轮没有再修改产品源码、超时、断言或默认策略；未新增全局 SoftwareOnly、CPU copy fallback 或未使用产品代码。既有显式 `--software-wpf` 仅作对照，既有 SetBackBuffer 软件可用参数未改变。

### 纯色独立对照（与产品结果分开）

读取原 `results/d3dimage-color-default/color-probe.log` 及两张截图：中心 RGB=255,255,255，实际是白色，不是黑色，也不是红绿成功。补充诊断源码位于仓库根 `.coding-agent/d3dimage-contract/ColorProbe.cs` / ColorProbe.csproj，独立于 Player slnx；无 MF、EVR、媒体文件依赖。重新发布同一 EXE，仅以 `--software` 切换进程渲染模式：

- `results/d3dimage-color-default-final`：60 次 writable、ColorFill/fence HRESULT=0，countAfter=0，front=True；两张实际 GetDC/BitBlt 客户区截图仍白色，中心均 255,255,255。**默认纯色可见性失败保留**，进程正常退出不等于视觉通过。
- `results/d3dimage-color-software-final`：相同 60 次提交；截图分别真实红色和绿色，中心 255,0,0 / 0,255,0，front=False。这说明 frontBufferAvailable 本身不能替代可见性断言。
- 两者 Unlock 后 `_isDirty=True`，ApplicationIdle 机会点 `_isDirty=False`，count=0、changePending=False、waitingForUpdateResourceBecauseBitmapChanged=False。证据支持 WPF 已处理 dirty 提交，而非继续卡在锁计数；不能据此证明 compositor 最终显示成功。
- GetDesc 实测：X8R8G8B8、SURFACE、usage=0x1 (RENDERTARGET)、DEFAULT pool、无多采样、320×180；创建参数 lockable=false，ColorFill 的高字节=255，XRGB 不以透明 alpha 合成。D3D9Ex HAL 创建设备成功，flags=HARDWARE_VERTEXPROCESSING|MULTITHREADED|FPU_PRESERVE。
- 实测 adapter ordinal=0 vendor=0x1414 device=0x8C、driver=2814751477605438、Caps2=0xE0000000（包含 CANSHARERESOURCE）、DevCaps2=0x7F、StretchRectFilterCaps=0x3000300；SM_REMOTESESSION=0。这些是观测值，不假定实体 GPU，也不由 remote=0 推断没有远程/虚拟显示链路。未直接取得 WPF compositor 自身 device/adapter LUID 或 WDDM 驱动模型版本，因此没有证据将白屏精确归因于 adapter mismatch 或某版 WDDM；仅记录本环境该独立默认合成场景受限。

原 Demo `D3D11MediaFoundation/Shared/WpfPresenter.cs` 用 CompositionTarget.Rendering、阻塞 Lock、默认 SetBackBuffer 重载（未启用软件可用参数），Graphics 使用 D3D11/D3D9 共享 A8R8G8B8 texture 的 level-0 surface；原测试主要验帧数/resize，不等于屏幕像素通过。正式 Player 是后台 EVR scheduler + UI TryLock、独立 X8R8G8B8 render target、GPU fence 后 dirty，WPF 持额外 COM 引用。纯色使用相同 XRGB render target 创建方式，但无 EVR/StretchRect/调度与实际控件布局，故不能凭纯色失败否定已实测可见的产品，也不能据原 Demo 帧数推断颜色成功。未盲改产品格式或 adapter。

### 最终构建、发布及真实回归

Player slnx Release 构建退出 0，读取构建日志确认 **0 warning / 0 error**；摘要 `results/d3dimage-closeout-build.log`。最终包：`publish/d3dimage-closeout/MediaFoundation.PlayerDemo.exe`，win-x64、自包含、IncludeSelfTestMedia=true。发布退出 0；使用同一包依次运行以下现有参数，结果目录均保留原始 self-test.log、playback-session.log、截图。

| 参数 | 结果 | results 子目录 |
| --- | --- | --- |
| `--scaling-test --software-wpf` | EXIT 0，完整 A/B case + HWND 对照 | d3dimage-closeout-scaling-software |
| `--self-test --seek-regression self-test.mp4` | EXIT 0，HWND 重点 | d3dimage-closeout-hwnd |
| `--scaling-test` | EXIT 0，尺寸/时钟/生命周期 A/B case + HWND 对照；另检查实际截图 | d3dimage-closeout-scaling-default |
| `--self-test --d3dimage --software-wpf --seek-regression self-test.mp4` | EXIT 0，WPF capture colors=5468 + 屏幕断言 | d3dimage-closeout-software |
| `--self-test --d3dimage --seek-regression self-test.mp4` | EXIT 0，默认 WPF capture colors=5468 + 屏幕断言 | d3dimage-closeout-default |

A/B scaling 完整覆盖两策略的初始尺寸、暂停 resize、seek 12 秒、20 次暂停 resize、close/reopen、8 次播放 resize。A mixer/display 同目标，B mixer 固定原生 640×360；均 no fallback。人工图像分析复核软件 A/B paused-before/paused-resize 为同片头画面缩放，seek 后为窗前三人，确有真实画面改变。默认 PresentationSurface playing/seek 亦由片头变为窗前三人，默认 MixerOutput seek 有同一真实电影场景；默认 focused 的 visual-normal.png 是画室电影场景，不是空白。scaling 的 EXIT 0 本身不是视觉断言，故上述截图核对单独列出。

focused 两模式覆盖既有菜单双向切换与意图恢复、视觉模板/实际屏幕取样、进度条鼠标拖动/取消/键盘、公开 Seek、暂停连续 Seek 最终目标、恢复 Playing、close/reopen 时钟。默认产品此次真实通过，不将历史失败删除，也不把仍失败的默认纯色写成通过。未完整重新执行倍率 sweep、EOF/循环、长期泄漏、真实跨屏 DPI；音轨选择不代表可听声音或音画同步。原机普通交互启动、compositor 内部 device adapter 与 WDDM 版本尚未直接实测。


## A/B 缩放本轮收敛（2026-10-23 07:41，优先于历史记录）

最终 Release 解决方案构建退出 0，0 warning / 0 error；在最后源码修改、发布及两种沙箱模式实测后再次完成构建。最终发布：`publish/scaling-verified/MediaFoundation.PlayerDemo.exe`（win-x64、自包含、IncludeSelfTestMedia=true）。使用实际已有 `Video/视频.mp4`，发布为 `self-test.mp4`；源为 640×360。默认产品仍为 B / PresentationSurface；未改普通启动路径语义、未启用全局 SoftwareOnly、未修改有效 MFPlay STEP。

修复：测试媒体构建/发布复制；测试相对路径统一按 EXE 目录；`--scaling-test` 独立进入测试；B 的 StretchRectFilterCaps LINEAR 位由错误的 POINT 位修正为 MINFLINEAR 0x200 和 MAGFLINEAR 0x02000000；新增原生错误文本进入 Core/Strings.resx。测试不再假设现有源大于最小窗口，但仍严格要求源与展示尺寸不同、A/B 实际尺寸符合策略且无 fallback。两个 case 独立执行并汇总失败。

源码复查：A 在 RequestTarget 中 TEST_ONLY 后实际 SetOutputType 提交目标类型，更新 frame size/PAR/stride/sample size/aperture，先分配新池并保留暂停帧，GPU 完成后提交才释放旧类型；保留 preroll/STEP。B 检查真实 LINEAR min/mag 能力。展示面替换由 WPF 持有额外 COM 引用，SetBackBuffer 成功替换后释放旧引用，关闭前先 detach。此检查不等价于长期泄漏证明。

| 最终同一发布包测试 | 结果 | 证据 |
| --- | --- | --- |
| `--scaling-test --software-wpf` | EXIT 0，A/B 全 case 通过，HWND 对照通过 | `results/scaling-verified-software/self-test.log`、`playback-session.log`、截图 |
| `--scaling-test` 默认 WPF | EXIT 1，两个 case 均执行并真实报告失败 | `results/scaling-final-default-both/self-test.log`、`playback-session.log` |

软件 WPF 为明确独立测试模式，仅测试参数显式启用，不是产品回退。A 的 mixer/display 从 757×426 到 650×366、704×396、721×406；B mixer 保持 640×360，display 为上述目标尺寸。每种策略包含暂停 resize、暂停 seek 到 12 秒、20 次暂停 resize、close/reopen、8 次播放 resize。前一同逻辑软件运行 `results/scaling-final-software` 的六张对照截图已逐张检查：暂停前后同片头画面保留；seek 后变为三人看向窗外的电影画面，而非仅工具栏时间变化。最终包重新完整跑过全部 case。

默认 WPF 的精确已观察阻塞阶段：`MfPlayD3DImagePresenter.OnTick -> D3DImage.TryLock(2ms)` 返回 false（frontBufferAvailable=True），并非原生 HRESULT 失败。B 展示仍为 640×360，未到新目标呈现；A 原生 SetOutputType 已成功提交 757×426，随后 display=0×0，未获得 WPF 锁重建展示。不得把软件模式成功写成默认 WPF 可见性成功。沙箱默认合成问题仍未解决；原机普通启动未直接实测，不声称已验证。历史默认合成同样失败，本轮没有以全局软件模式改变产品行为。物理跨屏 DPI、声音和长期泄漏未覆盖。

发布复现：`dotnet publish WPFMediaFoundationPlayer/MediaFoundation.PlayerDemo/MediaFoundation.PlayerDemo.csproj -c Release -r win-x64 --self-contained true -p:IncludeSelfTestMedia=true -o WPFMediaFoundationPlayer/publish/scaling-verified`。

## 当前集成收尾（2026-10-23，优先于下文历史记录）

本轮未修改产品源码、等待时间或断言，未设置全局 SoftwareOnly 产品 workaround；只构建、重新发布、执行既有聚焦测试并更新证据。下文 MediaEngine、字节流及倍速记录是历史结果，不代表当前正式 D3D 后端。

### 最终构建与刷新发布

独立 Player 解决方案 `WPFMediaFoundationPlayer.slnx` 的 Release 构建退出 0，**0 warning / 0 error**。随后从同一源码重新发布 win-x64、自包含、IncludeSelfTestMedia=true 到以下两个现有目录，均退出 0，刷新当前应用二进制（非使用旧包）：

- `publish/mfplay-unified/MediaFoundation.PlayerDemo.exe`
- `publish/mfplay-gui-fix/MediaFoundation.PlayerDemo.exe`

构建及两次发布输出保存于 `results/mfplay-closeout-build-publish.log`。这是覆盖发布，不声称清空过目录内无关历史文件。测试直接推送上述最终发布目录并执行 EXE，之后未再改产品源码或发布二进制。

### 最终二进制实测

以下路径相对于本 Player 目录；每个结果目录均保留原始 `self-test.log`、`playback-session.log` 和实际生成的截图。

| 场景 / 发布来源 | 既有测试参数 | 结果 | 原始日志 |
| --- | --- | --- | --- |
| 正式 D3DImage，软件 WPF / mfplay-unified | `--self-test --d3dimage --software-wpf --seek-regression self-test.mp4` | **PASS，EXIT 0**；04:14:52.0738532Z focused D3DImage PASS，04:14:52.0962496Z EXIT 0 | `results/mfplay-closeout-software/self-test.log`、同目录 `playback-session.log` |
| HWND / mfplay-gui-fix | `--self-test --seek-regression self-test.mp4` | **PASS，EXIT 0**；04:15:52.5686970Z focused Hwnd PASS，04:15:52.5922839Z EXIT 0 | `results/mfplay-closeout-hwnd/self-test.log`、同目录 `playback-session.log` |
| 正式 D3DImage，默认 WPF 合成 / mfplay-unified | `--self-test --d3dimage --seek-regression self-test.mp4` | **FAIL，EXIT 1**；04:16:32.6629083Z `Blank video screenshot: visual-normal.png, colors=1`，04:16:32.8946357Z EXIT 1 | `results/mfplay-closeout-default/self-test.log`、同目录 `playback-session.log` |

两条 PASS 均实际覆盖既有菜单双向切换/意图恢复、模板和视觉断言、播放时钟、暂停保持、公开 Seek、暂停连续 Seek(6/2/5s) 保留最终目标与稳定、恢复真实 Playing、close-reopen 时钟推进。软件 WPF 离屏取样 colors=5651，并通过既有实际屏幕取样断言。聚焦出口明确 `rate sweep not requested`，不宣称倍率、完整 EOF/循环及长时间生命周期全套重新通过；HasAudio=true 不证明可听声音和音画同步。

### 正式后端日志核对

软件 WPF 的 `self-test.log` 第 25、97、166、221 行，以及默认合成的第 25、97、167、222 行均为 `backend=MFPlay mode=D3DImage session-created`。软件 WPF 同时记录 `first-frame copied GPU-complete size=640x360`、back-buffer detach 及 frame pump stopped/handlers detached。

三次运行的原始自测/会话日志中均未发现 MediaEngine 创建或事件记录。程序集仍包含旧 MediaEngine 类型/互操作声明，不能将二进制内类型名存在误作运行时创建；这里确认的是本轮正式运行日志，而不是宣称删除全部历史实现。

### 仍然失败 / 未验证的边界

- 默认合成离屏 WPF 取样 colors=172，通过离屏像素断言，但实际屏幕 colors=1 仍严格失败；后续聚焦 seek/reopen 未执行。不能以离屏有图冒称屏幕正常，也未通过全局 SoftwareOnly 掩盖问题。失败在 PNG 保存前发生，不声称有失败的 `visual-normal.png`。
- **原机实验原型可播放是既有实验结果；本次新正式 MFPlay 集成发布尚未在原机运行。** 沙箱 MP4 通过不能代替原机用户 MKV、实际声音、音画同步、GUI 手动操作和默认合成验证。本轮可用执行环境为 Windows 沙箱，原机测试未执行，不标 PASS。
- 本轮只运行 win-x64；未重跑 win-x86、全部格式/协议、硬件解码、音频回采或完整倍率扫描；不沿用历史结果冒充当前重测。

---

以下保留历史回归记录，涉及旧后端或旧产物的失败与通过不得覆盖上述当前结果。

## 路径字节流收敛（本轮）

本轮原始证据：`results/path-stream/self-test.log`。首个普通文件（115 字符、47255617 字节、可读）在 SetSourceFromByteStream 成功后再次 Load，产生 ABORT / EMPTIED / 第二次 LOADSTART，再报 0x80070002；特殊、长路径及 missing 当时均未执行。不能称为特殊字符失败。

修复：本地字节流入口不再调用 Load，设置 MF_BYTESTREAM_CONTENT_TYPE（MP4 为 video/mp4），使用 media.mp4 简单源提示；会话保留 IMFByteStream 引用，在引擎 Shutdown/Release 后释放。网络入口仍沿用 SetSource + Load。默认 HWND 及 MFPlay 实现未修改。失败清理直接发布 Failed，不再短暂发布 Closed；Dispose 日志记录帧计时器停止及 Tick 解除。

最终聚焦执行：win-x64 自包含产物，`--source-path-test self-test.mp4 --software-wpf`；完整证据 `results/path-verified-final/self-test.log`。

| case | D3DImage | 默认 HWND / MFPlay |
| --- | --- | --- |
| plain（115 字符） | PASS，时钟推进及首帧 | PASS |
| 中文、空格、#、%、&（121 字符） | PASS，首帧及 GPU 回读 colors=1904 | PASS |
| 字面 %23%25%20（126 字符） | PASS | PASS |
| long（340 字符） | FAIL，MFCreateFile 返回 0x80070003，未进入 stream 加载 | FAIL，0x80070003 |
| missing（预期失败） | PASS，0x80070002，1 秒后 State=Failed | PASS，0x80070002，1 秒后 State=Failed |
| missing 后重新打开普通文件 | PASS | PASS |

日志显示普通/特殊文件播放后 source stream released，frame pump stopped timerEnabled=False handlerDetached=True，特殊文件本次呈现 40 帧。missing 清理呈现 0 帧且 pump 停止。Core/Wpf/Demo 的 HMAC ID 同源一致，URI 重建及跨后端 correlation=True；不记录媒体明文位置。成功帧调用和 TIMEUPDATE 不记日志，进度维持 5 秒限频。

结论：保留已实证恢复普通和特殊文件的字节流修复。340 字符路径在托管文件访问成功后仍被原生打开拒绝，两个后端均未解决；不将其误报为特殊字符缺陷，不扩改默认后端。测试保留严格失败，SUMMARY failures=2，退出 1；并非全套通过。本轮仅重跑上述聚焦路径测试，不替代下述历史功能/屏幕/倍速验证。

本轮先构建独立播放器 Release，再发布 win-x64 自包含产物执行 Windows 沙箱自测。根 demo 解决方案与播放器解决方案独立，不能互相替代构建验证。

## 保留的修复

- D3DImagePresenter 使用 16ms DispatcherTimer 持续消费 frame-server，不再依赖 CompositionTarget.Rendering 或 View.IsVisible；暂停时也持续 OnVideoStreamTick/TransferVideoFrame。帧传输不受 D3DImage.TryLock 失败阻断。
- MediaEngineSession 显式记录在途 seek；连续请求保留最新目标，在 SEEKED 中刷新并推进串行队列。播放意图等待 seek 完成；PLAYING/PAUSE 仍由真实原生事件确认，没有用 CANPLAY 伪造。
- 仅扩充现有 SelfTest：暂停连续定位 6→2→5 秒、暂停位置稳定、恢复播放与时钟推进。未新增测试架构，未改变默认 HWND。

## 实测与证据

日志路径均相对于仓库根目录；每个目录内 self-test.log 含原生事件跟踪。

| 场景 | 结果 | 日志 |
| --- | --- | --- |
| 修改前基线，D3DImage + 软件 WPF | 退出 1；首帧后 seek 卡住，暂停切换超时 0x8013153B | .coding-agent/player-final-baseline/self-test.log |
| 最终 D3DImage + 软件 WPF | 双向切换 Playing/Paused/Stopped、暂停连续 seek、恢复播放、Stop 均通过；屏幕与 WPF 像素检查通过；随后倍速断言失败，退出 1 | .coding-agent/player-final-d3d-software/self-test.log |
| 最终默认 HWND | 完整功能/生命周期自测退出 0；连续 seek、双向切换、2 倍速、EOF/循环、重开、错误恢复、Dispose 通过；MFPlay callback 13/13，非预期 Failed=0 | .coding-agent/player-final-hwnd/self-test.log |
| 最终 D3DImage + 默认 WPF 合成 | 双向切换通过；GPU/WPF 渲染取样有图，但实际屏幕取样 colors=1，退出 1 | .coding-agent/player-final-d3d-hardware/self-test.log |

软件 WPF 路径的 visual-d3dimage-wpf.png 与 visual-normal.png 可见真实视频片头，而不只是控件或黑屏。首轮修复产物截图经独立图像分析确认。最终软件 WPF 运行也通过相同像素断言，图片位于 player-final-d3d-software。默认 WPF 的失败发生在 PNG 写入前，因此没有失败的 visual-normal.png，不能冒称已有该截图。

## 最终剩余缺陷与验证边界

1. D3DImage 软件 WPF 路径设置 2 倍速后时钟推进比率仅 0.950（上一轮 1.031）。原生命令未报告失败 HRESULT；失败为自测 InvalidOperationException（标准 HResult 0x80131509），不是已查明的原生错误。未修改倍速逻辑或弱化断言；之后的 D3D EOF/生命周期测试未执行，不能标为通过。
2. D3DImage 默认 WPF 合成路径实际屏幕取样单色：Blank video screenshot: visual-normal.png, colors=1。失败为 InvalidOperationException（标准 HResult 0x80131509），没有对应原生失败 HRESULT。不能将 WPF 离屏有图等同于默认合成路径屏幕呈现正常。
3. 沙箱 D3D11CreateDevice(VIDEO_SUPPORT) 返回 DXGI_ERROR_UNSUPPORTED 0x887A0004；既有同适配器 BGRA 重试成功。上述 D3D 帧传输通过不代表硬件解码通过。
4. 沙箱无默认音频端点：0x80070490。实际声音及音画同步仍未验证。
5. 默认 HWND 错误文件负面测试保留真实 0x80070002，属于预期通过，不是剩余缺陷。
6. 本轮运行架构仅 win-x64；不冒称已重新验证 win-x86。

最初暂停切换超时的沙箱诊断位置：C:\Users\Administrator\AppData\Local\WPFMediaFoundationPlayer\Diagnostics\rendering-20261022-113621-68b61b68133c454fae97ffaa8f135dd4.log。该位置在远端任务用户目录；可供本地直接复核的完整异常及事件证据已保存至 player-final-baseline/self-test.log。
