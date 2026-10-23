# MKV 媒体诊断

## 目标媒体的真实证据

已分段读取本机 `session-20261023-013104416-50928-5673a5cd700d4216b207ff2ec89cedbe.log`，并搜索该日志目录的 track/Codec/MFT/EBML；当时只有此文件命中。

- sourceId=DA773766F1A84D6B，文件大小 3976124729 字节（第 4 行）。
- 第 71、145 行：V_MPEGH/ISO/HEVC，1920x1040；hvcC 前缀 `01022000000090000000000078F000FCFDFAFA00000F04200001001840010C01`。按 HEVCDecoderConfigurationRecord 字段解析：profile_idc=2（Main 10）、level_idc=120（Level 4）、亮度/色度位深均为 10。这是配置记录声明，不是解码输出验证。
- 第 72、146 行：A_TRUEHD，日语，48000 Hz，6 声道，24 bit。
- 第 73–74、147–148 行：两条中文 S_HDMV/PGS 字幕，第二条 default=0。
- 第 81–89 行：同 sourceId 在 HWND/MFPlay 下重新打开成功，时长 01:47:19.433、1920x1040，PLAY/PAUSE 成功。旧日志没有实际音频选轨，不能据此声称 TrueHD 已被选择并解码。
- 第 29–45、104–119 行：D3D11 VIDEO_SUPPORT、DXGI manager、Media Engine 创建均成功。
- 第 50–57、124–131 行：本地 MFCreateFile 和 SetSourceFromByteStream 成功，contentTypeHint=unset，随后异步报 SRC_NOT_SUPPORTED / MF_E_TOPO_CODEC_NOT_FOUND (0xC00D5212)，输出帧数为 0（61、135 行）。
- 第 69、143 行：旧探测返回 DirectoryNotFoundException / 0x80070003，只有 EBML 回退信息，没有目标媒体的 MFT 候选清单。不能把沙盒 MP4 的微软 AAC/H.264 候选冒充该影片的候选。

结论：已定位为 Media Engine frame-server 打开/拓扑阶段失败，而不是共享纹理传输失败；尚不能确定阻塞的是 HEVC Main 10 的 D3D11 路径、TrueHD 选轨，还是源处理器/拓扑选择差异。HWND 成功反驳“系统完全没有可用解码能力”，但不证明两个后端使用同一解码器或选择同一音轨。

## SDK 与管线差异

已核验本机 Windows SDK 10.0.26100.0 的 mfmediaengine.idl 第 1691–1700 行：公开创建 flags 包括 AUDIOONLY、WAITFORSTABLE_STATE、FORCEMUTE、REAL_TIME_MODE、DISABLE_LOCAL_PLUGINS。当前 CreateInstance(0) 没有禁用 local plugins，不存在可凭空增加的“允许第三方 MFT”flag。

MFPlay 自行构建播放拓扑及原生渲染器；当前 Media Engine 是带 DXGI manager、BGRA 输出的 frame-server。MFT 注册候选不证明该候选被选中，也不证明其具体 profile、位深、D3D11 设备管理及输出协商受支持。AAC/DTS/AC3 不是本文件的真实音轨，不应以这些测试编码推断 TrueHD。

IMFMediaEngineEx.SetSourceFromByteStream 接收字节流和 URL 提示，不接收 IMFMediaSource；不能把 SourceResolver 返回的媒体源直接传给它。本次把独立元数据探测改为 MFCreateFile(LocalPath) → SourceResolver.CreateObjectFromByteStream，采用与播放入口一致的匿名扩展名提示，不重新 Load、不丢音轨。MKV 的 MIME 提示保持 unset，不凭猜测强制 MIME；日志明确记录这一选择。

## 本次实现

- 本地日志 RegisterSource 保留 `sourceId` 与完整 `sourcePath=source.LocalPath`；保留大小写不敏感的文件源去重。
- 本地媒体路径、file URI 不再过度脱敏；网络 URI 仍整体替换，移除凭据及 token 查询。日志仅在本机存储，不自动上传。复制诊断菜单和提示明确告知含本地路径，分享前检查隐私。
- 探测使用本地字节流，避免旧转义文件 URL 导致的 0x80070003；媒体源 Shutdown 后不重复 Close 同一流，避免清理错误覆盖结果。
- MFT 候选记录名称、CLSID、硬件 URL 属性是否存在及 HRESULT；候选不冒充实际选中的解码器。未映射 TrueHD GUID 时明确 unknown，不编造 subtype。
- HWND 记录 HasAudio/HasVideo 的存在与选中状态及所有 MFPlay stream selection；独立 MF presentation 的 selected 仅为源默认值。
- EBML 增加 hvcC profile/level/位深解析，并以真实日志前缀作为回归输入。

## 构建与实测

发布目录：`WPFMediaFoundationPlayer/publish/mkv-evidence`（Release、win-x64、framework-dependent，需 .NET 10 Desktop Runtime）。

Windows 沙盒：

- `--diagnostics-test` 通过：路径保留、网络秘密移除、异常链、HRESULT、重复 source 去重；结果 `results/mkv-evidence-diagnostics/diagnostics-result.log`。
- `--media-probe-test` 最终通过：HEVC 配置解析、既有 EBML 边界/取消测试，以及仓库 MP4 的字节流 MF 轨道和候选枚举；结果 `results/mkv-evidence-probe-final/media-probe-result.log`。首次实验暴露重复 Close 问题，已修复后重跑。
- `--source-path-test self-test.mp4`：两后端的 plain/special/literal-percent 通过；两后端 long 均以 0x80070003 失败；整体退出 1。结果 `results/mkv-evidence-path/self-test.log` 第 244、401 行。未将此回归报告为全部通过，未扩展修改无关长路径策略。

未在宿主执行程序，未复制或运行目标 4 GB MKV；不能宣称其播放问题已经修复。

## 用户下一次采集

1. 运行上述新发布版本，打开原 MKV，先以 HWND 播放，再点击“更多 → 媒体诊断（轨道与解码器）”。
2. 切换 D3DImage，保留失败自动诊断和原 HRESULT。
3. 在本地 `%LOCALAPPDATA%/WPFMediaFoundationPlayer/Logs` 检查新日志：sourcePath/sourceId、MFPlay audio/video/track selected、MF resolver、MF track/subtype、MFT candidate/hardware/CLSID、失败链。
4. 如 SourceResolver 成功而 Media Engine 失败，可排除独立元数据源解析失败，但仍需比较实际选轨和解码兼容性。若需要改成自建 MF 解码/呈现拓扑，先报告这些证据及方案供选择；本次未引入 FFmpeg、未静音或跳过音轨、未擅自改后端架构。

## 探测边界

COM/MF 元数据探测在线程池运行；同步 MF 调用不能由 CancellationToken 强制中断，取消在轨道间及 EBML 读取检查。EBML 只读、随机跳过 Cluster，最多 4 MiB、100000 元素头、256 轨，校验父子长度；未知长度 Cluster 无法安全跳过时拒绝，不扫描载荷。容器元数据和注册候选均不等于播放成功。
