# 外部文件重载时保持光标与选区方案

## 结论

建议只调整“已有文档从磁盘重新加载”这条链路，不修改 `ActiveDocumentFileChangeService` 的文件监控职责，也不在第一阶段引入文本差异算法。

推荐实现如下：

1. `TextFileReader` 只负责异步读取文件文本，不再直接给 `TextEditor.Text` 赋值；
2. 文件读取完成后，在真正替换文本的前一刻捕获 `TextEditor.CurrentSelection`；
3. 全文替换完成后，根据新文档的真实字符数裁剪原选区的两个端点；
4. 原位置仍在新文档范围内时保持原字符偏移；原位置超过新文档末尾时回退到新文档末尾；
5. 使用原来的 `StartOffset` 和 `EndOffset` 重建选区，保留正向或反向选择方向；
6. 立即恢复 `TextEditor.CurrentSelection`，并把最终选区同步写回 `EditorModel.RuntimeSelection`；
7. 继续复用 `MainEditorView` 现有的滚动偏移记录和布局完成后滚动逻辑，不主动把视口滚到文档末尾。

这套方案直接对应“尽量保持光标位置，除非内容太短”的期望：

- 新内容足够长：光标保持原字符偏移；
- 新内容较短：光标安全落到新文档末尾；
- 原来存在选区：尽量保留选区及方向，超出部分收缩；
- 新内容为空：光标和选区统一回退到偏移 `0`。

第一阶段不尝试判断“原光标附近的文字被插入到了哪里”。如果外部程序在光标之前插入或删除大量内容，字符偏移虽然保持不变，但语义位置可能变化；这是本方案主动接受的边界。

## MVVM 与职责边界

本问题涉及文件 I/O、ViewModel 协调、编辑器控件状态和滚动视口。按照当前项目结构，建议继续遵循以下边界：

- `ActiveDocumentFileChangeService`
  - 负责检测当前活动文件是否发生有效外部变化；
  - 负责过滤本应用保存、旧标签通知和重复通知；
  - 不接触 `TextEditor`、光标、选区或 `ScrollViewer`。
- `TextFileReader`
  - 只负责文件打开、编码识别和文本读取；
  - 不决定这是首次加载还是外部重载；
  - 不负责编辑器运行时状态。
- `EditorViewModel`
  - 决定本次是首次加载还是保留状态的外部重载；
  - 协调读取结果、编辑器内容更新、`SaveStatus` 和磁盘同步基线；
  - 不直接操作 `ScrollViewer`。
- `SimpleWriteTextEditor`
  - 封装“全文替换后恢复合法选区”的编辑器行为；
  - 使用 `LightTextEditorPlus` 的真实选区和文档末尾 API；
  - 不感知文件路径、磁盘状态或确认面板。
- `MainEditorView`
  - 继续负责当前编辑器与共享 `ScrollViewer` 的连接；
  - 继续记录 `RuntimeSelection` 和滚动偏移；
  - 第一阶段不新增文件重载业务判断。

这样可以避免把 UI 状态倒灌到文件监控服务，也避免让文件读取器继续同时承担 I/O 和控件更新两类职责。

## 用户可见行为定义

### 1. 单一光标

假设重载前光标偏移为 `oldOffset`，新文档字符数为 `newCharCount`：

- `oldOffset <= newCharCount`：恢复到 `oldOffset`；
- `oldOffset > newCharCount`：恢复到 `newCharCount`；
- `newCharCount == 0`：恢复到 `0`。

等价规则是：

`restoredOffset = Math.Min(oldOffset, newCharCount)`

### 2. 正向选区

例如原选区为 `100 -> 500`：

- 新文档长度为 `800`：保持 `100 -> 500`；
- 新文档长度为 `300`：收缩为 `100 -> 300`；
- 新文档长度为 `50`：两个端点都回退到 `50`，选区折叠为文档末尾光标。

### 3. 反向选区

`Selection.StartOffset` 与 `Selection.EndOffset` 记录了选择方向，不能只使用 `FrontOffset` 和 `BehindOffset` 重建。

例如原选区为 `500 -> 100`，当前活动光标位于 `EndOffset == 100`：

- 新文档长度为 `800`：保持 `500 -> 100`；
- 新文档长度为 `300`：恢复为 `300 -> 100`；
- 新文档长度为 `50`：恢复为 `50 -> 50`。

