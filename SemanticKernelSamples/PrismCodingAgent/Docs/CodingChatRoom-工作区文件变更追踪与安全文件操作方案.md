# CodingChatRoom 工作区文件变更追踪与安全文件操作方案

## 1. 文档目的

本文为 `CodingChatRoom.AvaloniaShell` 设计一套面向编程场景的工作区文件逻辑能力，覆盖：

- 追踪工作区内文件的新增、修改、删除和移动；
- 提供移动文件、移动目录工具；
- 提供删除文件、删除目录工具；
- 删除操作进入应用自定义回收站，而不是系统回收站；
- 为文本文件保存可比较的基线内容，支持类似 Git 的文件列表和差异查看；
- 为后续界面直接展示“哪些文件发生了变化”提供稳定的查询模型；
- 在不破坏 `AgentLib` 通用文件工具定位的前提下，把编程特化能力放在合适的层次。

本文只设计逻辑层，不涉及 Avalonia 界面、样式和交互布局实现。

## 2. 当前代码结论

### 2.1 当前工具装配链路

现有链路如下：

```text
CodingChatStartup
  → CodingAgentOptions.AdditionalToolSources
  → CodingAgent
  → CodingWorkspaceToolProvider
  → CodingWorkspaceToolSession.CreateAsync
  → WorkspaceToolProvider.CreateDefaultToolRegistrations
  → CodingWorkspaceToolLease
  → 单次 CodingAgent 运行
```

关键代码位置：

- `AgentLib/AgentLib/Tools/WorkspaceTools_/WorkspaceToolProvider.cs`
- `AgentLib/AgentLib.Coding/CodingWorkspaceToolProvider.cs`
- `AgentLib/AgentLib.Coding/CodingWorkspaceToolSession.cs`
- `AgentLib/AgentLib.Coding/CodingWorkspaceToolLease.cs`
- `AgentLib/AgentLib.Coding/ICodingWorkspaceToolSource.cs`
- `ChatRoom/Code/CodingChatRoom.AvaloniaShell/Services/CodingChatStartup.cs`

`CodingWorkspaceToolSession` 当前直接创建通用 `WorkspaceToolProvider`，并把默认文件工具、Roslyn 工具、.NET 工具、图片工具和宿主附加工具合并为一次工作区会话的稳定工具集合。

### 2.2 当前文件写入能力

`WorkspaceToolProvider` 当前提供：

- `ReadFileLines`
- `WriteFileContent`
- `ReplaceStringInFile`
- `MultiReplaceStringInFile`
- 目录列举和搜索工具

已有写入保护机制：

1. 读取文件时记录 `FileSnapshotInfo`；
2. 覆写已有文件前要求文件已经读取；
3. 通过文件长度和最后写入时间判断读取后是否发生外部修改；
4. 写入绝对路径时限制在工作区范围内；
5. 新文件写入时自动创建父目录。

这套机制解决的是“避免模型误覆盖未知或已变化内容”，不是“记录工作区变更”。当前没有：

- 工作区基线；
- 变更状态模型；
- 修改前内容保存；
- 文本差异计算；
- 文件移动和目录移动；
- 安全删除和恢复；
- 工作区变更查询 API；
- 外部文件变化检测。

### 2.3 工作区生命周期

`CodingWorkspaceToolProvider` 已经有较完整的工作区切换事务和租约机制：

- `PrepareWorkspaceChangeAsync`
- `IWorkspaceChangeTransaction.Apply`
- `RollbackAsync`
- `CommitAfterPublish`
- `CodingWorkspaceToolLease`

这意味着“变更追踪服务”应该跟随 `CodingWorkspaceToolSession` 创建和释放，而不是由 ViewModel 临时创建。工作区切换后，旧会话仍可能被正在执行的 CodingAgent 使用，因此旧变更服务也必须遵守现有租约生命周期，不能在界面切换工作区的瞬间直接释放。

## 3. 总体设计结论

建议采用以下分层：

