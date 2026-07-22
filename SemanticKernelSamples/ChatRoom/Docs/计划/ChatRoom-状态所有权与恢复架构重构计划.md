# ChatRoom 状态所有权、恢复与线程模型重构计划

## 文档状态

- 状态：待审批
- 交付类型：架构重构计划，不包含实现
- 兼容性策略：项目尚未发布，不保留不合理 API 的兼容性
- 适用范围：`SemanticKernelSamples/ChatRoom/ChatRoom.slnx`
- 明确约束：不启动 .NET 现代化改造流程；不以升级框架或包版本替代设计重构

## 背景

当前问题由 `ChatRoomSession.FromPersistence` 暴露出来，但根因不在某一个方法的可见性，而在整个 ChatRoom 状态模型没有形成不可绕过的所有权边界。

现有链路大致如下：

1. `FromPersistence` 为了恢复消息和 `_lastSpeakTimeByRole`，复用同步 `AddMessage`。
2. `FromPersistence` 实际上只被单元测试调用，生产加载链路由 `ChatRoomManager.LoadAsync` 重新实现。
3. 同步 `AddMessage` 除了服务这个未接入生产的恢复工厂，也只被测试调用。
4. 为允许同步旁路存在，`AddMessageCore` 不能无条件负责线程调度，只能依赖调用者保证调用上下文。
5. 因此线程包装被放到 `AddMessageAsync` 外层，形成“安全入口 + 可绕过核心入口”的结构。
6. 生产加载又直接调用 `Session.Messages.Clear()`，说明即使删除同步 `AddMessage`，集合仍可从其他路径绕过线程和派生状态维护。

这不是单纯的可见性问题，而是以下职责被混在同一组对象中：

- 领域状态；
- UI 可观察集合；
- 流式运行状态；
- 持久化 DTO；
- 会话恢复器；
- 线程调度；
- 角色资源生命周期；
- 文件系统提交；
- 测试构造接缝。

只把 `AddMessage` 改为 `private`、把调度器移动到 `AddMessageCore`，或者让 `FromPersistence` 改调 `AddMessageAsync`，都只能移动症状，不能建立“状态只能从一个受控入口修改”的不变量。

## 调查范围与方法

本次调查覆盖：

- `AgentLib.ChatRoom` 核心领域、运行时、服务和持久化；
- `AgentLib.ChatRoom.Tests`；
- `ChatRoom.AvaloniaShell` 及 `ChatRoom.Shell.Tests`；
- ChatRoom 直接依赖的 `AgentLib` 与 `AgentLib.Coding` 边界；
- 现有 ChatRoom 需求文档、README 与历史计划文档。

调查采用以下证据：

- 编译器符号引用；
- 生产调用链；
- 测试调用链；
- 集合修改点；
- dispatcher 使用点；
- `ConfigureAwait` 后续状态修改；
- 持久化文件所有权和提交顺序；
- friend assembly 声明与 internal API 使用情况。

## 已确认的核心问题

### 问题 1：测试专用恢复与映射路径反向塑造生产 API

#### 证据

- `ChatRoomSession.FromPersistence` 位于 `AgentLib.ChatRoom/ChatRoomSession.cs:209-225`。
- 其所有调用均在 `AgentLib.ChatRoom.Tests/ChatRoomSessionTests.cs`。
- `ChatRoomSession.ToPersistence` 位于 `ChatRoomSession.cs:230-242`。
- 其所有调用同样均在 `ChatRoomSessionTests.cs`。
- 同步 `ChatRoomSession.AddMessage` 位于 `ChatRoomSession.cs:127-133`。
- 除 `FromPersistence` 内部调用外，其余调用均来自测试；由于 `FromPersistence` 没有生产调用，实际生产链不需要该同步入口。
- 生产保存由 `ChatRoomManager.SaveAsync` 在 `ChatRoomManager.cs:664-689` 重新构造 DTO。
- 生产加载由 `ChatRoomManager.LoadAsync` 在 `ChatRoomManager.cs:697-780` 重新实现角色和消息恢复。

#### 根因

测试直接围绕类内部构造步骤编写，而不是通过真实加载、保存和运行用例验证行为。为了方便这些测试，生产类型增加了第二套没有被生产使用的映射 API。

#### 后果

- 生产映射与测试映射可以长期漂移。
- 测试覆盖了死代码，却没有保护真实加载链路。
- 同步 `AddMessage` 作为测试辅助入口削弱了线程契约。
- `FromPersistence` 没有恢复 `CopilotChatMessage`，而生产加载会调用 `RestoreCopilotChatMessage`，两条路径语义已经不同。
- `ToPersistence` 测试还断言 DTO 与运行时共享相同消息对象，固化了浅复制，而不是稳定快照。

#### 结论

删除 `FromPersistence`、`ToPersistence`、同步 `AddMessage` 及其实现细节测试。恢复与保存映射只能存在于一个生产使用的 assembler/repository 边界中。

---

### 问题 2：原位加载导致运行时身份与持久化身份分裂

#### 证据

- `ChatRoomSession.SessionId` 和 `CreatedAt` 在构造后不可变，见 `ChatRoomSession.cs:27-49`。
- `ChatRoomService.LoadSessionAsync` 先创建一个全新空 `ChatRoomSession`，见 `Services/ChatRoomService.cs:131-139`。
- `ChatRoomManager.LoadAsync` 不替换该 Session，只把持久化身份存入 `_persistenceSessionId` 和 `_persistenceCreatedAt`，见 `ChatRoomManager.cs:31-32,89-91,759-760`。
- 后续持久化使用隐藏身份，UI 则继续读取 `manager.Session.SessionId` 和 `CreatedAt`。
- `SessionListViewModel` 使用公开 Session ID 判断当前会话和是否允许删除，见 `SessionListViewModel.cs:201-203,335-368`。

#### 根因

加载被设计为“把另一会话灌入已有 Manager 和 Session”，但 Session 身份不可替换，于是又增加一套隐藏身份补偿。

#### 后果

- UI 所见会话 ID 与实际保存目标不同。
- 加载后的会话可能无法被识别为当前会话。
- 当前会话删除保护可能失效。
- UI 可能生成重复或幽灵会话项。
- 创建时间和显示时间可能错误。
- 目录 ID、DTO ID、运行时 ID 可以形成三套身份。

#### 结论

禁止对已有 Manager 执行原位 `LoadAsync`。加载必须完整构造一个具有持久化身份的新聚合，全部成功后由应用服务原子替换当前会话引用。

---

### 问题 3：公开可变集合使聚合不变量可以被任意绕过

#### 证据

- `ChatRoomSession.Messages` 公开暴露 `ObservableCollection<ChatRoomMessage>`，见 `ChatRoomSession.cs:71-74`。
- `ChatRoomManager.Roles` 公开暴露 `ObservableCollection<ChatRoomRole>`，见 `ChatRoomManager.cs:68-72`。
- `_lastSpeakTimeByRole` 只在 `AddMessageCore` 和 `RemoveMessageCore` 中维护，见 `ChatRoomSession.cs:135-170`。
- `ChatRoomManager.LoadAsync` 直接调用 `Session.Messages.Clear()`，见 `ChatRoomManager.cs:768`，不会清理旧角色的 `_lastSpeakTimeByRole`。
- 正式角色添加需要初始化、工作区同步、模型注册、保存和失败释放，见 `ChatRoomManager.cs:300-335`，但公开 `Roles` 允许直接 `Add`。
- 正式角色移除需要保存、事件和资源释放，见 `ChatRoomManager.cs:464-513`，但公开 `Roles` 允许直接 `Remove`。
- 测试中存在直接 `session.Messages.Clear()` 和直接 `session.AddMessageAsync(...)` 构造场景。

#### 根因

同一个集合同时被当成：

- 聚合内部状态；
- UI 绑定集合；
- 外部可写 API；
- 测试数据构造器。

但集合变更之外还存在派生索引、持久化、事件、资源生命周期和业务验证，因此直接集合操作不可能是安全入口。

#### 后果

- 消息集合与 `_lastSpeakTimeByRole` 失配。
- 直接添加消息绕过 mention 解析、事件和持久化。
- 直接添加角色绕过初始化与模型配置。
- 直接删除角色绕过释放和磁盘状态清理。
- UI 与后台逻辑可能并发读写同一 `ObservableCollection`。

#### 结论

领域聚合不得公开可变集合。外部只读取不可变快照或只读视图；所有修改通过命令协调器完成。

---

### 问题 4：线程调度是局部包装，不是系统不变量

#### 证据

