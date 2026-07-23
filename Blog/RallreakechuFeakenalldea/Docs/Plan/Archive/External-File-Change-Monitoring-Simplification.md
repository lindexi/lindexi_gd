# EditorViewModel 外部文件变更职责收敛方案

## 结论

建议采用一个中等粒度的重构：新增 `ActiveDocumentFileChangeService`，把“当前文件监控的生命周期、磁盘基线比较、本应用保存过滤、同批通知合并”从 `EditorViewModel` 移出。

`EditorViewModel` 只保留真正属于界面协调的两项业务决策：

- 文档没有草稿修改时，从磁盘重新加载；
- 文档存在草稿修改时，显示确认面板并处理用户选择。

不建议把整个文件打开、保存和确认面板都塞进新服务，也不建议新增接口、依赖注入、事件队列或复杂取消模型。本次目标是收敛职责，不是建立一套通用文件同步框架。

## 当前实现核对

当前 `EditorViewModel` 中与外部文件变更有关的代码分散在以下位置：

1. 构造函数中订阅 `ActiveFileChangeMonitor.FileChanged`；
2. `CurrentEditorModel` 切换时停止旧监控、清除忽略状态并异步激活新标签；
3. 文件加载完成后更新 `LoadedFileDiskState`；
4. 文件保存期间设置 `_editorModelBeingSaved`，避免把自己的写入识别成外部修改；
5. 标签激活时检查离开期间是否发生磁盘修改；
6. 管理监控的启动、停止和忽略状态；
7. 过滤旧标签通知、重复通知和本应用保存通知；
8. 比较文件路径与磁盘状态；
9. 决定自动重载还是显示确认面板。

对应的状态字段包括：

- `_activeFileChangeMonitor`；
- `_monitoredEditorModel`；
- `_editorModelBeingSaved`；
- `_isActiveFileMonitoringSuppressed`；
- `_isHandlingActiveFileChange`。

这些状态彼此高度相关，描述的是同一个“活动文档文件同步会话”，放在 `EditorViewModel` 中会掩盖编辑器本身的打开、保存和标签管理主流程。

另外，现有代码与旧版方案文档已经存在两点偏差：

- 当前代码已经没有 `_hasPendingActiveFileChange` 和补偿循环，因此无需再次设计待处理队列；
- 当前实现使用的是 `Task.Yield()`，并不是 100 毫秒延迟。它的作用是让出当前 UI 调度，使同一轮已经排队的文件通知有机会进入单次处理保护，而不是等待外部程序完成写入。

## 哪些逻辑属于必要保护

不能为了减少代码量而把所有检查都删除。下面几项仍有明确价值，但应集中放进新服务。

| 保护 | 是否保留 | 原因 |
| --- | --- | --- |
| 单次处理保护 | 保留 | 一次保存可能产生多个 `Changed`、`Created` 或 `Renamed` 通知；异步读取或等待用户选择期间，后续通知仍可能进入，必须避免并发重载、重复弹窗以及第二次确认被当成忽略 |
| 本应用保存过滤 | 保留 | 保存过程中磁盘状态已经变化，但这不是外部修改，不能触发重载 |
| 标签切换后的过期通知过滤 | 保留 | `Dispatcher.UIThread.Post` 中可能还排着旧监控对象发出的通知 |
| 调度让步后的状态复查 | 保留 | `Task.Yield()` 后重新确认监控目标、当前标签和磁盘基线，过滤同批通知、状态已经同步或标签已经切换的情况 |
| 每个标签的磁盘基线 | 保留 | 非活动标签重新激活时，需要判断离开期间文件是否被修改 |
| 待处理标记和补偿循环 | 不引入 | 当前业务不要求无损处理高频连续外部写入 |
| 版本队列、哈希比较、重试循环 | 不引入 | 超出当前需求，且会再次增加状态和异常分支 |

这里的目标不是取消必要保护，而是让这些保护只在一个类里出现一次。单次处理保护不能依赖“第一次处理完成后，后续通知自然失效”这一假设，因为文件读取和用户确认都是异步过程；第一次处理完成以前，后续通知已经可能再次进入处理流程。

## 方案比较

### 方案 A：只在 EditorViewModel 内部继续压缩

可执行的调整包括：