```text
AgentLib
  ├─ 保留通用 WorkspaceToolProvider
  ├─ 抽取少量可复用的路径解析、写入前校验和原子写入能力
  └─ 不承载“回收站、基线、代码差异、变更列表”等编程语义

AgentLib.Coding
  ├─ CodingWorkspaceChangeTracker
  ├─ CodingWorkspaceFileOperations
  ├─ CodingWorkspaceRecycleBin
  ├─ CodingWorkspaceDiffService
  ├─ CodingWorkspaceFileTools
  └─ 把通用读取/搜索工具与编程特化写入工具组合成最终工具集合

CodingChatRoom.AvaloniaShell
  ├─ 在组合根提供应用数据目录
  ├─ 持有并暴露当前工作区变更查询入口
  └─ 后续把查询结果映射为 Avalonia ViewModel
```

核心判断：

- **变更追踪、应用回收站和文本 Diff 都是编程代理特性，应主要放在 `AgentLib.Coding`。**
- **不建议直接把这些行为加入 `AgentLib` 的默认 `WorkspaceToolProvider`。** 否则所有使用 AgentLib 通用文件工具的宿主都会隐式获得持久化基线、回收站目录和状态生命周期，职责过重。
- `AgentLib` 可以提供无业务语义的底层工具方法，例如安全路径解析、原子文件替换、文件快照校验；但是否创建回收记录、如何定义变更状态、如何展示 Diff，应由 `AgentLib.Coding` 决定。
- `CodingChatRoom.AvaloniaShell` 不应自行实现文件工具。Shell 只负责传入数据目录、选择配置以及消费变更状态。

## 4. 功能边界

### 4.1 第一阶段目标

第一阶段应完整支持：

- 工作区基线初始化；
- 新增、修改、删除、移动状态识别；
- 文本文件 Unified Diff；
- 文件和目录移动；
- 文件和目录安全删除；
- 回收站记录查询；
- 从回收站恢复；
- 查询当前工作区全部变化；
- Agent 工具执行与 Shell 查询看到同一份状态；
- 工作区切换和并发运行时状态正确。

### 4.2 第一阶段非目标

- 不实现 Avalonia 界面；
- 不实现 Git 暂存区、提交和分支；
- 不把系统 Git 作为必需依赖；
- 不追踪工作区外文件；
- 不将 `bin`、`obj`、`.git` 等默认排除目录纳入变化面板；
- 不对二进制文件生成逐行 Diff，只报告二进制内容已变化；
- 不保证恢复被用户从应用回收站目录手工删除的数据；
- 不把应用回收站放在代码工作区内部。

## 5. 变更语义

### 5.1 基线定义

“变化”必须相对于明确基线计算。建议把基线定义为：

> 当前代码工作区成功发布时，工作区中所有受追踪文件的状态。

这与 Git 仓库是否存在无关，也不会把用户在打开 CodingChatRoom 前已有的未提交修改错误地归因于 CodingAgent。

默认情况下，一次工作区会话只有一份基线。后续可以增加“将当前状态设为新基线”操作，但不应在每次 Agent 运行结束时自动重置，否则用户无法持续查看多轮对话累积的修改。

### 5.2 变更类型

建议定义：

```text
WorkspaceChangeKind
  Added
  Modified
  Deleted
  Moved
  TypeChanged
```

说明：

- `Added`：基线不存在，当前存在；
- `Modified`：基线和当前都存在，但内容不同；
- `Deleted`：基线存在，当前不存在；
- `Moved`：由受控移动操作明确记录的路径变化；
- `TypeChanged`：同一路径从文件变目录或从目录变文件。

对于“先新增再删除”的文件，最终不应出现在变化列表中；对于“先移动再修改”的文件，应保留 `Moved` 关系并标记内容也发生变化。

### 5.3 移动识别

不建议只依赖扫描结果通过内容哈希猜测移动，因为多个相同文件会造成歧义。规则建议为：

1. 通过 `MoveWorkspaceEntry` 工具执行的移动，记录确定的源路径和目标路径；
2. 外部移动只能在源文件消失、目标文件新增且内容哈希唯一匹配时推断为移动；
3. 无法唯一推断时，按一个 `Deleted` 和一个 `Added` 报告。