- `ChatRoomSession` 持有 `IMainThreadDispatcher`，并在 `AddMessageAsync`/`RemoveMessageAsync` 中调度集合写入。
- `AddMessageCore` 只能在 Debug 下检查线程，无法自行保证线程上下文。
- `Session.Messages.Clear()`、`Session.Title`、`Roles.Add/Remove/Clear` 等变更不经过同一个 dispatcher 边界。
- `ChatRoomManager.AddRoleAsync` 在多个 `ConfigureAwait(false)` 后直接 `Roles.Add(role)`。
- AI 的 `create_character` 工具可以从模型执行线程触发该链路。
- `RoleListViewModel` 直接订阅 `manager.Roles.CollectionChanged` 并同步刷新 UI 集合，没有 dispatcher 包装。
- `SessionListViewModel` 在 `ConfigureAwait(false)` 后直接修改 `Sessions`、`SelectedSession`、`IsBusy` 并触发导航事件。
- Shell 测试用 `TestMainThreadDispatcher.CheckAccess()` 永远返回 `true`，无法覆盖跨线程路径。
- `ChatRoomSessionTests.AddMessageAsync_WithDispatcher_InvokesOnMainThread` 设置 `CheckAccess=true`，却断言 `InvokeAsync` 被调用，测试逻辑与生产分支相反。

#### 根因

领域状态和 UI 绑定状态是同一对象图，导致核心层必须知道 UI dispatcher；同时又不是所有状态变更都经过该 dispatcher。

#### 后果

- 部分集合被认为需要 UI 线程，另一些集合依赖调用者自行记忆。
- 后台 AI 工具、加载、关闭和服务事件可能直接触发 UI 集合变更。
- `ObservableCollection` 在 UI 线程写入、后台线程读取，仍然不具备线程安全性。
- 测试替身会掩盖真实线程错误。

#### 结论

核心层完全移除 UI dispatcher、`ObservableCollection` 和显示文本。AvaloniaShell 维护自己的 UI 投影，并保证所有 UI 集合只在 Avalonia dispatcher 上修改。

---

### 问题 5：角色定义、持久化 DTO 与运行时对象共用同一可变实例

#### 证据

- `ChatRoomRole` 直接保存传入的 `ChatRoomRoleDefinition` 引用，见 `ChatRoomRole.cs:52-69`。
- `ChatRoomRoleDefinition` 的运行关键字段可被公开修改。
- `ChatRoomManager.UpdateRoleAsync` 原位逐字段修改 Definition，并在失败时逐字段回滚。
- `ChatRoomSessionData.Roles` 和 `Messages` 直接使用运行时类型。
- `ChatRoomManager.SaveAsync` 只复制列表容器，不深复制角色定义和消息。
- SystemPrompt 和 Memory 只在角色首次发言时注入；原位修改 Definition 不代表现有 AgentSession 已应用新配置。

#### 根因

一个对象同时承担：

- 存储 DTO；
- 编辑表单模型；
- 领域配置；
- 运行时配置；
- 角色身份；
- UI 可观察状态。

#### 后果

- 外部可以绕过 `UpdateRoleAsync` 直接修改 Definition。
- UI 显示新配置，但 AgentSession 可能仍使用旧人设和旧记忆。
- 保存过程中活对象继续变化，所谓快照不稳定。
- 每新增一个字段，手工回滚和多套映射都容易遗漏。

#### 结论

角色定义改为不可变值对象。持久化模型使用独立 DTO。角色更新创建新配置版本，并显式指定 AgentSession 迁移策略，而不是原位修改同一个对象。

---

### 问题 6：`ChatRoomManager` 同时承担过多层级职责

#### 当前职责

`ChatRoomManager` 当前同时负责：

- 消息和角色领域状态；
- 发言调度；
- 角色运行时创建和释放；
- 工作区切换；
- 模型 provider 注册；
- 加载和保存；
- 文件日志写入；
- UI 可观察属性；
- UI 线程相关事件；
- 关闭、停止和并发协调。

#### 根因

缺少明确的应用服务、领域聚合、运行时协调器和仓储边界，所有流程逐步堆积到一个类型中。

#### 后果

- `LoadAsync` 既构造资源又修改 UI 集合又替换领域状态。
- `AppendMessageAsync` 既修改内存又发事件又保存多个文件。
- 线程、持久化和领域不变量无法独立测试。
- 测试只能通过 internal 访问器和直接对象注入切开该类型。

#### 结论

拆分为不可变领域状态、角色运行时、单写者命令协调器、应用服务、仓储和 UI 投影。

---

### 问题 7：持久化存在多个事实副本，但没有聚合级原子提交

#### 证据

当前同一轮状态可能写入：

1. `room.config.json` 中的角色和消息；
2. `public_logs` 中的公开文本；
3. 每个角色的 `agent-session-state.json`；
4. 预留但生产未使用的角色私有日志。

相关写入分散在：

- `ChatRoomManager.SaveAsync`；
- `AppendMessageAsync`；
- `HandleAutoLoopMessageAsync`；
- `SaveRoleAgentSessionStateAsync`；
- UI 自动循环 `finally` 中的额外保存。

AI `StepAsync` 完成时可以先写角色 AgentSession 和公开日志，但 `room.config.json` 要到自动循环清理或外部显式 `SaveAsync` 才更新。

会话删除只删除会话目录，公开日志位于独立 `public_logs` 目录。角色删除不会自动删除对应角色状态文件。

#### 根因

文件级 API 由业务流程逐个调用，没有“提交一个完整 ChatRoom snapshot/revision”的仓储操作。

#### 后果

- 公开历史、角色私有上下文和配置可以处于不同轮次。
- 进程崩溃后角色可能记得 UI 中不存在的消息。
- 配置写入成功但日志失败时，操作抛异常但状态已部分提交。
- 重试可能生成重复日志。
- 删除后残留公开日志或角色状态。

#### 结论

仓储必须拥有完整聚合的保存、加载、列表和删除。一次提交包含会话元数据、角色定义、公开消息和角色 AgentSession 状态，并以 revision/generation 原子发布。

---

### 问题 8：并发、停止和关闭契约分散且可以旁路

#### 证据

- 自动循环拥有自己的状态锁。
- 工作区切换拥有独立 `SemaphoreSlim`。
- Coding executor 拥有独立生命周期锁。
- Manager 关闭使用 `_isClosingOrClosed` 和超时退化路径。
- `StepAsync` 可被外部直接并发调用。
- `RemoveRoleAsync`、`UpdateRoleAsync`、`LoadAsync` 和工作区切换没有共享统一操作状态机。
- `Stop()` 只发出取消信号，`StopAsync()` 才等待结束，但服务和 UI 主要暴露同步 Stop。
- `CloseAsync` 超时后可以在角色仍运行、仍未释放时返回，随后后台释放异常只写 Debug。

#### 根因

系统没有一个能够表达聊天室命令顺序和长时执行所有权的协调模型，正确性依赖多个锁、布尔字段、取消令牌和调用约定组合。

#### 后果

- 同一角色可能并发发言并修改同一 AgentSession。
- 删除或关闭可能与发言执行重叠。
- `CurrentSpeaker` 可被并发 Step 覆盖。
- UI 可认为已停止，但旧运行仍在完成或释放。
- `await DisposeAsync()` 完成不等于资源已全部释放。

#### 结论

引入单写者命令协调器和显式 execution lease。所有持久状态变化串行提交；长时模型执行在协调器之外运行，完成或失败通过命令回投。关闭状态单调前进，异步关闭必须等待所有资源完成释放。

---

### 问题 9：ChatRoom 通过 friend assembly 使用 AgentLib.Coding 内部工作区协议

#### 证据

- `AgentLib.Coding.csproj` 向 `AgentLib.ChatRoom` 声明 `InternalsVisibleTo`。
- `CodingChatRoomRoleExecutor` 直接调用 `CodingAgent.CreateWorkspaceCandidateAsync` 和 `PublishWorkspaceCandidateAsync`。
- `CodingWorkspaceToolCandidate` 为 internal 类型。
- ChatRoom 自行协调 `CopilotChatManager.WorkspacePath`、executor `_workspacePath` 和 Coding provider candidate 的提交与回滚。
- `CodingWorkspaceToolProvider` 已有公开 `SetWorkspacePathAsync`，但现有 API 无法把 ChatRoom 的附加状态更新纳入同一个明确的公开事务结果。

#### 根因

AgentLib.Coding 没有提供满足 ChatRoom 需求的公开工作区切换契约，ChatRoom 通过程序集友元进入内部两阶段协议。

#### 后果

- ChatRoom 依赖 Coding 内部 candidate 生命周期。
- Coding 内部实现调整会直接破坏 ChatRoom。
- friend access 掩盖了缺失的公开抽象。
- 回滚责任跨两个程序集分散。

#### 结论

在 AgentLib.Coding 提供公开、完整的工作区切换结果或运行时接口；ChatRoom 只调用公开能力。删除 `AgentLib.Coding` 对 `AgentLib.ChatRoom` 的 friend 声明。

## 目标架构

### 总体结构

