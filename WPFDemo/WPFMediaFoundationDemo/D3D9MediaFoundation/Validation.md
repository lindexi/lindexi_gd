# D3D9 发布与沙盒验证

## 发布

使用 .NET 9、Release、自包含运行时，未修改 TFM/SDK：

- `dotnet publish WPFMediaFoundationDemo/D3D9MediaFoundation/D3D9MediaFoundation.csproj -c Release -r win-x64 --self-contained true -o .coding-agent/d3d9-publish/win-x64`
- `dotnet publish WPFMediaFoundationDemo/D3D9MediaFoundation/D3D9MediaFoundation.csproj -c Release -r win-x86 --self-contained true -o .coding-agent/d3d9-publish/win-x86`

两次发布成功。视频随发布目录部署。

## 真实 EXE 验证

将各自发布目录推送到 Windows sandbox，直接执行 `D3D9MediaFoundation.exe --self-test`，不是 dotnet test 或模拟入口。

| 架构 | 最终结果 | 时间 | sandbox task |
| --- | --- | --- | --- |
| X64 | `RESULT kind=SourceReader9 frames=300 selfTest=True` | 12.0102820 秒 | 9c4980c64aef4ad7aba9384b60cdcdba |
| X86 | `RESULT kind=SourceReader9 frames=300 selfTest=True` | 12.0074623 秒 | e31ed9864fe74aff9ef6e470eeab114a |

自检在 4 秒时调用 MoveWindow 缩放窗口，保持原来的至少三帧成功阈值。计数仅在 StretchRect 和 Present 均成功后增加。结果拉取到 `.coding-agent/d3d9-results/passed-win-x64-final`、`.coding-agent/d3d9-results/passed-win-x86`。

## 失败诊断与修复

1. 原实现两架构首次 ReadSample 均失败，HRESULT `0xC00D36B4` (MF_E_INVALIDMEDIATYPE)，并非窗口初始化失败。
2. 在新建的 `Shared/VideoDevice.SourceReader9.cs` 中实现独立 D3D9 Source Reader 会话，DesktopRunner 仅对 SourceReader9 分派。优先保留 DXVA2 device manager、NV12 GPU surface、MFGetService、StretchRect、Present 路径。
3. 仅在 GPU 路径零帧且 ReadSample 明确返回该 HRESULT 时，记录错误并重建无 D3D manager 的软件解码 reader。其它 HRESULT 不吞掉，也不通过降低自检标准掩盖失败。
4. 软件 reader 解码成功后，沙盒适配器创建 NV12 offscreen surface 返回 `0x8876086C` (D3DERR_INVALIDCALL)。兼容路径将 NV12 转换并上传 A8R8G8B8 surface，再调用同一 D3D9 StretchRect → Present 呈现链路。没有替换为 D3D11、空窗口、伪造帧计数或跳过解码。
5. 最终两架构都走兼容路径完成 300 帧。硬件 DXVA2 分支在此沙盒不能成功验证；不能将此结果宣称为硬解通过。当前验证基于仓库自带视频，未覆盖所有输入格式、色彩空间或任意 stride 的媒体。

未修改 WpfPresenter、NativeCallback、MediaSession。

## D3D11 Source Reader 音频核实

当前共享 `MediaSession.InitializeReader` 先 SetStreamSelection(ALL_STREAMS, false)，再仅启用 FIRST_VIDEO_STREAM；Tick 也只调用 ReadSample(VideoStream)。不存在音频 reader、音频 sink/WASAPI 输出或音频主时钟。因此 SourceReader11 音频确实未恢复；本任务没有恢复音频，D3D9 兼容 reader 同样仅处理视频。300 帧通过不代表音频通过。

保留原 Source Reader 视频路径的完整恢复方案：独立读取/解码音频为 PCM，送入 Media Foundation audio renderer sink 或 WASAPI，以音频播放时钟同步视频，并使循环、关闭、设备丢失与错误传播一致。不能仅重新选中音频流就声称恢复，因为 Source Reader 不会自动播放音频。另开 MediaEngine 播放同一文件虽可输出声音，但形成双解码与双时钟，不能作为可靠同步恢复方案。

本轮优先完成 D3D9 双架构，遵守共享 MediaSession 的并行修改边界，因此音频只完成代码核实和方案评估，尚无声音/同步运行验证；后续需由负责会话的代理协调实施。
