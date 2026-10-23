# MKV 打开故障调查（2026-10-22）

## 2026-10-23 新日志核对

完整分段读取 `session-20261023-024511728-28544-67020efdc4cd45e8914b1bf49911d087.log`（96 行）。以下时间均为 UTC，源标识为 `4F51701BD39BF6AE`。

- 02:45:20：`.mkv` 文件存在且可读，4507855237 字节；`mode=Hwnd`，`MediaFoundation.Core: MFPlay Open`。音视频及 track=0/1 均 selected=1；02:45:22.469 切换前记录 `state=Playing position=00:00:01.7107510`。这证明进入播放状态，不等于验证实际声音/画面。
- 02:45:22.519 起切换正式 `D3DImage`；D3D9/D3D11 设备、Media Engine frame-server 创建均成功。02:45:22.649 `MFCreateFile(local)` 成功，02:45:22.651 `IMFMediaEngineEx.SetSourceFromByteStream(local; no Load)` 成功；contentTypeHint=unset，扩展名 `.mkv`。
- 02:45:22.815：失败 stage 为 `IMFMediaEngineNotify.ERROR`，mediaError=4 (`MF_MEDIA_ENGINE_ERR_SRC_NOT_SUPPORTED`)，HRESULT=`0xC00D5212` (`MF_E_TOPO_CODEC_NOT_FOUND`)。随后切换失败，呈现帧数为 0；不是文件打开或图形设备创建阶段失败。
- 后续 MediaProbe：MF track=0 为 HEVC 1920×1080，track=1 为 6 声道 48000 Hz、subtype=`eb27cec4-163e-4ca3-8b74-8e25f91b517e`。两轨各有一个注册候选；音频候选是 `Dolby TrueHD IEC-61937 converter MFT`，不等于实际选中的解码器；视频轨/metadata 查询另报 `0xC00D36E6`。
- EBML 容器声明：视频 HEVC profileIdc=2、10-bit、1920×1080；音频 A_TRUEHD、6 声道、48000 Hz、24-bit；另有两条 PGS 字幕。容器元数据和注册候选不能确定失败属于哪条轨道，也不能证明 profile/位深/D3D11 解码兼容性。此日志不足以断言缺少某个具体解码器或所有 MKV 不受支持。

源码轨道核对：`MediaPlayerControl.OpenFile` 使用 Wpf `Strings.resx` 的 `FileFilter`；正式 HWND 使用 MFPlay，正式 D3DImage 仍使用 Media Engine。`App` 中独立 `--mfplay-d3dimage <媒体文件路径>` 分支才调用 `MfPlayPrototype`（MFPlay + EVR）；原型没有文件选择对话框，“重新打开”仍使用同一命令行路径。本次没有将原型接入正式模式，也未新增 UI 入口。

本次仅扩展文件筛选为媒体/视频/音频/所有文件四类（保留 `*.*`），加入 MKV、WebM、传输流及常见音频扩展；筛选只决定文件可见性，不承诺系统解码能力。Demo 菜单资源明确标注 Media Engine 实验及原型独立入口；不添加依赖或更改后端。

本次验证：`WPFMediaFoundationPlayer.slnx` Release 构建成功，0 警告、0 错误。win-x64 自包含发布后，沙箱 `--diagnostics-test` 通过：新增筛选格式、媒体/视频/音频分类、全部要求扩展（含 MKV）、所有文件保留及 OpenFileDialog.Filter 赋值检查通过，原有诊断自测也通过。结果保存在 `.coding-agent/filter-regression-results`。未重测目标 MKV 播放，不声称解码问题已修复。


## 原日志证据

目标 session：`session-20261022-130447776-55508-1f2f4e9d650240128617f7d3f334f785.log`。已分段读取 MKV 打开至日志结束（1388–1496 行）；以下时间均为 UTC。