```text
ChatRoomApplicationService
├── CurrentRoomHandle
├── IChatRoomRepository
├── IChatRoomRuntimeFactory
└── ChatRoomCoordinator
    ├── ChatRoomState（不可变领域状态）
    ├── RoleRuntimeRegistry（运行时资源）
    ├── CommandQueue（单写者）
    ├── ExecutionLeaseRegistry
    └── EventStream（不可变事件）

ChatRoom.AvaloniaShell
├── ChatRoomProjection
├── ObservableCollection<MessageItemViewModel>
├── ObservableCollection<RoleItemViewModel>
└── IUiDispatcher / Avalonia Dispatcher
```

### 1. 不可变领域状态

建议引入以下概念，最终命名可在实施时按代码风格调整：

- `ChatRoomState`
  - `SessionId`
  - `RoomInstanceId`：每次加载/创建 coordinator 时生成，阻止旧房间迟到事件污染新房间
  - `Title`
  - `CreatedAt`
  - `StateRevision`
  - `PersistedRevision`
  - `NextMessageSequence`
  - `ImmutableArray<ChatRoomMessageState>`
  - `ImmutableArray<ChatRoomRoleDefinition>`
  - 每个角色的 `ConsumedThroughSequence`
  - 当前运行、持久化故障和关闭状态
- `ChatRoomMessageState`
  - `MessageSequence`：房间内单调递增的消费水位
  - `MessageId`
  - 只包含公开、稳定、可持久化的数据
  - 不继承 `NotifyBase`
  - 不引用 `CopilotChatMessage`
  - 不包含 UI 流式对象
- `ChatRoomRoleDefinition`
  - 改为不可变 record/value object
  - 包含稳定 `RoleId` 和不可复用的 `RoleIncarnationId`
  - 所有集合字段改为不可变集合或只读复制
  - 角色名、RoleId、执行种类在构造时验证

领域状态每次命令提交后生成新 `StateRevision`。消息消费水位以 `MessageSequence` 表达，不再使用 `Timestamp` 判断“上次发言以后”的消息；时间戳只用于显示和摘要。`LastActivityAt` 由最后一条已提交消息推导，空会话使用 `CreatedAt`，并冗余写入 manifest 供列表查询。

任何读取方只能读取一个完整版本，不得观察正在 Clear/Add 的中间状态。`WorkspacePath` 不写入持久化领域状态：它是本机文件访问授权和运行时资源状态，重启或重新加载后必须重新授权。协调器仅维护瞬态 `WorkspaceRuntimeState` 与单调 `WorkspaceVersion`。

### 2. 角色运行时与领域定义分离

`ChatRoomRole` 当前同时是领域角色、运行资源和 UI 模型。重构后分为：

- `ChatRoomRoleDefinition`：不可变配置；
- `IChatRoomRoleRuntime`：模型、AgentSession、工具和工作区资源；
- `RoleRuntimeStatus`：可对外查询的只读运行状态；
- `IChatRoomRoleRuntimeFactory`：根据 Definition 和应用依赖创建运行时。
- `CommittedRoleSessionCheckpoint`：最近一次已被领域状态接受的不可变 AgentSession 快照；
- `CandidateRoleExecution`：从 committed checkpoint 创建、与其他状态隔离的本次执行候选。

运行时由 `ChatRoomCoordinator` 独占：

- 外部不能直接构造生产运行时并插入房间；
- 外部不能直接释放运行时；
- 外部不能取得可变 `AgentApiEndpointManager`；
- 发言、更新、删除使用 `RoleId`，由协调器解析运行时。

#### AgentSession 一致性规则

仓储提交时禁止重新读取正在执行中的活 runtime。每个角色必须维护不可变 `CommittedRoleSessionCheckpoint`，至少包含：

- `RoleIncarnationId`；
- `RoleRuntimeVersion`；
- `RoleSessionRevision`；
- `ConsumedThroughSequence`；
- AgentSession serializer version；
- 深复制的序列化 payload。

每次执行从 committed checkpoint 创建隔离的 candidate AgentSession。执行完成后，runtime 返回最终公开结果和新的 candidate checkpoint；`CompleteRoleSpeak` 在同一个 state revision 中同时接受公开消息、推进到本次 `InputThroughSequence` 的消费水位并提升 candidate checkpoint。执行失败、取消、lease 失效或房间已被替换时，candidate checkpoint 必须丢弃。

如果底层 AgentSession 无法可靠复制，runtime 必须从 committed checkpoint 反序列化一个本次执行专用实例，不得直接修改被仓储 snapshot 引用的 committed 实例。仓储失败后的重试必须复用同一个不可变 snapshot 和 checkpoint，不得再次从活 runtime 捕获不同内容。

### 3. 单写者命令协调器

所有持久状态修改通过命令进入同一个协调器，例如：

- `AppendHumanMessage`
- `AddRole`
- `UpdateRole`
- `RemoveRole`
- `StartAutoLoop`
- `RequestRoleSpeak`
- `CompleteRoleSpeak`
- `FailRoleSpeak`
- `ReduceRoleSession`
- `ClearRoleSessionMemory`
- `RespondToApproval`
- `RefreshProviderSnapshot`
- `ChangeWorkspace`
- `StopAutoLoop`
- `CloseRoom`

#### 长时模型执行策略

本计划选择**房间级最多一个活跃 execution**，与当前单一 `CurrentSpeaker` 和串行自动循环语义一致。手动发言和自动循环必须走同一个 `RequestRoleSpeak` 命令，不能保留第二条并发旁路。

模型调用不能长期占用命令循环，否则人类插话无法及时提交。采用以下流程：

1. 命令循环验证角色和房间状态。
2. 为角色创建 `ExecutionLease`，记录：
   - `RoomInstanceId`；
   - `RunId`；
   - `ExecutionId`；
   - `RoleId` 和 `RoleIncarnationId`；
   - `RoleRuntimeVersion`；
   - `WorkspaceVersion`；
   - 发言开始 `StateRevision`；
   - `InputThroughSequence`；
   - 实际输入消息的 ID/序号集合；
   - 取消令牌；
   - 本次执行的 candidate AgentSession 所有权。
3. 命令循环提交“角色开始发言”状态后立即返回。
4. 模型流在协调器之外运行。
5. 流式增量作为瞬态事件发布，不直接修改领域消息集合。
6. 人类插话仍可进入命令循环并提交新消息。
7. 模型完成后回投 `CompleteRoleSpeak` 命令。
8. 协调器验证 room instance、execution、role incarnation、runtime version 和 lease 是否仍有效，再原子追加最终公开消息、接受 candidate checkpoint，并把角色水位推进到 lease 的 `InputThroughSequence`。
9. 完成时不得把消费水位推进到当前最新消息；执行期间到达的人类消息必须留给下一次发言。
10. 同一 execution 的重复 Complete/Fail/Cancel 回投必须幂等；lease 终止后的 delta 和结果全部丢弃。

#### 操作竞争规则

- 人类插话：不取消当前 execution，立即提交新消息；当前 execution 仍只消费其 lease 固定的输入。
- `StopAutoLoop`：取消当前 execution，停止产生新发言请求，并等待 execution 进入终态；不把普通停止解释为“当前发言自然完成”。
- 删除当前发言角色：标记角色 deleting、拒绝新 lease、取消并等待目标 execution、提交角色删除，再释放 runtime。
- 更新当前发言角色：当前 execution 使用旧配置完成；更新在其终态后应用，随后递增 `RoleRuntimeVersion`。如果更新要求清空 AgentSession，则在旧 execution 完成后执行迁移策略。
- 工作区切换：允许由当前 execution 内的工具触发；已有 execution 固定旧 `WorkspaceVersion` 和工具 lease，切换期间禁止启动新 execution，切换成功后的新 execution 使用新版本。
- 关闭和房间替换：使 room epoch/instance 失效，拒绝新命令和迟到结果。

多次人类插话以消息序号保留，不再压缩成单个布尔信号。自动循环根据持久状态和每个角色的 `ConsumedThroughSequence` 重新计算调度。

由此同时满足：

- 持久状态只有一个写入者；
- 人类可以在模型执行期间插话；
- 同一角色不能意外并发使用同一 AgentSession；
- 完成结果不会写入已经被关闭或替换的房间。

### 4. UI 投影层

核心层不再持有：

- `IMainThreadDispatcher`；
- `ObservableCollection`；
- `NotifyBase`；
- `DisplayText`；
- `LastUsageSummaryText`；
- Avalonia 线程假设。

核心层发布不可变事件，例如：

- `RoomStateChanged`
- `RoleAdded/Updated/Removed`
- `MessageCommitted`
- `RoleExecutionStarted`
- `RoleExecutionDelta`
- `RoleExecutionCompleted`
- `RoleExecutionFailed`
- `RoleExecutionCanceled`
- `ApprovalRequested`
- `ApprovalResolved`
- `PersistenceStateChanged`
- `RunStateChanged`

每个执行事件必须携带 `RoomInstanceId`、`ExecutionId`、`RoleId` 和单调事件序号。`RoleExecutionDelta` 使用 UI 中立 DTO 表达文本、推理摘要、Token 用量、工具调用、子代理和审批状态，不暴露 `CopilotChatMessage`。审批响应必须按 `ExecutionId + ApprovalId` 精确路由，不再遍历所有角色广播。

