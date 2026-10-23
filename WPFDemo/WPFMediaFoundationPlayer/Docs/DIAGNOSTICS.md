# 播放诊断

普通启动即由 Demo 订阅 Trace（Core/Wpf 不安装全局监听器）。日志路径：

`%LOCALAPPDATA%\WPFMediaFoundationPlayer\Logs\session-yyyyMMdd-HHmmssfff-PID-GUID.log`

每次进程会话独立文件，启动保留最近 19 个旧文件；当前文件合计最多 20 个（并发进程或删除失败时可能暂时超出）。后台有界队列 4096 条，约 500ms flush，退出最多等待 3 秒。写盘/队列失败通过 stderr 和资源化 UI 提示，不向播放抛出异常。

更多菜单支持打开日志文件夹、复制诊断信息；播放和模式切换失败显示 HRESULT 与完整日志路径。日志中的媒体位置替换为类型/匿名 ID；异常保留 ToString 的堆栈/异常链结构，媒体 URI 和 Windows 路径脱敏。日志文件自身路径单独记录于文件头。

定位字段：UTC、thread、Startup/process/architecture/OS/runtime、WPF tier/processMode、Adapter name/vendor/device/LUID/driverVersion/queryHRESULT、stage/HRESULT、D3D creation flags/fallback、SharedTexture size/format/bind/misc、MediaEngine event、播放/seek/rate/position、D3DImage first frame/progress/front buffer、Switch begin/restore/complete/failed。

高频帧调用成功不记录，MediaEngine TIMEUPDATE 不记录；呈现和播放器进度最多每 5 秒一次，即使无首帧也保留进度状态。驱动版本查询不支持时记录查询 HRESULT，不猜测版本。

## 自测和本次验证边界

`MediaFoundation.PlayerDemo.exe --diagnostics-test` 使用与普通启动相同的初始化路径，验证启动、完整异常类型/内层 HRESULT、日志完整路径及媒体脱敏；成功生成 `diagnostics-result.log`。现有 `--self-test self-test.mp4 --d3dimage` 可继续用于真实播放与 D3D 故障记录。

本次 Release 构建与 x64 自包含发布成功。沙箱首次运行未拉取到预期结果文件；修正自测读取日志的共享模式后，重试被远端连接重置/超时阻断。未取得运行成功证据，不能声称诊断自测或真实播放通过，也未声称原 D3D 根因已修复。