## 6. 核心逻辑组件

### 6.1 CodingWorkspaceChangeTracker

职责：

- 初始化工作区基线；
- 保存受追踪文件的基线元数据和文本内容引用；
- 接收受控文件操作事件；
- 扫描并发现非受控外部变化；
- 生成规范化的变化列表；
- 对外发布变化通知；
- 在工作区会话释放时停止监听并释放资源。

建议公开契约：

```text
ICodingWorkspaceChangeTracker
  WorkspacePath
  RefreshAsync(cancellationToken)
  GetChangesAsync(cancellationToken)
  GetFileDiffAsync(path, cancellationToken)
  AcceptCurrentStateAsBaselineAsync(cancellationToken)
  ChangesChanged event
```

其中 `GetChangesAsync` 返回不可变快照，不能直接暴露内部可变集合。

### 6.2 CodingWorkspaceBaselineStore

职责：

- 保存基线清单；
- 保存需要生成 Diff 的原始文本内容；
- 保存哈希、文件长度、时间戳和文件类型；
- 支持按相对路径读取基线内容。

基线不建议只保存在内存中。原因：

- 大文本会增加进程长期内存占用；
- 工作区切换后旧租约可能仍存活；
- 应用异常退出后，持久化基线便于诊断和恢复。

建议存储位置：

```text
%LocalAppData%/CodingChatRoom/Workspaces/{workspace-id}/Baseline/
```

`workspace-id` 使用规范化完整路径的 SHA-256，而不是直接使用路径作为目录名。

建议结构：

```text
Workspaces/{workspace-id}/
  workspace.json
  baseline.json
  objects/
    {content-sha256}
  recycle-bin/
  operations/
```

`objects` 使用内容寻址，重复内容只保存一次。首版可以先直接复制文本文件，接口仍按内容存储设计，后续再做压缩或去重。

### 6.3 CodingWorkspaceFileOperations

这是所有“受控修改”的统一入口，职责：

- 校验源路径和目标路径位于工作区内；
- 禁止操作工作区根目录本身；
- 禁止目标路径穿越工作区；
- 处理文件和目录冲突；
- 执行写入、替换、移动、删除、恢复；
- 在修改前让 ChangeTracker 捕获必要的基线或操作前快照；
- 修改成功后通知 ChangeTracker 刷新对应路径；
- 失败时不产生虚假的成功记录。

建议所有编程写工具最终都调用该组件，不允许每个工具自行使用 `File.WriteAllText`、`File.Move`、`Directory.Move`。

### 6.4 CodingWorkspaceRecycleBin

职责：

- 把删除项移动到应用私有目录；
- 生成回收记录；
- 防止同名删除互相覆盖；
- 支持恢复到原路径或指定新路径；
- 支持永久清理单条记录；
- 支持按保留天数或总大小清理。

建议每次删除生成一个唯一 `recycleId`，目录结构为：

```text
recycle-bin/{recycle-id}/
  manifest.json
  content/
```

`manifest.json` 至少包含：

```text
RecycleId
WorkspaceId
OriginalRelativePath
EntryKind
DeletedAtUtc
ContentLength
OperationId
```

删除目录时，直接把整个目录移动到 `content`。应用数据目录通常和工作区不在同一卷，因此不能假设 `Directory.Move` 一定可用。实现需要：

1. 优先尝试原子移动；
2. 跨卷时复制到回收站临时目录；
3. 校验复制结果；
4. 把临时目录原子改名为正式回收项；
5. 最后删除工作区源项；
6. 任一步失败都保留可诊断状态，不能静默丢失数据。

### 6.5 CodingWorkspaceDiffService

职责：

- 比较基线文本与当前文本；
- 生成行级 Unified Diff；
- 为新增和删除文件生成完整新增/删除 Diff；
- 对二进制、大文件和不可解码文件返回摘要；
- 限制最大输入和最大输出，避免把超大 Diff 直接加载到内存或模型上下文。