每个 execution 必须且只能产生一个 Completed、Failed 或 Canceled 终止事件。Stop、删除和 Close 必须取消或明确拒绝尚未完成的审批，不能留下永久等待中的模型任务。

订阅 API 必须原子返回初始 snapshot 和之后的事件流位置，避免“先读 snapshot、再订阅”之间漏事件。工具、推理和审批详情默认是瞬态运行信息，不纳入房间恢复事实；若未来需要跨重启恢复，应另建存储计划。

AvaloniaShell 的 `ChatRoomProjection`：

1. 订阅核心事件；
2. 将事件调度到 Avalonia UI 线程；
3. 在 UI 线程维护 `ObservableCollection`；
4. 处理 Add、Remove、Replace、Reset 等完整投影语义；
5. 将公开消息与内部执行详情分成不同的 UI 模型。

设计时数据使用独立 `DesignChatViewModel` 或设计数据工厂，不再要求生产 ViewModel 提供依赖为 `null!` 的 public 无参构造器。

### 5. 应用服务拥有会话用例

`ChatRoomApplicationService` 负责：

- 创建会话；
- 打开会话；
- 原子切换当前会话；
- 关闭当前会话；
- 列表与删除；
- 保存策略；
- provider/config 快照注入；
- 将 UI 命令转换为 coordinator 命令。

ApplicationService 使用独立异步生命周期门串行化 Create/Open/Switch/Close/Delete。`CurrentRoomHandle` 携带 room epoch，所有外部命令必须经当前 handle 路由；UI、工具和服务不得长期保存并直接调用旧 coordinator。

加载成功后的 current-room 交换是独立线性化点。交换后新房间不会因旧房间关闭失败而回滚；切换 API 返回结构化结果，明确 `SwitchCommitted`、新 handle 和旧房间的关闭状态。旧 coordinator 进入 `RetiredRoomRegistry`，直到 Closed、CloseFaulted 或 Abandoned，避免成为无所有者后台任务。组合根只创建并共享一个 repository 实例。

`ChatRoomCoordinator` 不直接知道：

- 文件路径；
- JSON；
- 会话列表 UI；
- Avalonia dispatcher；
- Settings 页面。

### 6. 仓储拥有完整聚合提交

定义 `IChatRoomRepository`，至少包含：

- `ListAsync`
- `LoadAsync(SessionId)`
- `CommitAsync(ChatRoomSnapshot, expectedRevision)`
- `DeleteAsync(SessionId)`

Repository 只接收由 coordinator 产生的不可变 `ChatRoomSnapshot`，其中包含 `ChatRoomState` 和各角色最后一次 committed checkpoint。Repository 不得调用 runtime API 捕获状态。

#### 默认文件实现

继续使用文件系统，但采用 versioned generation：

```text
{baseFolder}/
├── .writer.lock
└── {sessionId}/
    ├── manifest.json
    ├── generations/
    │   ├── 00000001/
    │   │   ├── room.json
    │   │   └── role-states/
    │   └── 00000002/
    │       ├── room.json
    │       └── role-states/
    └── derived-logs/
```

`manifest.json` 至少包含：

- manifest schema version；
- canonical `SessionId`；
- current generation ID；
- current aggregate `StateRevision`；
- stable `CommitId`；
- `CreatedAt`、`LastActivityAt`、标题、角色数和消息数摘要；
- `room.json` 的文件长度和内容校验值；
- 每个 `RoleIncarnationId` 对应的 checkpoint 文件、长度、校验值和“无 checkpoint”标记；
- 可选 previous generation，仅供显式 repair 使用。

`StateRevision` 是领域命令 revision；generation ID 是仓储发布序号，两者不要求相等。debounce 可以从已持久化 state revision 10 直接发布 state revision 15。

提交过程：

1. 长生命周期 repository 在根目录取得独占 writer lease；本计划只支持单 writer，不承诺多进程并发写入。
2. 在每会话提交锁内重新读取 manifest，**先**比较 `CommitId`：如果当前 manifest 已指向相同 CommitId，直接返回原提交成功结果，即使调用方携带的是已经过期的 `expectedRevision`。
3. 如果 CommitId 尚未发布，再对 `expectedRevision` 执行 CAS 检查；revision 不一致时返回并发冲突。
4. 扫描正式但未被 manifest 引用的 generation。若其中存在相同 CommitId，且 SessionId、StateRevision、文件索引与本次 snapshot 校验值完全一致，则复用该 generation；不一致的同 CommitId 视为数据损坏，其他孤儿 generation 进入清理队列。
5. 没有可复用 generation 时，使用调用方提供的稳定 `CommitId`，在同一会话目录和同一文件系统写入 `.tmp-{guid}` generation。
6. 写入 `room.json` 和所有 committed role checkpoint，并在文件内重复记录 SessionId、StateRevision、CommitId 和 serializer version。
7. flush、关闭并校验全部文件。
8. 将临时 generation 重命名为不可变正式目录；正式目录名或其 metadata 必须能稳定关联 CommitId，以支持崩溃后的幂等识别。
9. 写入、flush 临时 manifest，再通过经过平台测试的原子发布原语替换正式 manifest。
10. manifest 替换成功是唯一持久化线性化点。线性化点之前可以响应取消；之后不得把取消、旧 generation 清理或派生日志失败报告为“提交失败”。
11. 提交成功响应丢失时，相同 CommitId 重试通过步骤 2 返回原成功结果；generation 重命名后、manifest 发布前崩溃时，通过步骤 4 复用孤儿 generation，不生成重复 generation。
12. 提交失败时旧 generation 保持可读；普通 Load 不静默拼接或回退损坏版本，修复必须通过显式 `RepairAsync`。

Load/List 忽略临时目录和没有有效 manifest 的会话。generation 清理由 repository 串行执行，并确保不会删除正在被读取的版本。Delete 与 Commit 使用同一会话生命周期锁：先写删除 tombstone 或把会话目录原子移出可见命名空间，再取消并等待派生任务，最后删除全部 generation 和派生日志，防止迟到任务复活会话。

派生日志按 `(SessionId, GenerationId)` 幂等生成到临时文件后替换，不使用追加模式。角色普通删除只保证从当前及后续 generation 消失；旧 generation 可在保留期内包含旧 incarnation。若要求物理安全擦除，应另行执行 hard-delete/compaction。

本计划只实现文件 generation + manifest。若目标平台无法提供所需原子发布和 writer lease 语义，应记录阻断证据并重新审批其他存储方案；实施过程中不得自行切换 SQLite。

### 7. 加载流程

新加载流程必须是全有或全无：

1. Repository 按强类型 `SessionId` 加载 snapshot。
2. 校验目录 ID、文档 ID、schema version、revision。
3. Assembler 创建具有正确 ID 和 CreatedAt 的不可变 `ChatRoomState`。
4. RuntimeFactory 创建全部角色运行时。
5. 注册模型 provider，按 checkpoint 索引恢复 AgentSession；WorkspacePath 属于瞬态授权，不从 snapshot 恢复。
6. 任一角色失败时释放全部候选运行时，当前房间保持不变。
7. 全部成功后创建新的 `ChatRoomCoordinator`。
8. ApplicationService 原子交换当前 room handle。
9. 发布一次会话切换事件。
10. 将旧 coordinator 送入 RetiredRoomRegistry 并启动真实关闭；切换结果明确返回旧房间是否 Closed、Stuck 或 CloseFaulted。

每个角色 checkpoint 索引必须声明状态是否预期存在。预期存在但文件缺失、校验失败、revision 不一致、role incarnation 不匹配或 serializer version 不兼容时，默认 Load 失败。需要重置角色私有会话时，必须通过显式 recovery policy，并把被重置角色返回给 UI。

ExecutionLease 和流式增量不持久化。进程崩溃后所有执行均视为中断，不恢复为 Running。

禁止：

- 对旧 `Session.Messages` 执行 Clear/Add；
- 对旧 `Roles` 执行 Clear/Add；
- 使用 `_persistenceSessionId` 补偿身份；
- 向 UI 暴露加载中的半成品。

### 8. 保存策略

业务命令提交后由应用层触发仓储提交，不再要求 UI 或调用者记得额外调用 `SaveAsync`。

采用 `PersistencePump` 串行保存：

- coordinator 分别维护 `StateRevision` 和 `PersistedRevision`；
- command 返回 `CommandReceipt`，明确表示状态已被内存接受，并包含可等待的 durability task；
- UI 可以立即显示 accepted state，并根据 `PersistenceStateChanged` 展示 Saving/Dirty/Faulted；
- 同一会话不允许并发调用 repository `CommitAsync`；
- pump 可以短时间 debounce，并只提交最新完整 snapshot；
- 新 execution 只能在其依赖的上一轮 role checkpoint 已 durable 后启动；
- repository 失败后进入 `PersistenceFaulted`，拒绝新的持久状态命令，只允许查询、重试、关闭或显式放弃未持久化状态；
- 重试复用原不可变 pending snapshot 与 CommitId；
- `CloseAsync` 只有在 `PersistedRevision == StateRevision` 时才能成功。