- 合并 `ActivateEditorModelSafelyAsync`、`ActivateEditorModelAsync` 和 `CheckFileChangeOnActivationAsync`，在一个方法中完成当前标签确认、首次加载、重新激活检查、异常处理和监控启动；
- 把磁盘状态更新与比较改成更短的辅助方法；
- 保留 `Task.Yield()`，并把它明确为 UI 调度让步；如果迁移后的通知入口已经天然形成调度边界，也可以在验证重复通知行为后删除，但不改成固定短延迟；
- 把 `CheckFileChangeOnActivationAsync` 的单次判断直接并入激活方法；其他内部函数只在迁移后仅剩单次转发且不承担异常处理、生命周期管理或独立业务语义时内联。

优点：

- 改动文件少；
- 行为风险最低。

缺点：

- 五个监控状态字段仍然留在 `EditorViewModel`；
- 文件监控基础设施和 UI 决策仍然混在一起；
- 预计只能减少几十行代码，不能真正解决职责膨胀。

该方案适合只做一次短期整理，但不建议作为最终结构。

### 方案 B：提取活动文档文件变更服务

新增一个只服务于当前活动标签的协调类，把监控相关状态整体迁移出去。

优点：

- `EditorViewModel` 能明显缩短；
- 状态和状态转换集中，后续更容易单独测试；
- 不改变现有 UI、文件读取器和标签模型；
- 不需要设计通用抽象。

缺点：

- 项目总代码行数不一定明显下降，只是代码被放回更合适的位置；
- 新服务会依赖 `EditorModel`，但项目中的 `TempDocumentAutoSaveService` 已经采用相同方向，此依赖可以接受。

这是本方案推荐的选择。

### 方案 C：提取完整的文档文件服务

让新服务同时负责打开、保存、监控、重载和确认交互。

该方案虽然能让 `EditorViewModel` 最短，但会让业务服务直接接触 `TextEditor`、Avalonia UI 线程和确认面板，形成新的大类，并没有真正降低复杂度。因此本次不采用。

## 推荐设计

### 1. 新增 ActiveDocumentFileChangeService

建议文件位置：

- `SimpleWrite/Business/FileHandlers/ActiveDocumentFileChangeService.cs`

该服务是对现有 `ActiveFileChangeMonitor` 的上层协调，不替代底层 `FileSystemWatcher` 包装。

服务负责：

- 持有并启停 `ActiveFileChangeMonitor`；
- 记录当前受监控的 `EditorModel`；
- 记录当前是否处于本应用保存过程；
- 记录用户是否忽略了当前活动文档的后续通知；
- 串行处理一次有效通知，处理期间忽略重复通知；
- 在让出一次 UI 调度后重新确认活动文档和磁盘状态；
- 更新和比较 `LoadedFileDiskState`；
- 把确认后的“有效外部变更”通过异步回调交给 `EditorViewModel`。

服务不负责：

- 读取文件内容到 `TextEditor`；
- 设置 `SaveStatus`；
- 显示或持有确认面板；
- 决定用户草稿是否可以被覆盖；
- 管理标签集合和当前标签属性。

### 2. 建议的最小 API

| API | 用途 |
| --- | --- |
| 构造函数接收 `Func<EditorModel, Task>` | 有效外部变更确认后，异步交给 `EditorViewModel` 处理 |
| `ActivateAsync(EditorModel, bool checkDiskState)` | 激活标签，可选检查标签离开期间的磁盘变化，然后启动监控 |
| `Deactivate()` | 标签切换时停止当前监控并使已排队通知失效 |
| `MarkSynchronized(EditorModel)` | 文件成功加载或保存后，记录新的磁盘基线 |
| `BeginLocalSave(EditorModel)` | 返回一个作用域，在本应用保存期间忽略该文档产生的文件通知 |
| `Suppress(EditorModel)` | 用户选择忽略后，停止当前文档监控 |
| `Dispose()` | 释放底层监控对象 |

这里不需要为服务再设计接口。当前只有一个使用方，也没有替换实现的需求；直接使用具体类更简单。

### 3. 服务内部状态

原来位于 `EditorViewModel` 的以下状态迁入服务：

- `_activeFileChangeMonitor`；
- `_activeEditorModel`，替代 `_monitoredEditorModel`；
- `_localSavingEditorModel`，替代 `_editorModelBeingSaved`；
- `_isSuppressed`；
- `_isHandlingChange`。

不增加 `_hasPendingChange`，也不增加通知队列。