建议输出模型：

```text
WorkspaceFileDiff
  RelativePath
  PreviousRelativePath
  ChangeKind
  IsBinary
  IsTruncated
  OldLineCount
  NewLineCount
  Hunks
```

Diff 算法可先使用 Myers 行差异算法。若项目已有合适依赖可复用，否则在 `AgentLib.Coding` 内实现一个小而独立的文本差异组件，不建议引入完整 Git 库只为生成 Diff。

## 7. 变更发现策略

仅使用 `FileSystemWatcher` 不够可靠，仅在工具调用后登记也不完整。建议采用“受控操作日志 + 文件系统观察 + 按需校准扫描”的混合模式。

### 7.1 受控操作日志

所有 Agent 文件写入、替换、移动、删除和恢复都经过 `CodingWorkspaceFileOperations`，因此可以立即得到精确变化：

- 操作类型明确；
- 移动源和目标明确；
- 删除对应回收记录明确；
- 可在工具返回前更新变化列表。

这是主要数据来源。

### 7.2 FileSystemWatcher

用于提示“工作区可能被外部修改”，例如：

- 用户通过 IDE 编辑文件；
- 测试或生成器修改源码；
- 外部工具重命名文件。

Watcher 事件只做脏标记和防抖，不直接作为最终状态。原因是事件可能重复、丢失、乱序，也可能只看到目录级变化。

### 7.3 校准扫描

以下时机调用 `RefreshAsync`：

- 工作区基线创建完成后；
- 受控操作完成后，只扫描受影响路径；
- FileSystemWatcher 防抖后；
- 一次 CodingAgent 运行完成后；
- Shell 主动请求变化列表前，如果当前状态为脏；
- Watcher 报告缓冲区溢出后执行全量扫描。

全量扫描只比较受追踪文件的元数据；长度或时间戳可疑时再计算内容哈希。不能在每个文件系统事件上立即遍历整个工作区。

## 8. 文件追踪范围

### 8.1 默认排除目录

复用 `CodingWorkspaceToolSession` 当前默认排除项：

- `.git`
- `.vs`
- `artifacts`
- `bin`
- `obj`
- `TestResults`

建议额外排除应用自身可能创建的临时目录，但应用回收站本身位于工作区外，因此不需要特殊排除。

### 8.2 .gitignore

`AgentLib.Coding` 已引用 `GitignoreParserNet`。建议变更追踪默认遵守工作区根目录的 `.gitignore`，但要保留配置项：

```text
RespectGitIgnore = true
```

工具直接操作一个被忽略文件时，操作本身仍可执行，但默认不进入变化列表。后续可以允许 Shell 显示“被忽略的变化”。

### 8.3 文本和二进制

建议基线内容保存规则：

- 对合理大小的文本文件保存完整基线内容；
- 对二进制文件只保存哈希和元数据；
- 对超过阈值的文本文件只保存哈希和可选首尾摘要，不生成完整 Diff；
- 文件类型判断不能只依赖扩展名，应结合 NUL 字节、解码结果和大小限制。

建议初始默认值：

```text
MaxTextBaselineFileSize = 4 MiB
MaxDiffInputFileSize = 4 MiB
MaxDiffOutputCharacters = 200,000
```

这些值应放入 `CodingWorkspaceChangeTrackingOptions`，不要散落为多个魔法数字。

## 9. Agent 工具设计

建议新增以下工具。

### 9.1 MoveWorkspaceEntry

```text
MoveWorkspaceEntry
  sourcePath
  destinationPath
  overwrite = false
```

统一处理文件和目录，避免模型先判断类型再选择不同工具。行为：

- 源必须存在且位于工作区内；
- 目标必须位于工作区内；
- 禁止移动工作区根目录；
- 禁止把目录移动到自身子目录；
- 默认禁止覆盖；
- 首版即使 `overwrite = true`，也应先把目标安全删除到回收站，再执行移动，而不是永久覆盖。