必须满足：

- 单次仓储提交是完整 snapshot；
- 提交 revision 可检测并发覆盖；
- `CloseAsync` 完成前保存队列已 flush；
- 持久化失败可观察，不静默写 Debug；
- 不存在“AgentSession 已保存，但公开消息未保存”的成功状态。

manifest 线性化点之后发生的取消、旧版本清理失败或派生日志失败作为独立维护告警报告，不能把已经 durable 的命令改报为失败。

### 9. AgentLib.Coding 边界

AgentLib.Coding 提供公开、稳定的 workspace transaction 契约。Prepare 创建候选资源但不改变可观察状态；Apply 切换 Coding 内部引用并保留旧资源和回滚能力；Rollback 恢复旧引用；Finalize 释放回滚能力，并让旧资源在旧 workspace lease 全部结束后退休。未 Finalize 的 transaction 在 Dispose 时必须回滚。

ChatRoom 负责跨角色的瞬态 workspace saga：

1. 协调器暂停启动新 execution；已有 execution 固定旧 `WorkspaceVersion` 并可继续，包括从工具调用触发本次切换。
2. 为全部相关 runtime Prepare。
3. 依次 Apply 全部候选。
4. 全部 Apply 成功后，协调器原子发布新的 `WorkspaceRuntimeState` 和 `WorkspaceVersion`；该内存发布是 workspace 切换的唯一线性化点。
5. 发布后调用每个 transaction 的 `CommitAfterPublish`。该操作必须先以不可失败、幂等的状态转换永久撤销回滚能力，再异步安排旧资源退休；transaction 在 committed 状态 Dispose 时不得回滚。
6. 旧资源退休或清理失败作为 `WorkspaceCleanupFaulted` 维护状态记录并可幂等重试，但新 WorkspaceVersion 保持有效，绝不回滚到旧版本。
7. 发布前失败或取消时逆序 Rollback。
8. 发布前回滚失败时房间进入 `WorkspaceTransitionFaulted`，在显式修复或重新加载前禁止新 execution。

如果某个 runtime 无法提供“发布后不可回滚、幂等提交、清理可重试”的契约，则不得进入第 4 步；必须在 Prepare/Apply 阶段失败。`Finalize` 不再表示一个可能把状态回滚的普通 Dispose，而是发布后的资源退休过程。

WorkspacePath 不进入 repository snapshot，因此该 saga 不与 manifest commit 组成伪造的跨文件事务。公开 API 不接受在 Coding 内部锁中执行的任意 ChatRoom callback，ChatRoom 不引用 `CodingWorkspaceToolCandidate`。

### 10. 停止、关闭与不可取消执行

关闭状态机定义为：

```text
Active -> Closing -> Closed
                  -> CloseFaulted
                  -> Stuck -> Abandoned
```

- `CloseAsync` 只有在命令入口关闭、全部 execution 终止、所有回投处理完成、PersistencePump flush 成功、全部 runtime 实际释放后才成功完成。
- 调用方取消等待不能把房间从 Closing 恢复为 Active。
- `TryCloseAsync(timeout)` 期限到达时返回包含 ExecutionId、RoleId 和 runtime 状态的 `Stuck` 结果，不得返回成功。
- `ForceAbandonAsync` 只能逻辑放弃：失效 room instance 与全部 lease，拒绝迟到事件，并把未退出资源登记为 `AbandonedResource`。它不能宣称托管任务或外部工具已经物理终止。
- 如果产品要求真正强杀模型、语言服务器或工具副作用，相关执行必须迁移到可终止的独立进程边界；本计划不伪造进程内强杀能力。
- 派生日志任务由 repository 队列拥有，不阻塞 coordinator 关闭，但会话删除和应用退出必须能够取消或排空它们。

## 破坏性 API 调整清单

以下 API 计划删除、收紧或替换，不提供兼容层。

### `ChatRoomSession`

删除：

- `ObservableCollection<ChatRoomMessage> Messages`
- `IMainThreadDispatcher MainThreadDispatcher`
- `AddMessageAsync`
- `RemoveMessageAsync`
- 同步 `AddMessage`
- `FromPersistence`
- `ToPersistence`
- 仅测试使用的 `HasRoleSpoken`
- UI 显示用途的 `DisplayText`

替换为：

- 不可变 `ChatRoomState.Messages`
- coordinator 查询和命令
- 独立 UI projection

### `ChatRoomManager`

删除或替换：

- public `ObservableCollection<ChatRoomRole> Roles`
- public `Persistence` setter
- `LoadAsync`
- `SaveAsync`
- `StepAsync(ChatRoomRole)`
- `AddRoleAsync(ChatRoomRole)`
- 同步 `Stop()`
- 可在资源未释放时完成的关闭语义
- public `Session`/`Roles` 对象图访问
- `RegisterRoleModelProviders` 和 `RegisterModelProvidersForRole` 一类运行时装配入口
- 工具直接持有并调用 `ChatRoomManager`

替换为：

- `ChatRoomCoordinator`
- RoleId/command-based API
- `StopAsync`
- 完整等待的 `CloseAsync`、有期限但不伪装成功的 `TryCloseAsync`、仅逻辑放弃的 `ForceAbandonAsync`
- ApplicationService 的 load/save/list/delete 用例

### `ChatRoomRole`

删除或收紧：

- public 运行时构造器
- public 可变 `EndpointManager`
- public 可变 Definition 引用
- public `ChatRoomContext` setter
- UI 显示属性和 `NotifyBase`
- internal `Executor` 测试观察孔
- public `SpeakAsync`/`SpeakFirstAsync`
- public `ReduceSessionAsync`/`ClearSessionMemory`
- public `ApproveToolExecution`/`RejectToolExecution`
- public `MainThreadDispatcher`/`WorkspacePath`

替换为：

- internal runtime implementation
- 不可变 Definition
- `RoleRuntimeStatus` 快照
- factory 注入测试替身

### `ChatRoomService`、工具与消息模型

删除或替换：

- `ChatRoomService.CurrentManager`
- 旧 `SessionService` 与新 ApplicationService 并存的服务定位模式
- `ChatRoomRoleManagementTools`、`WorkspacePathTools` 对 Manager 的直接引用
- public 可变 `ChatRoomMessage` 及其 `CopilotChatMessage` 桥接
- 核心层 public `NotifyBase` 消息/会话/角色对象图

替换为：

- `CurrentRoomHandle` 和不可变查询 snapshot
- ApplicationService command API
- 带 `ExecutionId`/`ApprovalId` 的工具与审批命令
- UI 中立 execution event DTO 与 Avalonia projection model

### 持久化

删除：

- `ChatRoomPersistence` 的低层 public 文件写入 API
- `SavePublicMessageAsync`
- 生产未使用的 `SaveRoleMessageAsync`
- `SaveRoleAgentSessionStateAsync` 等由 Manager 逐个调用的 API
- 同步 `ListSessionIds`、`Delete`
- `SessionService.LoadSessionAsync` 返回可变 DTO 的入口

替换为：

- `IChatRoomRepository`
- 不可变、带版本的 stored DTO
- 聚合级 `CommitAsync`
- 异步 list/load/delete

### friend assembly

清理：

- 删除 `AgentLib.Coding` 对 `AgentLib.ChatRoom` 的 `InternalsVisibleTo`。
- 删除 `AgentLib.ChatRoom` 中 csproj 与 `AssemblyInfo.cs` 的重复 friend 声明。
- 删除 AvaloniaShell csproj 中重复且当前无实际用途的 friend 声明。
- 是否完全取消测试 friend access 由重构后的测试接缝决定；不得为了取消 friend access 把 internal 成员改成 public。

## 预计影响文件与模块

以下为实施时必须纳入的主要文件区域，避免只重写 `ChatRoomSession`/`ChatRoomManager` 而遗漏调用方：

### AgentLib.ChatRoom 核心

- `ChatRoomSession.cs`
- `ChatRoomManager.cs`
- `ChatRoomManager.ChatRoomAutoLoopRunner.cs`
- `ChatRoomRole.cs`
- `ChatRoomRoleFactory.cs`
- `IChatRoomRoleFactory.cs`
- `IChatRoomRoleExecutor.cs`
- `IChatRoomRoleExecutorFactory.cs`
- `StandardChatRoomRoleExecutor.cs`
- `CodingChatRoomRoleExecutor.cs`
- `ChatRoomRoleExecutionContext.cs`
- `ChatRoomRoleExecutionResult.cs`
- `ChatRoomSpeakResult.cs`
- `Model/ChatRoomMessage.cs`
- `Model/ChatRoomRoleDefinition.cs`
- `Model/ChatRoomSessionData.cs`
- `Model/ChatRoomJsonSerializerContext.cs`
- 新增正式 state、checkpoint、snapshot、command、event、repository 和 coordinator 文件