分别裁剪 `StartOffset` 和 `EndOffset` 后再构造 `Selection`，可以保留仍然有效的选择方向和活动端。

### 4. 滚动视口

目标不是无条件恢复绝对像素位置，而是避免因为全文替换产生的临时末尾光标把视口滚到文档底部。

预期规则：

- 新文档尺寸足够时，尽量保留现有滚动偏移；
- 恢复后的光标不在当前视口内时，允许现有逻辑做最小滚动，使光标重新可见；
- 新文档显著变短时，允许 `ScrollViewer` 将偏移裁剪到新的最大范围；
- 不为了保持旧像素偏移而显示超出新文档范围的空白区域。

## 当前调用链核对

外部文件变化后的实际链路如下：

1. `SimpleWrite/Business/FileHandlers/ActiveFileChangeMonitor.cs`
   - `FileSystemWatcher` 收到 `Changed`、`Created`、`Deleted`、`Renamed` 或 `Error`；
   - 通过 `Dispatcher.UIThread.Post` 把通知发送到 UI 线程。
2. `SimpleWrite/Business/FileHandlers/ActiveDocumentFileChangeService.cs`
   - 过滤非当前文档、本应用保存、已抑制、正在处理和路径不匹配的通知；
   - 再次比较 `LoadedFileDiskState`；
   - 调用构造函数传入的 `HandleExternalFileChangeAsync`。
3. `SimpleWrite/ViewModels/EditorViewModel.cs`
   - `SaveStatus == SaveStatus.Saved` 时自动调用 `LoadFileToTextEditorAsync`；
   - 有草稿时显示确认面板；
   - 用户选择 `ReloadFromDisk` 后同样调用 `LoadFileToTextEditorAsync`。
4. `SimpleWrite/Business/FileHandlers/TextFileReader.cs`
   - `ReadToTextEditor` 完成编码识别和文本读取；
   - 最后执行 `textEditor.Text = await ReadAllTextAsync(file)`。
5. `LightTextEditorPlus`
   - `TextEditor.Text` 使用全选范围替换整篇文档；
   - 全文替换完成后，底层将光标设置为“替换起点 + 新文本长度”；
   - 全选替换的起点是 `0`，因此光标最终位于新文档末尾。
6. `SimpleWrite/Views/Components/MainEditorView.axaml.cs`
   - `CurrentSelectionChanged` 把这个末尾选区写入 `EditorModel.RuntimeSelection`；
   - 布局完成后调用 `UpdateTextEditorScrollViewer`，使末尾光标可见；
   - 结果表现为光标和视口一起跳到文档末尾。

## 根因

问题不在 `ActiveDocumentFileChangeService`，该服务只是决定“何时需要重新加载”。

直接原因是 `TextFileReader.ReadToTextEditor` 把文件读取和编辑器全文替换绑定在同一个方法里，而 `LightTextEditorPlus.TextEditor.Text` 的全文替换语义会把光标移动到新内容末尾。业务代码没有在替换前保存选区，也没有在替换后恢复合法位置。

此外，当前调用方式还有一个容易被忽略的时序问题：

- 如果在开始文件读取前就捕获选区；
- 文件较大或磁盘较慢；
- 用户在等待期间又移动了光标；
- 读取完成后恢复早先捕获的选区；

那么会覆盖用户在读取期间做出的最新光标移动。

因此，必须先完成异步文件读取，再在给 `TextEditor.Text` 赋值的前一刻捕获当前选区。

## 设计目标

本次实现应满足：

1. 自动外部重载后，光标尽量保持原字符偏移；
2. 用户确认“采用磁盘内容”后，采用同样的保持规则；
3. 新内容不足以容纳原位置时，不产生越界选区；
4. 支持空选区、正向选区和反向选区；
5. 不使用 `string.Length` 猜测编辑器文档长度；
6. 不把光标逻辑放进文件监控服务；
7. 不改变首次打开文件的既有行为；
8. 不增加固定延时、调度重试或人为防抖；
9. 不修改只读的 `LightTextEditorPlus` 项目；
10. 不引入新的公共 API、接口或依赖注入层。

## 明确不做

第一阶段不包含：

- 基于 Myers、LCS 或其他文本差异算法的光标映射；
- 按行号和列号恢复光标；
- 根据光标附近文本构造语义锚点；
- 自动寻找相同单词、段落或 Markdown 标题；
- 修改 `LightTextEditorPlus.TextEditor.Text` 的全局赋值语义；
- 为文件刷新单独创建消息队列或取消令牌体系；
- 强行恢复超过新文档范围的旧滚动偏移；
- 顺带调整外部文件冲突确认、撤销栈或临时快照行为。