如果希望工具契约更明确，也可以拆成 `MoveFile` 和 `MoveDirectory`。从模型调用稳定性和减少工具数量考虑，推荐统一工具。

### 9.2 DeleteWorkspaceEntry

```text
DeleteWorkspaceEntry
  path
  recursive = false
```

行为：

- 文件可直接删除到应用回收站；
- 空目录可直接删除到应用回收站；
- 非空目录要求 `recursive = true`；
- 禁止删除工作区根目录；
- 返回 `recycleId`、原相对路径和删除项类型；
- 不提供绕过回收站的模型工具。

### 9.3 RestoreWorkspaceEntry

```text
RestoreWorkspaceEntry
  recycleId
  destinationPath = null
  overwrite = false
```

默认恢复到原路径。目标已存在时默认失败；允许覆盖时，现有目标也必须先进入回收站。

### 9.4 GetWorkspaceChanges

```text
GetWorkspaceChanges
  refresh = true
  includeIgnored = false
  maxResults = 500
```

返回结构化变化摘要，供 Agent 在完成任务前自检，也供 Shell 逻辑层复用。Shell 不应通过调用 AI 工具来查询，而应直接调用同一服务 API。

### 9.5 GetWorkspaceFileDiff

```text
GetWorkspaceFileDiff
  filePath
  maxCharacters = 默认值
```

返回单文件 Diff。对于移动文件，应同时返回旧路径和新路径。

### 9.6 ListWorkspaceRecycleBin

提供回收项查询。首版是否暴露给模型可以配置；逻辑层必须具备此能力，Shell 后续恢复界面会使用。

## 10. 现有写工具的改造方式

这是本方案最重要的集成点。

如果只新增移动和删除工具，而保留 `WorkspaceToolProvider.WriteFileContent` 直接写盘，则变更追踪需要依赖事后扫描，无法在同一操作中可靠保存修改前内容。因此建议：

### 10.1 AgentLib 通用层改造

把 `WorkspaceToolProvider` 中以下能力抽取为可复用公共组件，保持业务中立：

```text
WorkspacePathResolver
WorkspaceFileReadGuard
WorkspaceAtomicFileWriter
WorkspaceFileSnapshot
```

或者为 `WorkspaceToolProvider` 增加可选的文件修改执行器：

```text
IWorkspaceFileMutationHandler
  WriteAllText(...)
  ReplaceText(...)
```

默认实现保持当前行为，不启用追踪、不创建回收站，确保 AgentLib 现有调用方行为兼容。

### 10.2 AgentLib.Coding 特化

`CodingWorkspaceToolSession` 创建 `WorkspaceToolProvider` 时传入由 `CodingWorkspaceFileOperations` 实现的 mutation handler。这样：

- 现有工具名称和参数保持不变；
- 现有“先读取再修改”的保护逻辑保持不变；
- 真正落盘前能够捕获原内容；
- 写入后立即更新变更状态；
- AgentLib 通用层不理解基线和回收站。

如果修改 `WorkspaceToolProvider` 的注入点会使其结构明显复杂化，则备选方案是在 `AgentLib.Coding` 中创建完整的 `CodingWorkspaceFileTools`，只复用 AgentLib 的路径解析、行读取和字符串替换组件，并在 Coding 会话中不注册通用层的三个写工具：

- `WriteFileContent`
- `ReplaceStringInFile`
- `MultiReplaceStringInFile`

随后由 Coding 版本以相同工具名重新注册。

**推荐优先采用可选 mutation handler。** 它是最小且兼容的 AgentLib 扩展点，也允许其他宿主自行接入审计、虚拟文件系统或权限策略。

## 11. 对外查询与生命周期

### 11.1 CodingWorkspaceToolSession

建议会话新增持有：

```text
CodingWorkspaceChangeTracker
CodingWorkspaceFileOperations
CodingWorkspaceRecycleBin
```

它们与 Roslyn 工具一起随 Session 退休。`CodingWorkspaceToolLease` 应增加只读的逻辑入口：

```text
ChangeService
```