### 服务、模板和工具

- `ChatRoomPersistence.cs`
- `Services/ChatRoomService.cs`
- `Services/SessionService.cs`
- `Services/RoleTemplateService.cs`
- `Services/CodingAssistantRoleFactory.cs`
- `Services/PresetTemplates.cs`
- `Model/RoleTemplate.cs`
- `Tools/ChatRoomRoleManagementTools.cs`
- `Tools/WorkspacePathTools.cs`

不可变角色定义会影响模板复制、预设角色创建和工具编辑链，不能只修改运行时类。

### AgentLib.Coding 边界

- `AgentLib.Coding/CodingAgent.cs`
- `AgentLib.Coding/CodingWorkspaceToolProvider.cs`
- `AgentLib.Coding/CodingWorkspaceToolCandidate.cs`
- `AgentLib.Coding/AgentLib.Coding.csproj`
- 新增公开 workspace transaction 契约及测试

### AvaloniaShell

- `App.axaml.cs`：组合根和单一 repository 生命周期
- `Infrastructure/AvaloniaMainThreadDispatcher.cs` 或新的 Shell 自有 UI dispatcher 抽象
- `ViewModels/ChatViewModel.cs`
- `ViewModels/SessionListViewModel.cs`
- `ViewModels/RoleListViewModel.cs`
- `ViewModels/RoleEditViewModel.cs`
- `ViewModels/RoleLobbyViewModel.cs`
- `ViewModels/SettingsViewModel.cs`
- `ViewModels/MainViewModel.cs`
- `ViewModels/ViewModelBase.cs`
- 相关 XAML 设计数据与绑定

### 项目和测试配置

- `AgentLib.ChatRoom.csproj`
- `AgentLib.ChatRoom/AssemblyInfo.cs`
- `ChatRoom.AvaloniaShell.csproj`
- `AgentLib.ChatRoom.Tests`
- `ChatRoom.Shell.Tests`
- `ChatRoom.Shell.Tests/TestMainThreadDispatcher.cs`

新持久化 DTO、manifest 和 checkpoint 必须加入 source-generated JSON context；friend 声明和测试 project 引用必须随新接缝同步清理。

## 测试架构重写

### 测试迁移门禁

不得按测试方法或测试类批量删除。很多现有测试同时包含实现细节断言和重要业务断言；重构时只删除错误的构造方式或断言，并先把其保护的业务不变量迁移到新测试。

必须维护测试迁移台账：

| 旧测试或断言 | 当前保护的不变量 | 新测试名称/层级 | 最早完成阶段 | 删除旧断言的条件 |
|---|---|---|---|---|
| `FromPersistence_*` | 身份、消息顺序、消费水位恢复 | assembler/repository contract | 阶段 1/5 | 新往返测试通过 |
| `ToPersistence_*` | LastActivityAt、深快照、角色/消息完整性 | snapshot/serializer contract | 阶段 1 | 无共享可变引用测试通过 |
| dispatcher 属性和 `AddMessageAsync_*` | UI 集合线程亲和性 | projection + UiThreadHarness | 阶段 7 | 新线程测试通过 |
| executor 类型/对象同一性 | execution kind、AgentSession 连续性、失败释放 | runtime factory/behavior | 阶段 3 | 替代行为测试通过 |
| 直接 `Messages`/`Roles` 构造 | mention、manager 仲裁、持久化、加载等业务场景 | coordinator/application scenario | 阶段 4/6 | 每个业务场景已映射 |
| provider 精确调用次数 | 模型可用和选型结果 | runtime behavior | 阶段 3 | 业务结果测试通过 |

硬性规则：

1. 删除或收紧生产观察孔之前，替代行为测试必须已经通过。
2. 同一改动中先加入替代测试，再删除旧断言。
3. 阶段 8 不允许删除迁移台账中仍为空的测试项。
4. 不允许以“新 API 尚未完成”为理由临时删除失败测试。
5. 不保留生产兼容层，但旧路径的 characterization tests 必须运行到对应切换点。

可以删除的内容是：同步 `AddMessage` 方法级测试、`FromPersistence`/`ToPersistence` 方法存在性测试、`AddMessageAsync_WithDispatcher_InvokesOnMainThread` 与 `...DoesNotAddDirectly` 两个错误 dispatcher 测试、具体 executor 类型/对象同一性、DTO `AreSame`、provider 精确调用次数。其身份、深复制、调度水位、AgentSession 连续性、资源释放和业务调度语义必须先迁移。

### 新增测试设施

#### `ChatRoomScenarioBuilder`

- 只通过正式 command/application API 构造房间状态；
- 可注入 fake role runtime factory；
- 可控制角色执行完成、失败、取消和流式增量；
- 不需要访问生产对象内部字段。
- 不暴露 command queue、lease registry、内部 revision 字段或 coordinator 私有状态；
- fake runtime 只提供 Started/AllowDelta/AllowApproval/AllowComplete/Fail/Cancel 等显式 gate；
- 时间、ID、CommitId 和取消源均可注入，不依赖当前时间或调度概率。

#### Repository contract fixture

- 通过真实 repository commit/load 构造持久化场景；
- 只验证 commit/load/list/delete、expected revision、幂等 CommitId、深隔离和失败后的最后成功 revision；
- 不暴露 generation、manifest 或文件名。

#### File repository durability fixture

- 只用于文件实现测试；
- 可检查 generation、manifest、writer lease、tombstone 和派生日志；
- 通过窄的 `IAtomicFilePublisher`/文件提交原语注入确定性故障；
- 不直接调用 serializer 私有辅助方法。

#### `UiThreadHarness`

- 拥有真实专用 UI 测试线程或 Avalonia 测试消息泵；
- `CheckAccess` 基于真实线程 ID；
- dispatcher 回调排队而非内联伪装；
- 可确认已入队、单步执行、暂停、恢复和用队列哨兵 drain；
- 传播 UI 回调异常；
- 对事件发布线程、处理线程和单调序号，以及 `PropertyChanged`、`CollectionChanged` 和导航事件记录线程；
- 每个测试结束验证队列为空且消息泵没有未观察异常。

### 必须新增的回归测试

#### 恢复与身份

1. 保存再加载后，`SessionId` 和 `CreatedAt` 与原会话一致。
2. 加载后会话列表只有一个对应项并正确选中。
3. 当前加载会话不可删除。
4. 目录 ID 与文档 ID 不一致时加载失败，不静默分叉。
5. 任一角色恢复失败时，当前会话保持不变，全部候选运行时已释放。
6. 历史 AI 消息恢复后，公开内容和 UI 投影均正确。
7. provider 注册、工作区授权和 AgentSession 恢复在任一候选阶段失败或取消时不泄漏 runtime。
8. snapshot 损坏或 repository 校验失败时不关闭旧房间、不交换 handle、不发布切换事件。
9. 成功交换只发布一次完整事件；旧房间关闭失败以结构化切换结果报告，不回滚新房间。
10. Standard 与 Coding runtime 均可从 checkpoint 恢复并继续增量对话。

#### 聚合不变量

1. 外部无法取得可变消息和角色集合。
2. 未知 RoleId 不能发言。
3. 未加入当前房间的运行时不能写入消息。
4. 删除角色后运行时释放，当前及后续 generation 不再包含该 `RoleIncarnationId`；RoleId 重用不会恢复旧 checkpoint。
5. 角色定义更新经过验证并生成新配置版本。
6. 影响 SystemPrompt/Memory 的更新遵循明确的 AgentSession 迁移策略。

#### 并发与执行

1. 房间级同时最多一个 execution；并发请求恰好一个获得 lease，其余得到明确拒绝或排队结果。
2. 人类在流式执行期间多次插话，当前 execution 的 `InputThroughSequence` 固定，下一次执行恰好看到新增消息，不重复、不跳过。
3. lease 1 取消后 lease 2 已启动，lease 1 的迟到 delta/complete/fail 不得影响 lease 2。
4. 同一 execution 的 Complete/Complete、Complete/Fail、Fail/Complete 回投幂等。
5. 角色删除后以相同 RoleId 重新加入，旧 role incarnation 的事件不得影响新角色。
6. 旧房间切换后，旧 `RoomInstanceId` 的事件不得污染新 projection 或新状态。
7. Stop、Close、RemoveRole、UpdateRole、WorkspaceTransition 与 completion 同时发生时遵循文档中的竞争规则。
8. runtime 已完成但完成命令尚未处理时发生 Close/Remove，结果按 lease 终态规则处理。
9. 队列中尚未开始的发言请求在关闭时全部完成为取消或拒绝，不永久悬挂。
10. execution 终止后到达的 delta 被丢弃，不产生 UI 幽灵消息。
11. `StopAutoLoop` 完成时当前 execution 已取消并进入终态，run state 为 Idle。
12. `TryCloseAsync` 对忽略取消的模型返回 Stuck；`CloseAsync` 不提前成功；ForceAbandon 后迟到结果不能提交。
13. Close 完成时 execution、PersistencePump 和 runtime 均实际结束；多资源释放失败按规则聚合并可观察。