## 方案比较

### 方案 A：在 ActiveDocumentFileChangeService 中保存光标

不推荐。

原因：

- 该服务当前只依赖 `EditorModel` 和文件监控基础设施；
- 让它访问 `TextEditor` 会把 UI 控件状态带入文件同步服务；
- 服务将同时承担通知过滤和编辑器交互，破坏现有职责收敛结果；
- 未来若更换编辑器控件，文件监控服务也会被迫修改。

### 方案 B：在 MainEditorView 中监听“即将重载”和“重载完成”事件

可以实现，但不建议作为第一选择。

优点：

- View 可以直接捕获和恢复 `ScrollViewer.Offset`；
- 可以完全抑制中间选区事件对运行时状态的影响。

缺点：

- 需要新增 ViewModel 到 View 的请求事件或 UI 服务；
- 文件重载流程会跨越更多对象；
- 需要处理事件订阅、标签切换、异常结束和重复请求；
- 当前问题只需要在同一个 UI 调度周期内恢复选区，没有证据表明必须建立完整的 UI 事务。

如果第一阶段验证后仍存在一帧滚到底部的现象，再升级为该方案更合适。

### 方案 C：读取与应用分离，由 SimpleWriteTextEditor 原子恢复选区

推荐。

做法：

- `TextFileReader` 先返回完整文本；
- `EditorViewModel` 决定是否需要保持选区；
- `SimpleWriteTextEditor` 在同一个同步方法中完成“捕获选区、全文替换、裁剪、恢复”；
- 方法返回最终选区，`EditorViewModel` 同步更新 `EditorModel.RuntimeSelection`。

优点：

- 变化集中且职责清楚；
- 捕获选区发生在异步读取之后，不覆盖用户的新移动；
- 全文替换和恢复在同一 UI 调度周期内完成，不给界面渲染末尾中间态的机会；
- 不需要修改监控服务和确认面板；
- 不需要让 ViewModel 操作 `ScrollViewer`；
- 能覆盖刷新期间切换标签后 `RuntimeSelection` 需要同步裁剪的情况。

## 推荐设计

### 1. TextFileReader 只返回文本

目标文件：

- `SimpleWrite/Business/FileHandlers/TextFileReader.cs`

当前类已经提供 `ReadAllTextAsync(FileInfo)`。建议让 `EditorViewModel` 直接调用该方法，并删除不再使用的 `ReadToTextEditor(FileInfo, TextEditor)` 包装。

理由：

- 编码识别和文件读取属于文件层；
- 何时给编辑器赋值属于上层协调；
- 只有先得到字符串，才能在赋值前一刻捕获最新选区；
- 删除单次转发方法可以避免未来再次绕过选区恢复流程。

读取失败时应保持当前行为：异常向上交给现有调用方处理，且因为尚未替换文本，编辑器内容、选区和磁盘同步基线都不应变化。

### 2. SimpleWriteTextEditor 封装保持选区的全文替换

目标文件：

- `SimpleWrite/Business/TextEditors/SimpleWriteTextEditor.cs`

建议新增一个 `internal` 方法，语义类似：

- 输入：新的完整文本；
- 行为：保存当前选区，设置 `Text`，按新文档末尾裁剪并恢复选区；
- 返回：最终恢复后的 `Selection`。

建议方法签名表达为 `internal Selection ReplaceAllTextPreservingSelection(string text)`。名称需要明确这是“替换全部文本”而不是普通局部编辑，避免以后被错误用于查找替换或用户输入链路。

不建议把它设计成公共 API，也不需要新增接口。当前编辑器创建入口已经统一使用 `SimpleWriteTextEditor`，该能力只服务于 SimpleWrite 自身的重载流程。

方法内部顺序必须固定为：

1. 校验输入文本；
2. 读取当前 `CurrentSelection`；
3. 设置 `Text`；
4. 通过 `GetDocumentEndCaretOffset()` 获取新文档末尾；
5. 分别裁剪原 `StartOffset` 与 `EndOffset`；
6. 使用裁剪后的两个端点重建 `Selection`；
7. 设置 `CurrentSelection`；
8. 返回最终选区。