`LoadedFileDiskState` 建议继续保留在 `EditorModel`，原因是磁盘基线属于每个标签自己的文档状态。若把它搬进服务，就需要额外维护按 `DocumentId` 索引的字典和标签释放生命周期，反而更复杂。

### 4. 单次通知处理模型

服务收到 `FileSystemWatcher` 通知后只执行一次以下流程：

1. 当前没有活动文档、已被忽略、正在处理其他通知或正在保存该文档时，直接返回；
2. 标记正在处理；
3. 调用 `Task.Yield()` 让出一次 UI 调度，使同一轮已经排队的通知有机会先经过单次处理保护；
4. 再次确认通知路径仍属于当前活动文档；
5. 再次确认活动文档没有切换，也没有进入本应用保存状态；
6. 读取当前 `FileDiskState`，与 `LoadedFileDiskState` 比较；
7. 状态确实不同时，等待 `EditorViewModel` 的异步处理回调完成；
8. 在 `finally` 中解除单次处理标记。

如果步骤 7 尚未完成时又收到通知，直接忽略。不要记录“还有一次待处理”，也不要在回调完成后循环补偿。

`Task.Yield()` 不承诺等待外部程序完成写入，也不保证所有同批通知都先于当前延续执行；它只形成一次明确的调度让步，让已经排队的通知有机会被单次处理保护过滤。当前没有证据表明需要人为增加固定等待时间；未来若确认存在读取外部写入中间状态的问题，再单独评估防抖或重试策略。

### 5. EditorViewModel 保留的处理逻辑

`EditorViewModel` 中最终只需要保留一个外部变更回调，其职责可以概括为：

1. 确认目标仍是当前标签；
2. `SaveStatus == SaveStatus.Saved` 时调用 `LoadFileToTextEditorAsync`；
3. 否则等待 `ExternalFileChangeConfirmationViewModel.ShowAsync`；
4. 用户选择重新加载时调用 `LoadFileToTextEditorAsync`；
5. 用户选择忽略时调用服务的 `Suppress`。

这部分涉及当前标签和用户交互，继续留在 ViewModel 是合理的，不应继续下沉。单次处理保护还必须保证同一文档不会并发调用 `ShowAsync`：当前确认面板在已有确认任务时会立即返回 `Decision.Ignore`，该兜底结果不能被误当成用户主动选择忽略并触发 `Suppress`。

## EditorViewModel 接入点

### 1. 构造函数

不再直接订阅 `ActiveFileChangeMonitor.FileChanged`，只创建服务并传入外部变更处理回调。

### 2. 标签切换

`CurrentEditorModel` setter 中只做两件与文件监控相关的事情：

- 立即调用 `Deactivate()`，停止旧标签监控；
- 在新标签加载或激活检查完成后调用 `ActivateAsync(...)`。

服务在每次激活时重置该活动会话的忽略状态，从而保持当前“切走再切回后重新检查磁盘状态”的行为。

### 3. 文件加载完成

`LoadFileToTextEditorAsync` 在成功读取并设置 `SaveStatus.Saved` 后调用 `MarkSynchronized(editorModel)`。

### 4. 文件保存

保存代码使用 `BeginLocalSave(editorModel)` 包住实际文件写入。写入和刷新成功后调用 `MarkSynchronized(editorModel)`，离开作用域后恢复正常通知处理。

这样 `_editorModelBeingSaved` 不再出现在 `EditorViewModel`，同时仍然保留对自身保存事件的可靠过滤。

### 5. 标签重新激活

- 首次打开且刚刚完成文件加载时，调用 `ActivateAsync(editorModel, checkDiskState: false)`，因为基线已经是最新状态；
- 已经创建过编辑器、只是从其他标签切回来时，调用 `ActivateAsync(editorModel, checkDiskState: true)`，检查离开期间的外部修改。

## 可以同步简化的 EditorViewModel 代码

完成服务提取后，可继续做以下不改变业务的整理：