#### 持久化原子性

1. 相同 expected revision 的并发提交恰好一个发布，另一个得到冲突。
2. generation 未完整写入或任一 checkpoint/flush/close/manifest 写入在发布前失败时，manifest 不切换，旧版本可由新 repository 实例完整加载。
3. manifest 替换成功后，取消、清理失败或派生日志失败不把 durable commit 报为失败。
4. 相同 CommitId 重试不生成重复 generation。
5. commit 成功后公开消息和 committed checkpoint 属于同一 StateRevision。
6. 人类在角色执行期间触发保存时，只能保存该角色最后 committed checkpoint，不能捕获 candidate 状态。
7. 删除与 commit 并发时不会删除后复活；删除会取消派生任务并删除全部可见 generation 和日志。
8. 删除角色后重新使用同一 RoleId 不会恢复旧 `RoleIncarnationId` 的 checkpoint。
9. 保存 snapshot 与后续运行时修改相互隔离。
10. 损坏 manifest、缺失当前 generation、checkpoint 校验失败时普通 Load 明确失败，不拼接版本；Repair 单独测试。
11. 路径穿越、绝对路径、Windows 目录别名、大小写冲突和链接越界防护从旧 Persistence 测试迁移。

#### UI 线程

1. 核心事件从工作线程发布时，UI 集合只在 UI 线程变化。
2. 创建、加载、设置保存完成于工作线程时，ViewModel 状态仍在 UI 线程提交。
3. 消息投影完整处理 Add、Remove、Replace 和 Reset。
4. 空回复、取消和异常不会留下幽灵流式消息。
5. AI 工具创建/删除角色不会从工作线程直接修改 UI 集合。
6. 生产 ViewModel 不存在依赖为 `null!` 的 public 设计器构造器。
7. 旧 room 事件在切换后迟到，不污染新 projection。
8. UI 线程发起命令并等待核心完成时不死锁。
9. 异步命令失败时 `IsBusy` 和错误状态在 UI 线程恢复，导航事件符合契约。

#### 自动循环与聊天室业务

现有集成测试中的以下业务必须迁移，不得因内部队列和 Manager 被删除而消失：

- 多 mention 按文本顺序调度；
- AlwaysParticipate、MentionOnly 和 manager 的参与规则；
- 新 mention 对默认队列的优先级；
- 自我 mention、mention 环和最大尝试上限；
- manager 仲裁、重新指派和链式继续；
- 空回复终止而非死循环；
- 人类触发后默认角色各尝试一次并自然停止；
- AI 工具创建角色后被 mention 可正常发言；
- 只有人类角色可选时 manager 兜底；
- 工作区审批、拒绝、取消、失败回滚和旧 lease 退休；
- 角色大厅连续添加、重复角色错误和会话列表空会话策略。

这些测试通过正式 command/application API 和 deterministic runtime gate 构造，不再直接修改集合或依赖固定 `Task.Delay`。

## 实施阶段

### 阶段 1：并行建立正式契约、测试台账和内存接缝

1. 以新类型名新增不可变 state/message/role definition、stored DTO、checkpoint、snapshot 和 serializer context 注册，不原位修改旧可变类型。
2. 定义 `IChatRoomRepository`、内存 repository fake、repository contract harness、CommitId 和 revision 结果。
3. 定义 runtime、candidate checkpoint、execution event、approval 和 factory 接口。
4. 新增 `ChatRoomScenarioBuilder`、严格 `UiThreadHarness` 和测试迁移台账。
5. 保持旧主流程和旧 characterization tests 可编译。

#### 验收

- 新模型不引用 `NotifyBase`、dispatcher、Avalonia 或 `ObservableCollection`。
- stored DTO 与运行时对象不存在共享可变引用。
- repository contract tests 可在内存 fake 上运行。
- 台账中的每个待删除测试都标明替代测试和最早阶段。
- 整个 `ChatRoom.slnx` 编译通过。

### 阶段 2：先修正 AgentLib.Coding 公开工作区边界

1. 设计并实现公开 workspace transaction 接口。
2. 将 candidate/session、回滚和旧 lease 退休封装在 Coding 项目中。
3. 让现有 `CodingChatRoomRoleExecutor` 先改用公开接口。
4. 迁移工作区审批、拒绝、取消、回滚和释放测试。
5. 删除 `AgentLib.Coding` 对 `AgentLib.ChatRoom` 的 friend 声明。

#### 验收

- ChatRoom 不引用 AgentLib.Coding internal 类型或方法。
- 公开契约定义 Prepare/Apply/Rollback/Finalize 的所有权和失败语义。
- 整个解决方案编译，现有 ChatRoom 行为仍通过。

### 阶段 3：实现正式角色运行时和 checkpoint 适配器

1. 将 Standard/Coding 执行适配到统一 runtime 接口。
2. 实现从 committed checkpoint 创建 candidate execution、完成时返回 candidate checkpoint、失败时丢弃。
3. 将角色构造和释放收归正式 runtime factory/registry。
4. 迁移 execution kind、AgentSession 连续性、模型可用、创建失败释放等行为测试。
5. 替代测试通过后删除 `Executor` 观察孔和具体实现类型/对象同一性断言。

#### 验收

- 外部无法构造未注册 runtime 并写入房间。
- runtime 生命周期有唯一所有者。
- 仓储测试不需要读取活 runtime。
- 整个解决方案编译通过。

### 阶段 4：实现单写者协调器和业务调度

1. 实现 command queue、不可变 state revision、MessageSequence 和 execution lease。
2. 实现消息、角色、自动循环、审批、记忆、工作区、Stop 和 Close 命令。
3. 将现有 mention、manager 仲裁、空回复和工具创建角色等业务测试迁移到 coordinator/application scenario。
4. 实现 RoomInstanceId、RoleIncarnationId、runtime version 和迟到事件拒绝。
5. 实现 Stuck/CloseFaulted/Abandoned 状态机和 deterministic 并发测试。

#### 验收

- 所有持久状态写入只发生在协调器内部。
- 房间级最多一个 execution，且人类插话不被长时调用阻塞。
- 消费水位不因执行期间插话而跳过消息。
- `CloseAsync` 不提前成功；Stuck 和逻辑放弃语义可观察。
- 整个解决方案编译通过。

### 阶段 5：实现 PersistencePump 和文件仓储

1. 实现有序 PersistencePump、accepted/durable receipt、Dirty/Faulted 和幂等重试。
2. 实现 generation、manifest、writer lease、per-session CAS 和 CommitId。
3. 实现 checkpoint 索引、文件校验、Repair、tombstone、generation GC 和派生日志。
4. 通过 `IAtomicFilePublisher` 注入完整故障矩阵。
5. 增加旧存储格式处理策略；由于无需兼容，可直接拒绝旧格式或提供开发期清理命令。

#### 验收

- manifest 发布是唯一线性化点，单次 commit 全有或全无。
- Load 只读取 manifest 指向的完整 generation。
- 删除、commit 和派生任务不会复活会话。
- domain/coordinator 不引用文件系统类型。
- 整个解决方案编译通过。

### 阶段 6：新增正式 ApplicationService 与完整加载切换

1. 实现 application lifecycle gate、CurrentRoomHandle/epoch、RetiredRoomRegistry 和结构化 switch result。
2. 创建新会话时构造完整 coordinator。
3. 打开会话时完整加载 snapshot、runtime 和 checkpoint，再原子交换 handle。
4. 将会话列表、删除、空会话策略和当前会话保护迁移到新服务。
5. 保留旧 UI 调用栈，先通过适配层并行验证正式实现；此适配层只用于迁移并在阶段 8 删除。

#### 验收

- 加载后的公开身份与持久化身份完全一致。
- 失败加载不污染当前房间。
- 成功交换只发布一次事件；旧房间关闭失败不回滚新房间。
- 整个解决方案编译通过。

### 阶段 7：切换 Avalonia 投影、工具与组合根

1. 新增 `ChatRoomProjection`，原子取得初始 snapshot 和事件流位置。
2. 让消息、角色、审批和运行状态集合完全由 UI 层拥有。
3. 重写 SessionList、RoleList、RoleEdit、RoleLobby、Chat、Settings 和 Main ViewModel 的状态来源与命令调用。
4. 让角色管理和工作区工具调用 ApplicationService command，不直接持有 Manager。
5. 删除生产 ViewModel 的无效设计器构造器，改用独立设计数据。
6. 删除 ViewModel 中 `_isRunning`、`_autoLoopCts` 等影子状态，并让组合根共享单一 repository 实例。

#### 验收

- UI 不读取旧 `CurrentManager.Session/Roles` 对象图。
- 所有 UI 集合和导航状态通过确定性线程测试。
- 旧房间迟到事件不污染新 projection。
- 整个解决方案编译并通过 Shell 业务测试。