这里必须使用编辑器的 `GetDocumentEndCaretOffset()`，不能使用 `newText.Length`，原因包括：

- 编辑器内部对换行符有自己的文档字符计数语义；
- 文件文本可能包含 `\r\n`，编辑器文档偏移不应由业务层猜测；
- 以后若底层支持更复杂的字符对象，字符串 UTF-16 长度不一定等于光标坐标系长度。

### 3. 选区裁剪算法

建议按端点处理，而不是按 `FrontOffset + Length` 处理。

伪流程：

```text
oldSelection = CurrentSelection
Text = newText
documentEnd = GetDocumentEndCaretOffset()

newStart = oldSelection.StartOffset 超过 documentEnd 时取 documentEnd，否则保留原值
newEnd = oldSelection.EndOffset 超过 documentEnd 时取 documentEnd，否则保留原值
restoredSelection = Selection(newStart, newEnd)
CurrentSelection = restoredSelection
return restoredSelection
```

关键点：

- `CaretOffset` 不允许负数，因此只需要处理超过新文档末尾的情况；
- 使用 `StartOffset` 和 `EndOffset` 保持选区方向；
- 原端点仍合法时，可以保留其 `IsAtLineStart` 信息；
- 原端点被裁剪时，使用新文档末尾 `CaretOffset`，不要继续携带旧位置的行首标记；
- 两个端点都超过末尾时，选区自然折叠到末尾；
- 空文档的末尾就是 `0`。

### 4. EditorViewModel 区分首次加载与外部重载

目标文件：

- `SimpleWrite/ViewModels/EditorViewModel.cs`

建议保留一个统一的文件加载核心方法，但显式传入是否保持选区，例如私有参数 `preserveSelection`。调用处必须使用命名参数，避免布尔值语义不清。

调用规则：

| 调用场景 | 是否保持选区 | 原因 |
| --- | --- | --- |
| `OpenFileAsync` 首次打开文件 | 否 | 没有需要保留的既有编辑位置 |
| `ActivateEditorModelAsync` 首次延迟加载 | 否 | 属于初始化，不是刷新 |
| `HandleExternalFileChangeAsync` 自动重载已保存文档 | 是 | 当前问题的主要入口 |
| `PromptExternalFileChangeAsync` 用户选择采用磁盘内容 | 是 | 用户只选择替换内容，不应额外丢失可保留的编辑位置 |

统一加载核心流程建议调整为：

1. 校验 `TextEditor` 和 `FileInfo`；
2. 异步读取完整文件文本；
3. 确保后续控件访问运行在 UI 线程；当前调用链可继续保留 `ConfigureAwait(true)`，若未来改成后台延续，则必须显式切回 `Dispatcher.UIThread`；
4. 若 `preserveSelection == false`，按既有方式设置 `Text`；
5. 若 `preserveSelection == true`，调用 `SimpleWriteTextEditor` 的保持选区替换方法；
6. 将返回的最终选区写入 `editorModel.RuntimeSelection`；
7. 设置 `SaveStatus.Saved`；
8. 调用 `ActiveDocumentFileChangeService.MarkSynchronized` 更新磁盘基线。

必须在文件读取完成后、文本赋值前捕获选区。不要在 `await ReadAllTextAsync` 之前保存选区。

当前所有编辑器都由 `EditorViewModel.CreateTextEditor` 创建为 `SimpleWriteTextEditor`。保留模式下应把这一点作为内部不变量：如果实际对象不是 `SimpleWriteTextEditor`，应尽早暴露实现错误，不能静默退回 `textEditor.Text = text`，否则同一缺陷会在异常路径中重新出现。

### 5. RuntimeSelection 必须显式同步

活动编辑器通常会通过 `MainEditorView.CurrentSelectionChanged` 自动更新 `EditorModel.RuntimeSelection`，但重载期间仍需要由 `EditorViewModel` 显式写入最终选区。

原因是文件读取是异步的：

1. 开始读取时，目标标签是当前标签；
2. 用户在读取完成前切换到了其他标签；
3. 旧编辑器从 `MainEditorView` 脱离，选区事件不再写回旧模型；
4. 文件读取完成，旧编辑器在后台流程中完成内容替换和选区裁剪；
5. 如果不显式更新 `RuntimeSelection`，模型中可能仍保存超出新文档长度的旧选区；
6. 下次切回标签时，`RestoreRuntimeState` 会尝试恢复这个过期选区。