但 Shell 不能依赖一次临时租约长期展示状态，否则会阻止旧 Session 释放。

### 11.2 CodingWorkspaceToolProvider

建议提供当前已发布工作区的查询租约或状态访问器，例如：

```text
AcquireWorkspaceStateLeaseAsync
```

该租约只用于短时间读取变化快照和 Diff。工作区切换后，Shell 重新获取当前状态，不长期缓存旧服务实例。

也可以由 `CodingWorkspaceToolProvider` 自身转发：

```text
GetWorkspaceChangesAsync
GetWorkspaceFileDiffAsync
GetRecycleBinEntriesAsync
RestoreWorkspaceEntryAsync
```

从 API 简洁性考虑，推荐提供一个只读/管理型 facade：

```text
ICodingWorkspaceState
```

`CodingAgent` 对外暴露当前已提交工作区的 `WorkspaceState` 查询入口。Shell 的 `CodingWorkspaceController` 在工作区成功提交后切换到新的状态对象。

### 11.3 工作区切换

切换流程建议：

```text
PrepareWorkspaceChangeAsync
  → 创建候选 Session
  → 初始化或加载候选工作区基线
  → 启动 Watcher
  → 返回事务

Apply
  → 建立发布屏障

Shell 发布新工作区路径和 WorkspaceState

CommitAfterPublish
  → 新 Session 成为已提交状态
  → 旧 Session 等待租约释放后退休
```

基线初始化失败必须导致候选工作区准备失败，不能发布一个没有可靠追踪状态的工作区。

## 12. 并发与一致性

同一工作区可能出现：

- Agent 工具并行调用；
- 用户 IDE 同时保存；
- FileSystemWatcher 回调；
- Shell 请求刷新；
- 工作区切换；
- 应用关闭。

建议每个工作区会话使用一个异步操作门：

```text
SemaphoreSlim mutationGate
```

规则：

1. 写入、移动、删除、恢复必须串行；
2. Refresh 可以与纯读取并行，但不能与正在提交的文件操作交叉生成中间状态；
3. Diff 从一个变化快照读取，避免读取过程中基线被替换；
4. Watcher 回调不直接执行 I/O，只设置脏标志并调度防抖刷新；
5. `AcceptCurrentStateAsBaselineAsync` 必须独占 mutation gate；
6. 取消发生在文件操作提交前时可以退出，进入不可分割提交阶段后应完成或回滚，不能留下半移动目录。

## 13. 原子性和故障恢复

### 13.1 文本写入

建议改为：

1. 在同目录创建唯一临时文件；
2. 写入并关闭临时文件；
3. 捕获原始内容或确认基线已存在；
4. 使用平台支持的替换/改名完成提交；
5. 更新操作日志和变化状态；
6. 删除残留临时文件。

不能继续把 `File.WriteAllText` 直接作为最终写入路径，因为进程异常时可能留下部分内容。

### 13.2 操作日志

每个受控修改分配 `operationId`，状态至少包括：

```text
Prepared
Committed
Failed
RecoveryRequired
```

日志不需要做成数据库，首版可使用每操作一个 JSON 文件。它主要用于删除跨卷复制、覆盖目标和目录移动失败后的恢复诊断。

### 13.3 符号链接和重解析点

仅用 `Path.GetFullPath` 不能阻止通过工作区内符号链接访问工作区外内容。涉及写入、移动和删除时必须明确策略。

建议首版安全策略：

- 可以移动或删除“链接本身”；
- 不递归跟随目录符号链接；
- 写入目标路径解析过程中遇到重解析点时拒绝；
- 目录复制到回收站时不遍历链接目标。

该规则需要 Windows 和 Unix 分别测试。

## 14. Shell 组合根调整

`CodingChatRoomPaths` 建议新增：

```text
WorkspaceStateDirectory
```

路径：

```text
%LocalAppData%/CodingChatRoom/Workspaces
```

`CodingChatStartup` 创建 `CodingAgent` 时传入：

```text
CodingWorkspaceFeatures
  EnableChangeTracking = true
  EnableSafeDelete = true
  StateRootPath = paths.WorkspaceStateDirectory
```