### 阶段 8：删除旧架构、迁移桥和已替代测试

1. 仅在测试迁移台账对应替代测试已通过后，删除旧 `ChatRoomSession`、`ChatRoomManager`、`ChatRoomRole` 和 `ChatRoomPersistence` API。
2. 删除旧 `ChatRoomService`/`SessionService` 定位器、迁移适配层、Manager 直连工具和 UI 对象图。
3. 删除已替代的实现细节断言与重复 friend 声明；不得删除未映射业务测试。
4. 更新 serializer context、README、需求实现说明和相关历史计划状态。
5. 全量搜索旧符号，确保没有兼容壳或旁路入口残留。
6. 运行整个解决方案构建、全部 ChatRoom 测试和迁移台账检查。

#### 验收

以下符号和模式不应继续存在于生产代码：

- `ChatRoomSession.FromPersistence`
- `ChatRoomSession.ToPersistence`
- `ChatRoomSession.AddMessage`
- public `ObservableCollection` 类型的 `Messages`/`Roles`
- `ChatRoomManager.LoadAsync`
- `_persistenceSessionId`
- `_persistenceCreatedAt`
- public `ChatRoomManager.Persistence`
- `ChatRoomService.CurrentManager`
- public `ChatRoomManager.Session`
- public `ChatRoomRole.EndpointManager`
- public `ChatRoomRole.SpeakAsync`/`SpeakFirstAsync`
- public `ChatRoomRole.ReduceSessionAsync`/`ClearSessionMemory`
- public `ChatRoomRole.ApproveToolExecution`/`RejectToolExecution`
- public `ChatRoomRole.MainThreadDispatcher`
- `ChatRoomRole.Executor`
- public 可变 `ChatRoomMessage.CopilotChatMessage`
- 工具对 `ChatRoomManager` 的直接引用
- 旧 `SessionService` 与新 ApplicationService 并存
- ChatRoom 核心中的 `IMainThreadDispatcher`
- ChatRoom 对 `CodingWorkspaceToolCandidate` 的引用

## 实施顺序原则

- 先建立新模型和测试，再切换生产流程，避免在旧类型上继续加补丁。
- 每个阶段结束时必须编译整个 `ChatRoom.slnx`，并运行阶段相关测试与仍适用的旧 characterization tests。
- 不创建旧 API 到新 API 的长期兼容适配层。
- 临时桥接代码必须在计划中标明删除阶段，并在最终全量搜索中清除。
- 线程问题使用确定性调度测试，不使用固定短 `Task.Delay` 证明行为。
- 并发测试必须用显式 gate 和队列 drain 建立 happens-before；timeout 只作为死锁 watchdog。
- 持久化故障使用可注入故障点，不依赖真实磁盘偶发失败。
- 关闭、取消和资源释放测试必须设置有限测试超时，但业务实现不得用静默超时伪装释放完成。
- 每个旧行为测试必须先迁移、后移除旧入口；阶段 8 必须确认测试台账无空项。
- 新旧栈并行期保留一组公共业务场景等价测试，直到 UI 和工具全部切换。

## 明确不纳入本次主重构的事项

以下问题可能真实存在，但不应让本计划无限扩张：

- 全面移除 AgentLib 所有类型的 `NotifyBase`；
- 重写 AgentLib 全部流式消息和子代理 UI 更新路径；
- .NET TFM 或包版本现代化；
- 非 ChatRoom 项目的所有 friend assembly；
- Provider 设置同步和模型 registry 的全局重写，除非它阻塞新的 runtime factory；
- 更换 Avalonia 或测试框架；
- 旧持久化格式的正式兼容迁移。

如果实施过程中发现这些事项阻塞目标架构，应另建计划，而不是在本计划中临时扩大范围。

## 风险与处理

### 风险 1：重写范围较大

处理：按新模型、运行时、协调器、仓储、应用服务、UI 投影分阶段切换，每一阶段都有独立验收标准。

### 风险 2：长时模型调用与单写者模型冲突

处理：命令循环只提交执行 lease，不等待模型完成；流式增量为瞬态事件，最终结果通过命令回投。

### 风险 3：文件 generation 实现复杂

处理：实现并验证 manifest 原子发布、独占 writer lease 和 CommitId 幂等协议。如果目标平台无法满足这些语义，记录阻断证据并重新提交存储方案审批；本计划实施过程中不得自行切换 SQLite。

### 风险 4：角色配置更新与 AgentSession 语义

处理：把迁移策略变成显式参数或命令规则。至少区分：

- 保留现有 AgentSession；
- 清空 AgentSession；
- 尝试保留历史但重建系统上下文。

不得继续以“Definition 已改”暗示运行态已完全更新。

### 风险 5：测试语义覆盖在迁移中不可见地下降

处理：以测试迁移台账作为阶段门禁。测试方法数量下降不是问题，但任何旧业务不变量在替代测试仍为 pending 时都不得删除；阶段 8 必须确认台账无空项。

## 最终验收标准

重构完成后必须满足：

1. 恢复期间一次性构造完整会话，运行期间不存在恢复专用同步旁路。
2. 生产 API 不再因为单元测试方便而开放状态变更入口。
3. 消息、角色和会话身份只能通过唯一协调边界修改。
4. 核心层没有 UI dispatcher、Avalonia 类型或公开可变 UI 集合。
5. UI 线程正确性由 projection 层和确定性线程测试保证。
6. 加载后的 SessionId、CreatedAt 和持久化目标完全一致。
7. 不存在 `_persistenceSessionId` 一类影子身份。
8. 保存、加载和删除由仓储拥有完整聚合语义。
9. 仓储只接收不可变 committed checkpoint，不在提交时读取活 runtime。
10. 一次持久化提交中的公开消息和 AgentSession checkpoint 属于同一 StateRevision 和 CommitId。
11. 消息增量使用单调 MessageSequence；执行期间插话不会被完成时水位跳过。
12. 房间级最多一个 execution，迟到、重复和旧 incarnation 回投均被拒绝或幂等处理。
13. `StopAsync` 取消并等待当前 execution；`CloseAsync` 只在真实完成时成功，Stuck/CloseFaulted/Abandoned 不伪装成 Closed。
14. manifest 替换是唯一持久化线性化点，发布前后取消和错误语义明确。
15. ChatRoom 不再通过 friend assembly 使用 AgentLib.Coding 内部工作区协议。
16. 测试主要验证公开行为和架构不变量，不断言具体执行器类型、内部对象同一性或死代码映射方法。
17. 测试迁移台账无空项，原有 mention、manager 仲裁、持久化安全、工作区和会话列表业务均有替代覆盖。
18. 最终搜索确认旧 API、兼容壳和旁路集合修改全部移除。

## 审批点

审批本计划时重点确认以下决策：

1. 是否同意彻底删除 `ChatRoomSession.FromPersistence`、`ToPersistence`、同步 `AddMessage` 及对应方法级测试/实现断言，而不是调整其可见性；其中保护的身份、消息顺序、水位和深快照行为必须迁移。
2. 是否同意删除 `ChatRoomManager.LoadAsync`，由应用服务加载完整新聚合后替换当前会话。
3. 是否同意核心层完全移除 `ObservableCollection`、`NotifyBase` 和 `IMainThreadDispatcher`。
4. 是否同意采用单写者命令协调器，并明确房间级同时最多一个 execution。
5. 是否同意人类插话不取消当前 execution，而 `StopAutoLoop` 取消并等待当前 execution。
6. 是否同意活跃角色更新等待当前 execution 结束后生效，删除则取消并等待后再提交删除。
7. 是否同意采用 committed/candidate AgentSession checkpoint，仓储禁止在提交时序列化活 runtime。
8. 是否同意 command 先返回 accepted receipt、UI 可显示 Dirty/Saving/Faulted，而 Close 必须等待 durable；持久化故障期间拒绝新的持久状态命令。
9. 是否同意默认采用仅支持单 writer 的文件 generation + manifest + CommitId 原子提交；SQLite 或其他存储必须重新审批。
10. 是否同意 manifest/current generation 损坏时普通 Load 明确失败，不静默回退；回退只能通过显式 Repair。
11. 是否同意 WorkspacePath 为本机瞬态授权，不随会话持久化；工作区切换使用公开 transaction 和运行时 WorkspaceVersion。
12. 是否同意工具、推理、子代理和审批详情默认仅作为瞬态 execution 事件，不跨重启恢复。
13. 是否同意 `CloseAsync` 可能因不可取消模型长期等待；`TryCloseAsync` 返回 Stuck，`ForceAbandonAsync` 只逻辑放弃而不宣称物理释放。
14. 是否同意角色定义不可变，并为 SystemPrompt/Memory 更新建立显式 AgentSession 迁移策略。
15. 是否同意破坏性删除公开运行时对象、低层持久化 API 和 Manager 直连工具，不提供兼容层。
16. 是否同意同步调整 AgentLib.Coding 的公开工作区边界并删除跨程序集 friend access。