因此，保持选区方法返回最终 `Selection`，上层无条件同步到对应 `EditorModel`，可以覆盖活动和非活动两种完成时机。

### 6. MainEditorView 第一阶段保持不变

目标文件：

- `SimpleWrite/Views/Components/MainEditorView.axaml.cs`

第一阶段不建议新增重载事件或滚动事务。

现有事件时序能够配合立即恢复：

1. `TextEditor.Text` 开始替换时，底层先把文档标记为需要重新布局；
2. 全文替换把光标移动到末尾并触发 `CurrentSelectionChanged`；
3. `MainEditorView` 此时无法取得新文档的有效渲染信息，只会等待 `LayoutCompleted`；
4. `SimpleWriteTextEditor` 在同一个 UI 调度周期内立即恢复裁剪后的选区；
5. 等布局真正完成时，现有回调读取的是当前最终光标，而不是先前的末尾中间态；
6. 滚动逻辑因此会围绕恢复后的光标执行。

同时，UI 线程不会在两个同步赋值之间插入一次界面渲染，所以末尾选区只是一段内部中间状态，不应形成用户可见的一帧跳动。

如果实际验证仍发现特定平台存在可见闪动，再追加第二阶段方案：

- 在 `MainEditorView` 中新增一个短生命周期的运行时状态恢复作用域；
- 作用域内忽略中间选区和滚动事件；
- 文本替换后一次性恢复选区与滚动偏移；
- 使用目标 `EditorModel` 和版本号过滤标签切换后的过期恢复。

没有验证到闪动以前，不建议预先引入这套跨 ViewModel/View 协调。

## 完整时序

### 自动重载已保存文档

1. 外部程序保存文件；
2. `ActiveFileChangeMonitor` 把通知投递到 UI 线程；
3. `ActiveDocumentFileChangeService` 确认磁盘状态变化；
4. `EditorViewModel.HandleExternalFileChangeAsync` 发现文档状态为 `Saved`；
5. 调用带 `preserveSelection: true` 的加载流程；
6. `TextFileReader.ReadAllTextAsync` 完成文件读取；
7. 在 UI 线程读取此刻最新的 `CurrentSelection`；
8. 全文替换；
9. 根据新文档末尾裁剪并恢复选区；
10. 同步更新 `EditorModel.RuntimeSelection`；
11. 设置 `SaveStatus.Saved`；
12. 更新 `LoadedFileDiskState`；
13. 新布局完成后，现有滚动逻辑确保恢复后的光标可见。

### 有草稿时用户确认重载

1. 外部文件变化被检测到；
2. `SaveStatus != Saved`，显示确认面板；
3. 用户选择“采用磁盘内容”；
4. 后续流程与自动重载相同，使用 `preserveSelection: true`；
5. 草稿内容被磁盘内容替换，但只要偏移仍合法，光标和选区保持原位置。

### 文件读取期间切换标签

1. 目标标签开始读取磁盘文件；
2. 用户切换到其他标签；
3. 读取完成后，旧编辑器仍可安全更新自身内容；
4. 选区根据旧编辑器当前状态裁剪；
5. 最终选区显式写回旧 `EditorModel.RuntimeSelection`；
6. 不操作当前标签使用的共享 `ScrollViewer`；
7. 下次切回旧标签时，沿现有 `RestoreRuntimeState` 流程恢复合法状态。

## 异常与边界处理

### 1. 新文件更短

这是主要回退场景。任何超过新文档末尾的端点都裁剪到末尾，不允许把越界 `Selection` 交给底层。

底层 `CaretManager` 当前没有完整的选区越界保护，不能依赖编辑器自动修正。

### 2. 新文件为空

`GetDocumentEndCaretOffset()` 返回偏移 `0`。无论原来是光标还是选区，最终都恢复为 `0 -> 0`。

### 3. 新文件更长

原端点继续有效时保持不变。不会因为新增内容位于文档末尾而把光标跟随到新末尾。

### 4. 外部修改发生在光标之前

第一阶段保持字符偏移，不保持语义位置。

例如原光标位于偏移 `1000`，外部程序在文件开头插入 `100` 个字符，恢复后仍位于偏移 `1000`，不会自动移动到 `1100`。

如果以后确认这是高频痛点，再评估差异映射；当前不要为单一跳末尾问题引入复杂算法。

### 5. 换行符变化