1. 合并 `ActivateEditorModelSafelyAsync`、`ActivateEditorModelAsync` 和 `CheckFileChangeOnActivationAsync`，保留一个 `ActivateEditorModelAsync`，在其中直接完成当前标签确认、首次加载或重新激活检查，并捕获预期的 `IOException`、`UnauthorizedAccessException` 后启动监控；
2. 删除 `StartActiveFileMonitoring` 和 `StopActiveFileMonitoring`；
3. 删除 `ActiveFileChangeMonitorOnFileChanged` 和 `HandleActiveFileChangeSafelyAsync`；
4. 删除 `HasFileChangedOnDisk`、`UpdateLoadedFileDiskState` 和 `PathEquals`；
5. 删除五个监控状态字段，只保留一个 `ActiveDocumentFileChangeService` 字段；
6. 对迁移后仅剩单次转发的其他内部函数继续评估内联，但保留承担异常边界、资源生命周期或独立业务语义的方法；
7. 删除 `HandleActiveFileChangeSafelyAsync` 时一并消除名不副实的 `Safely` 命名；合并后的 `ActivateEditorModelAsync` 明确负责捕获预期文件异常，不再需要用 `Safely` 表达这一点。

预计外部文件变更相关代码在 `EditorViewModel` 中可由约 170 行收敛到 40 至 60 行。项目总代码不会同比减少，但监控实现将成为一个边界清楚、可独立阅读的组件。

## 明确不做的设计

本次不引入：

- `_hasPendingActiveFileChange` 或补偿循环；
- `Channel`、消息队列或后台常驻工作线程；
- 每次通知一个 `CancellationTokenSource` 的防抖实现；
- 文件内容哈希比较；
- 自动重试读取和指数退避；
- 多标签同时建立 `FileSystemWatcher`；
- 通用接口和依赖注入注册；
- 把确认面板 ViewModel 注入业务服务。

这些能力只有在确认存在高频外部写入、自动生成文件、多人同步编辑或多个 ViewModel 复用需求后才值得加入。

## 行为边界与待确认事项

### 1. 连续真实外部修改

如果第一次外部变更仍在自动重载或等待用户选择，第二次真实修改可能被忽略。这是本方案主动接受的边界。

### 2. 用户选择忽略

建议第一阶段保持当前语义：忽略后停止当前活动标签的监控；切换到其他标签再切回来时，重新检查磁盘状态。

可选的后续优化是：用户手动保存当前文档后视为冲突已经解决，自动恢复监控。该行为应单独确认，不要在职责提取时顺便改变。

### 3. 文件删除

当前 `FileDiskState.TryRead` 在文件不存在时返回 `false`，因此删除通知不会被识别为“状态不同”。本次重构只迁移现有行为，不扩展文件删除提示。

### 4. 磁盘状态精度

当前只比较文件长度和最后写入时间。极少数“长度相同且时间戳未变化”的修改可能无法识别。本次不引入内容哈希。

## 建议实施范围

预计修改：

- 新增 `SimpleWrite/Business/FileHandlers/ActiveDocumentFileChangeService.cs`；
- 修改 `SimpleWrite/ViewModels/EditorViewModel.cs`。

预计保持不变：

- `ActiveFileChangeMonitor.cs` 的底层监听事件；
- `FileDiskState.cs` 的状态读取规则；
- `EditorModel.LoadedFileDiskState`；
- `ExternalFileChangeConfirmationViewModel` 和对应视图；
- `TextFileReader`；
- 临时快照自动保存流程；
- 标签关闭和标签运行时状态恢复流程。

## 建议实施顺序

1. 新建 `ActiveDocumentFileChangeService`，先完整迁移五个状态字段及磁盘状态辅助方法；
2. 将原 `ActiveFileChangeMonitorOnFileChanged` 的过滤和单次处理逻辑迁入服务；
3. 通过异步回调把“有效外部变更”交回 `EditorViewModel`；
4. 将加载、保存和标签切换接入服务 API；
5. 删除 `EditorViewModel` 中已失去职责的方法和字段；
6. 最后再合并标签激活辅助方法，避免同时移动职责和改写流程导致审查困难。

## 验证建议

代码实施后至少验证：

1. 外部修改一个已保存文档时，当前标签自动重新加载；
2. 当前文档有草稿修改时，只显示一个外部变更确认面板；
3. 一次外部保存产生多个监控通知时，不重复加载或重复弹窗；
4. 本应用保存当前文件时，不触发外部变更处理；
5. 保存非当前标签时，不影响当前标签的文件监控；
6. 快速切换标签后，旧标签已排队的通知不影响新标签；
7. 标签离开期间文件被修改，切回时能够检测；
8. 用户选择忽略后，当前活动期间不再持续提示；
9. 用户选择重新加载后，磁盘基线正确更新，不立即再次提示；
10. 文件读取失败时不产生未观察的异步异常；
11. 项目构建成功，现有打开、保存、另存为、关闭和临时快照行为不受影响。