| 行 | 时间 | 结果 |
| --- | --- | --- |
| 1388–1390 | 13:06:54.370 | 首个 MKV，sourceId=C46CB5E1AC542823，长度 49，3976124729 字节，可读，mode=D3DImage |
| 1414 | 13:06:54.6823556 | MediaEngine Open；不是 MFPlay |
| 1425 | 13:06:54.6848238 | MFCreateFile(local) 成功，0x00000000 |
| 1427 | 13:06:54.6848913 | IMFMediaEngineEx.SetSourceFromByteStream(local; no Load) 成功，0x00000000 |
| 1433 | 13:06:54.8685293 | IMFMediaEngineNotify.ERROR，mediaError=4，param2/HRESULT=0xC00D5212 |
| 1435–1442 | 13:06:54.872–.883 | 释放流，控件传播相同错误，呈现帧数为 0 |
| 1443–1445 | 13:07:11.186 | 第二个 MKV，sourceId=D9ECAD5DB9AE5706，长度 50，2979383313 字节，可读，D3DImage |
| 1478–1480 | 13:07:11.277 | 本地文件和 SetSourceFromByteStream 同样成功 |
| 1486 | 13:07:11.4250034 | 同样异步失败：mediaError=4，HRESULT=0xC00D5212 |

错误分类：`MF_MEDIA_ENGINE_ERR_SRC_NOT_SUPPORTED`（4）；原生 HRESULT 为 `MF_E_TOPO_CODEC_NOT_FOUND`（0xC00D5212）。这是媒体拓扑构建时找不到所需 codec 的分类，不是文件不存在，也不是图形设备初始化失败。不能仅凭此断言所有 MKV 不支持，或确定缺少哪条音轨/视频轨的解码器。

原日志已匿名化，不能仅凭 sourceId 将两次打开严格对应到用户给出的影片名称。实际 F: 目录可枚举，但没有读取媒体二进制短头；未复制或修改任何用户媒体。

## MIME 核对与边界

MediaEngineSession 的现有表仅包含 MP4/M4V/MOV、MP3、WMV、WMA、WAV。MKV 对应 null，代码不设置 MF_BYTESTREAM_CONTENT_TYPE；匿名 URL 提示为 media.mkv。没有显式设置 application/octet-stream。原日志也没有 IMFByteStream.SetString(MF_BYTESTREAM_CONTENT_TYPE) 调用。

上述结果排除“代码明确把 MKV 标成 MP4/octet-stream”的指控，但不证明底层实际属性值，也不证明 MIME 提示完全无关。未在故障机器对实际媒体做 source resolver/轨道类型枚举及 MIME A/B 测试，因此保留 MIME 表，不臆测添加 MIME 即可修复播放。后续日志会记录应用提供的 contentTypeHint（不是读取回来的原生属性）及匿名扩展名，并输出 SDK 枚举/错误常量分类。

## 本次修复

- RegisterSource 按源去重，重复注册在磁盘探测之前返回；本地 URI 大小写等价源也只记录一次。
- PropertyChanged 仅源通知触发注册，而非每次 Position 等属性通知。
- 所有源隐私别名在写注册日志前登记；补充正斜杠本地路径脱敏。
- 不添加超长路径功能、不升级框架或依赖、不安装 codec、不改变媒体文件。

## 验证

- PlayerDemo Release 构建与 win-x64 自包含发布成功。
- 根 WPFMediaFoundation.slnx Release 构建成功：0 错误，2 个既有 NETSDK1138 警告。根解决方案不含独立播放器项目，因此播放器另行构建。
- 沙箱 --diagnostics-test PASS：重复注册、等价 URI、并发重复注册、媒体路径/URI 脱敏、异常链和 HRESULT 保留。结果：`.coding-agent/mkv-diagnostics-results/diagnostics-result.log`。
- 既有小 MP4 沙箱 --self-test --d3dimage --software-wpf：双后端切换、打开、实际画面、播放/暂停及 Seek 通过；2 倍速时钟断言失败，实测 0.960，退出 1。结果：`.coding-agent/mkv-playback-results`。音频环境不可用（0x80070490），不声称声音验证通过。
- 本次没有证明目标 MKV 已恢复播放，也未证明缺失的具体 codec 或系统容器能力边界。不得将 MP4 回归当作 MKV 修复证明。