外部工具可能把 `CRLF` 改成 `LF`，或反向转换。必须使用编辑器替换后的文档末尾计算合法范围，不能用读取字符串长度裁剪。

### 6. 软换行位置变化

`CaretOffset.IsAtLineStart` 用于区分软换行边界的上一行末尾和下一行开头。

推荐规则：

- 端点偏移没有被裁剪时保留原 `CaretOffset`；
- 端点超出新文档时使用新文档末尾；
- 不在第一阶段等待布局后重新推导软换行侧。

内容、窗口宽度或字体变化可能让旧的软换行侧信息不再完全对应，但字符偏移仍然有效，且该边界比当前无条件跳到文档末尾更可接受。

### 7. 文件读取失败

因为读取和应用已经分离，读取异常发生时尚未修改编辑器：

- 保留原文本；
- 保留原选区；
- 保留原滚动位置；
- 不更新 `LoadedFileDiskState`；
- 继续使用现有异常处理边界。

### 8. 连续外部通知

继续沿用 `ActiveDocumentFileChangeService._isHandlingChange` 的单次处理保护。本方案不增加新的并发状态。

### 9. 重载期间用户移动光标

选区在文件读取完成后才捕获，所以能够采用赋值前的最新位置。

文本真正开始替换以后，捕获、替换和恢复都在同一个 UI 调度周期内同步完成，不存在用户在三者之间继续输入或移动光标的窗口。

### 10. 重载期间用户编辑文本

这是现有外部重载链路的并发业务边界，不属于本次光标修复范围。

若未来要避免“文件读取开始时为 Saved，但读取完成前用户已经输入”的覆盖风险，应单独引入文档版本复查或保存状态复查，不应和光标保持混在同一次修改中。

## 预计修改文件

### 必须修改

- `SimpleWrite/Business/FileHandlers/TextFileReader.cs`
  - 保留纯文本读取；
  - 删除不再使用的 `ReadToTextEditor`。
- `SimpleWrite/Business/TextEditors/SimpleWriteTextEditor.cs`
  - 新增全文替换并保持合法选区的内部方法；
  - 新增端点裁剪辅助逻辑。
- `SimpleWrite/ViewModels/EditorViewModel.cs`
  - 读取文本后再应用；
  - 区分首次加载与外部重载；
  - 外部重载完成后同步 `RuntimeSelection`。

### 第一阶段保持不变

- `SimpleWrite/Business/FileHandlers/ActiveDocumentFileChangeService.cs`
- `SimpleWrite/Business/FileHandlers/ActiveFileChangeMonitor.cs`
- `SimpleWrite/Models/EditorModel.cs`
- `SimpleWrite/Views/Components/MainEditorView.axaml.cs`
- `SimpleWrite/ViewModels/ExternalFileChangeConfirmationViewModel.cs`
- `LightTextEditorPlus` 下的所有项目。

## 建议实施顺序

1. 在 `SimpleWriteTextEditor` 中实现选区端点裁剪与全文替换恢复方法；
2. 让该方法返回最终 `Selection`；
3. 将 `EditorViewModel.LoadFileToTextEditorAsync` 改为先调用 `ReadAllTextAsync`；
4. 为加载核心增加明确的 `preserveSelection` 行为参数；
5. 首次打开和首次延迟加载继续使用不保留模式；
6. 自动外部重载和确认后的磁盘重载使用保留模式；
7. 把保留模式返回的最终选区写入 `EditorModel.RuntimeSelection`；
8. 删除已无调用方的 `TextFileReader.ReadToTextEditor`；
9. 检查受影响文件编译错误；
10. 执行 SimpleWrite 项目构建；
11. 按验证矩阵手工检查光标、选区、滚动和标签切换；
12. 若确认仍有平台相关闪动，再评估 `MainEditorView` 的第二阶段恢复作用域。

## 验证矩阵

### 基础光标

1. 打开一个长文档，把光标放在中间；
2. 外部程序只修改光标之后的内容；
3. 验证自动重载后光标偏移不变；
4. 验证视口没有滚到文档末尾。

### 文档变长

1. 光标放在文档中间；
2. 外部程序在文档末尾追加大量内容；
3. 验证光标仍在原偏移；
4. 验证不会跟随新增内容滚到底部。

### 文档变短但仍包含原位置

1. 光标放在偏移较小的位置；
2. 删除文档后半部分，但保留光标所在范围；
3. 验证光标保持原偏移。

### 文档短于原光标位置

