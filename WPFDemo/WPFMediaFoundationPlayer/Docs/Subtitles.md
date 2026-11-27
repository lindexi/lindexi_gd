# 字幕

## 公开 API

`MediaFoundation.Core` 提供不可变的 `SubtitleCue(Start, End, Text)` 和 `SubtitleDocument`。时间区间为 `[Start, End)`，允许片段重叠。文档按开始时间排序，`GetText(TimeSpan position)` 返回该位置所有活动片段，以换行分隔；查询不依赖上次播放位置，因此支持跳转和循环。

`new SubtitleDocument(IEnumerable<SubtitleCue> cues)` 验证、复制并稳定排序片段，允许空文档。片段须非空，开始时间非负，结束晚于开始且文字非空；无效片段通过包含索引的 `ArgumentException` 报告。任意文本格式的解析器均可创建文档，无需更改时间轴或展示层。

`SubtitleReader.Parse(string text, SubtitleFormat format, double? frameRate = null)` 委托独立格式解析器；`SubtitleReader.LoadAsync(string path, CancellationToken cancellationToken = default, double? frameRate = null)` 加载 UTF-8（允许 BOM）文件。`.sub` 按内容区分 MicroDVD 与 SubViewer；其它扩展名为 `.srt`、`.vtt`、`.lrc`、`.mpl`。`frameRate` 仅用于 MicroDVD，未声明帧率时必须由调用方提供，声明冲突时报错。`SubtitleFormat` 只列出内置格式，不限制文档来源。解析失败抛出带格式、行号和原因的 `FormatException`；文件访问错误保留原异常。调用者负责将错误转为业务结果。

`MediaFoundation.Controls.MediaPlayer` 提供 `SubtitleFailed` 事件：字幕展示失败时关闭字幕，通过 `PlaybackFailedEventArgs.Error` 保留原始错误，音视频播放不因此停止。宿主负责向用户呈现操作结果。

`MediaFoundation.Controls.MediaPlayer` 新增 `SubtitleDocument? Subtitle` 和 `TimeSpan SubtitleDelay` 属性。正延迟表示字幕晚出现。赋值、展示和属性通知均在所属 Dispatcher。加载操作与播放器分离：调用者可异步加载成功后赋值，失败不会替换正在显示的字幕。Open、Close、Dispose 清除字幕和延迟。`MediaPlayerControl.Player` 暴露同一入口，不重复提供一套属性。

## 支持范围

支持外挂 SRT 和 WebVTT 的文本时间轴，包括多行、重叠和 WebVTT NOTE。文字按统一字幕样式显示；SRT 的 b/i/u 标签及 WebVTT 的文字标记不保留作者样式。WebVTT 的时间戳标记用于提取完整句子，不实现逐字显现。定位、区域和 STYLE 不属于文本模式，加载时明确报错，不静默丢弃。外挂 ASS/SSA 和图形字幕不在文本解析入口的支持范围；本库不添加字幕原生依赖。其它文本编码应由调用者解码后使用 Parse。

新增文本格式：

| 格式 | 时间和文本语义 |
| --- | --- |
| LRC | 多时间戳、元数据及全局毫秒 offset；正 offset 提前；同时间文字合并，空时间点清屏，末尾文字延续至时间轴结束；增强逐字时间明确拒绝 |
| MicroDVD | 帧号区间、文件帧率声明或显式帧率、竖线换行；样式控制明确拒绝；Demo 加载要求文件有帧率声明 |
| SubViewer 2.0 | INFORMATION/SUBTITLE 区段、百分秒时间、多行及 `[br]` 换行；样式和非零 DELAY 等未实现设置明确拒绝 |
| MPL2 | 十分之一秒区间、竖线换行；斜体控制明确拒绝 |

## 内嵌文本字幕

`EmbeddedSubtitleReader.LoadAsync(path, cancellationToken)` 在后台扫描本地可定位文件，返回只读 `EmbeddedSubtitleTrack` 列表。轨道提供 `Id`、`Name`、`Language`、`Codec`；`Document` 非空表示可选文本时间轴，否则 `UnsupportedReason` 说明不可用原因。空列表表示没有字幕轨；容器损坏抛带路径、偏移上下文及原异常的 `IOException`。取消通过 `OperationCanceledException` 报告。整个操作不持有长期媒体句柄，不改变播放后端。

| 容器与编码 | 支持范围 |
| --- | --- |
| Matroska S_TEXT/UTF8 | TimestampScale、Cluster 相对时间、BlockDuration、DefaultDuration、后续同轨时间或 Segment 时长；未知长度 Segment/Cluster；不解码 lacing、压缩、加密及轨道时间变换 |
| MP4 tx3g | 非分片、自包含样本；UTF-8 或带 BOM 的 UTF-16，普通默认文本配置；样式与定位不支持 |
| MP4 wvtt | 非分片 vttc/payl、iden 和空样本；作者样式、布局及富文本不支持 |

MP4 支持 stco/co64、固定或可变样本大小、常见单段正常速率编辑及前置空编辑；分片、加密、ctts、复杂编辑或其它字幕编码保持轨道可见并报告不可用。每表最多 100 万项，单样本/文本最多 16 MiB，超限明确失败。读取只保留字幕及必要索引，不把视频载荷读入内存。网络、直播与受保护内容不通过本地文件 API 读取。

Demo 的“字幕 → 内嵌字幕 → 读取字幕轨道”列出轨道，选择可用轨道后赋给 `Player.Subtitle`；读取失败保留正在显示的字幕，媒体切换使旧结果失效。关闭字幕沿用同一入口。

## 展示与生命周期

Core 只处理文本和时间轴。WPF 使用原生媒体位置查询字幕，不使用独立累计时钟。D3DImage 在视频区域叠加；HWND 使用透明、不激活、输入穿透的独立窗口，通过 Owner 关联宿主顶层窗口。字幕按视频等比显示区域定位，并随宿主布局、可见性、最小化和 DPI 更新。卸载、关闭媒体或释放播放器时关闭字幕窗口。

Demo 默认 D3DImage。右键菜单提供加载、关闭和延迟调整；加载失败显示操作错误，保留已有字幕。异步加载结果只应用于发起操作时的媒体。