不建议把这些能力伪装成普通 `AdditionalToolSources`，因为：

- 它们必须拦截现有写工具；
- 它们与 Session 生命周期深度绑定；
- Shell 还需要直接查询同一份状态；
- 单纯附加工具无法保证所有写入口都被追踪。

因此应作为 `AgentLib.Coding` 的正式可选功能配置，而不是仅由 Shell 拼装几个孤立工具。

## 15. 建议的类型与文件布局

建议新增：

```text
AgentLib/AgentLib.Coding/WorkspaceChanges/
  CodingWorkspaceChangeTrackingOptions.cs
  WorkspaceChangeKind.cs
  WorkspaceEntryKind.cs
  WorkspaceChange.cs
  WorkspaceChangeSet.cs
  WorkspaceFileDiff.cs
  ICodingWorkspaceState.cs
  CodingWorkspaceChangeTracker.cs
  CodingWorkspaceBaselineStore.cs
  CodingWorkspaceDiffService.cs
  CodingWorkspaceFileOperations.cs
  CodingWorkspaceRecycleBin.cs
  CodingWorkspaceRecycleEntry.cs
  CodingWorkspaceFileTools.cs
  WorkspaceOperationJournal.cs
```

AgentLib 通用层建议新增：

```text
AgentLib/AgentLib/Tools/WorkspaceTools_/
  IWorkspaceFileMutationHandler.cs
  DefaultWorkspaceFileMutationHandler.cs
  WorkspacePathResolver.cs
  WorkspaceAtomicFileWriter.cs
```

需要调整：

```text
AgentLib/AgentLib/Tools/WorkspaceTools_/WorkspaceToolProvider.cs
AgentLib/AgentLib.Coding/CodingAgentOptions.cs
AgentLib/AgentLib.Coding/CodingAgent.cs
AgentLib/AgentLib.Coding/CodingWorkspaceToolProvider.cs
AgentLib/AgentLib.Coding/CodingWorkspaceToolSession.cs
AgentLib/AgentLib.Coding/CodingWorkspaceToolLease.cs
ChatRoom/Code/CodingChatRoom.AvaloniaShell/Infrastructure/CodingChatRoomPaths.cs
ChatRoom/Code/CodingChatRoom.AvaloniaShell/Services/CodingChatStartup.cs
ChatRoom/Code/CodingChatRoom.AvaloniaShell/Services/CodingChatRuntime.cs
ChatRoom/Code/CodingChatRoom.AvaloniaShell/Services/CodingWorkspaceController.cs
```

## 16. 测试方案

### 16.1 AgentLib 通用层测试

在 `AgentLib.Tests` 中验证：

- 未提供 mutation handler 时行为与当前完全一致；
- 自定义 handler 能接管写入和替换；
- 先读取再写入保护仍然生效；
- 工作区外路径仍被拒绝；
- 原子写入失败不破坏原文件；
- 路径大小写规则符合当前操作系统。

### 16.2 AgentLib.Coding 单元测试

在 `AgentLib.Coding.Tests` 中覆盖：

- 工作区初始化后无变化；
- 新文件显示为 Added；
- 基线文件编辑显示为 Modified；
- 基线文件删除显示为 Deleted；
- 新增后删除不产生最终变化；
- 文件移动显示为 Moved；
- 目录移动正确映射其子文件；
- 移动后修改同时保留移动关系和内容变化；
- 删除进入应用回收站；
- 同名文件多次删除不会覆盖；
- 从回收站恢复到原路径；
- 恢复目标冲突默认失败；
- 删除非空目录且 recursive=false 时失败；
- 禁止删除工作区根目录；
- 禁止把目录移动到自身内部；
- 跨卷回收逻辑在复制失败时不删除源；
- 文本文件生成正确 Unified Diff；
- 二进制文件只返回变化摘要；
- 超大 Diff 被截断；
- `.gitignore` 排除生效；
- Watcher 事件防抖后触发刷新；
- Watcher 溢出后执行全量校准；
- 外部编辑可以被 Refresh 发现；
- 并发修改被串行化；
- 工作区切换后旧租约仍可完成，释放后停止 Watcher；
- 候选工作区初始化失败时不发布新工作区。