1. 光标放在文档后半部分；
2. 外部程序把文件缩短到原光标之前；
3. 验证光标落到新文档末尾；
4. 验证无异常、无非法选区。

### 空文件

1. 光标放在非零位置；
2. 外部程序清空文件；
3. 验证光标为 `0`；
4. 验证滚动偏移回到有效范围。

### 正向选区

1. 创建一个从前到后的文本选区；
2. 外部修改后让新文档仍包含完整选区；
3. 验证选区范围和方向保持；
4. 再让新文档只包含选区前半部分；
5. 验证选区尾部裁剪到文档末尾。

### 反向选区

1. 从后向前拖出选区；
2. 外部修改但保持文档足够长；
3. 验证 `StartOffset`、`EndOffset` 方向不被颠倒；
4. 缩短文档并验证超出端点被正确裁剪。

### 草稿确认重载

1. 在编辑器中产生未保存修改；
2. 外部程序修改同一文件；
3. 在确认面板选择采用磁盘内容；
4. 验证磁盘内容生效；
5. 验证光标或选区按同一规则恢复；
6. 验证只出现一次确认。

### 读取期间移动光标

1. 使用较大文件或调试断点延长读取过程；
2. 重载开始后移动光标；
3. 验证最终采用赋值前的最新光标，而不是读取开始时的位置。

### 读取期间切换标签

1. 当前标签触发外部重载；
2. 在读取完成前切换到其他标签；
3. 验证当前标签不被旧编辑器的恢复流程滚动；
4. 切回原标签；
5. 验证恢复的是裁剪后的合法选区。

### 回归

1. 首次打开文件行为不变；
2. 新建空白文档行为不变；
3. 本应用保存不会触发外部重载；
4. 用户选择忽略外部变化后行为不变；
5. 标签切换时原有选区和滚动恢复不变；
6. 查找结果选区、状态栏光标信息和快捷键选词行为不受影响；
7. 文件读取失败时不覆盖当前内容；
8. SimpleWrite 项目构建成功。

## 自动化测试建议

当前解决方案中没有独立的 `SimpleWrite.Tests` 项目，本次也没有新增或修改公共 API，因此不建议只为这个缺陷立即搭建完整测试项目。

如果后续已经建立 SimpleWrite 测试工程，可优先为纯裁剪逻辑补充以下测试：

- 空光标在范围内保持不变；
- 空光标超过末尾时裁剪；
- 正向选区完整保留；
- 正向选区部分超出时收缩；
- 反向选区保持方向；
- 两个端点都超出时折叠到末尾；
- 空文档统一回退到零；
- 端点等于文档末尾时不被错误修改。

UI 层仍需保留手工验证，因为最终是否出现可见滚动跳动取决于 Avalonia 布局、`ScrollViewer` 和 `LayoutCompleted` 的组合时序。

## 验收标准

实现完成后，应同时满足：

1. 外部刷新不再无条件把光标移动到文档末尾；
2. 原字符偏移仍合法时保持不变；
3. 原字符偏移越界时安全回退到新文档末尾；
4. 正向和反向选区都不会被错误翻转；
5. 不产生越界选区异常；
6. 视口不再因为末尾中间态滚到底部；
7. 新文档过短时允许滚动偏移按新范围收缩；
8. 首次打开、保存、确认忽略、标签切换和查找选区行为无回归；
9. 不修改 `ActiveDocumentFileChangeService` 和 `LightTextEditorPlus`；
10. SimpleWrite 构建成功。

## 可选的后续增强

只有在字符偏移保持仍不能满足实际使用后，再按成本从低到高评估：

1. 行号与列号恢复
   - 先定位原行，再将列裁剪到新行长度；
   - 对在文件开头插入整行的场景更友好；
   - 但对自动换行、换行符变化和大段移动仍不稳定。
2. 邻近文本锚点
   - 保存光标前后少量文本，在新内容中搜索最接近的匹配；
   - 需要处理重复文本和匹配歧义。
3. 文本差异映射
   - 根据旧文本和新文本建立偏移映射；
   - 语义最接近，但时间和内存成本最高；
   - 大文件需要阈值、取消和降级策略。
4. 完整视口锚定
   - 保存视口顶部对应的文档位置，而不是绝对像素；
   - 适合对“阅读位置”稳定性要求高的场景。

这些增强都不应阻塞当前“刷新后跳到末尾”的最小修复。