### 16.3 Shell 测试

在 `CodingChatRoom.AvaloniaShell.Tests` 中验证：

- `CodingChatRoomPaths` 创建 WorkspaceStateDirectory；
- Startup 正确启用 Coding 工作区特性；
- WorkspaceController 成功切换时同步切换 WorkspaceState；
- 切换失败时保留旧变化状态；
- Runtime 释放时正确释放 CodingAgent 和追踪资源。

## 17. 分阶段实施建议

### 阶段一：建立可拦截的安全写入基础

- 为 `WorkspaceToolProvider` 增加可选 mutation handler；
- 抽取路径解析和原子文本写入；
- 保持现有工具契约和测试兼容；
- 补充 AgentLib 通用层测试。

### 阶段二：实现基线和变化查询

- 实现基线存储；
- 实现扫描、哈希和变化归一化；
- 实现 `GetWorkspaceChanges`；
- 实现文本 Diff；
- 接管现有三个写工具。

### 阶段三：实现移动和应用回收站

- 实现统一移动工具；
- 实现安全删除、回收记录和恢复；
- 实现跨卷复制提交和操作日志；
- 补充目录、冲突和故障恢复测试。

### 阶段四：补充外部变化发现

- 接入 FileSystemWatcher；
- 实现防抖和脏状态；
- 实现增量校准与全量校准；
- 在 CodingAgent 运行结束时刷新变化。

### 阶段五：Shell 逻辑接入

- 扩展 `CodingChatRoomPaths`；
- Startup 启用功能；
- Runtime 和 WorkspaceController 暴露当前 WorkspaceState；
- 暂不实现界面，只保证 ViewModel 将来可以订阅变化并获取 Diff。

## 18. 关键设计决策汇总

1. **主体功能放在 `AgentLib.Coding`，不放在 AvaloniaShell。**
2. **AgentLib 只增加无编程业务语义的文件修改扩展点和安全底层能力。**
3. **变更基线以工作区成功打开时的状态为准，不以 Git HEAD 为准。**
4. **Git 不是依赖；Git 仓库和非 Git 目录都能工作。**
5. **自定义回收站位于应用数据目录，不污染工作区，也不使用系统回收站。**
6. **所有 Agent 写入、移动、删除、恢复统一经过 CodingWorkspaceFileOperations。**
7. **受控操作日志是主来源，FileSystemWatcher 只负责脏通知，扫描负责最终校准。**
8. **移动优先使用受控操作关系，外部移动仅在哈希唯一匹配时推断。**
9. **文本文件提供行级 Diff，二进制和超大文件提供摘要。**
10. **追踪服务跟随 CodingWorkspaceToolSession 生命周期并服从现有租约机制。**
11. **删除工具永远不永久删除；永久清理仅作为 Shell 管理能力提供。**
12. **第一阶段保留现有工具名和参数，降低系统提示词、测试和模型行为的迁移成本。**

## 19. 最终建议

最合适的实现不是在 Shell 中额外监听目录，也不是简单给 `WorkspaceToolProvider` 增加两个 `File.Move`/`Directory.Delete` 方法。

推荐把它作为 `AgentLib.Coding` 的正式可选工作区特性实现：

```text
Coding Workspace Feature
  = 可拦截的现有写工具
  + 统一文件操作服务
  + 工作区基线
  + 变化归一化
  + 文本 Diff
  + 应用私有回收站
  + 移动/删除/恢复工具
  + Shell 可直接消费的状态 API
```

AgentLib 通用层只需要优雅地开放“文件修改执行器”这一处扩展点，并补足路径解析和原子写入等通用能力。这样既能保证 CodingChatRoom 获得完整、可靠、可测试的编程文件体验，也不会让 AgentLib 的默认文件工具被迫承担 Git 类工作区管理职责。