# ChatRoom 工作区与角色运行时重构计划

## 背景

当前 `CodingAssistantRoleFactory` 实施引入了 `ChatRoomRoleKind`、`RoleTemplate.IsRuntimeBuiltIn`、角色绑定集合，以及由 `ChatRoomService` 协调编程工具工作区和释放流程的设计。代码审查确认，这些设计把“角色配置”“角色运行时能力”“聊天室会话生命周期”和“宿主模板覆盖”混合到了一起。

主要问题如下：

- `ChatRoomRoleKind` 使通用聊天室模型开始识别具体角色种类，后续每增加一种特殊角色都可能继续扩展枚举和分支。
- `RoleTemplate.IsRuntimeBuiltIn` 把模板来源和存储策略写入数据模型，而模板是否来自当前进程本应由模板服务自身掌握。
- `ChatRoomService` 直接持有 `CodingAssistantRoleFactory`，并负责绑定角色、响应工作区变化和释放编程工具，破坏了应用服务的通用性。
- `CodingAssistantRoleFactory.BindRole` 要求角色创建后再次补充运行时行为，容易遗漏加载、添加、移除和异常回滚等路径。
- 工作区路径同时保存在 `ChatRoomManager`、`ChatRoomRole` 内部聊天管理器、`CodingAssistantRoleFactory` 和 `CodingWorkspaceToolSession`，缺少单一状态来源。
- `CloseCurrentSession` 和 `ChatRoomManager.SetWorkspacePath` 使用同步阻塞等待异步任务，可能造成 UI 线程死锁，并掩盖异步资源释放边界。
- 当前模板创建链只传递 `ChatRoomRoleDefinition`，无法携带非序列化的角色运行时能力；历史会话恢复也依赖角色种类枚举重新装配工具。

本次重构目标不是继续修补编程助手特例，而是为 `AgentLib.ChatRoom` 建立通用的角色运行时扩展和工作区生命周期机制，使编程助手只是该机制的一个使用者。

## 已确认的设计约束

1. 不引入或保留 `ChatRoomRoleKind`，聊天室模型不区分“编程助手”和“普通角色”。
2. 不在 `RoleTemplate` 增加 `IsRuntimeBuiltIn` 或其他模板来源标记。
3. `ChatRoomService` 不得知道 Roslyn、.NET CLI、编程助手工厂或工作区编程工具的存在。
4. `CodingAssistantRoleFactory` 不提供 `BindRole`/`UnbindRole` 这种创建后补装配 API。
5. 编程助手仍使用普通 `ChatRoomRole`，不新增角色继承体系。
6. 工作区路径只能有一个权威状态来源；其他对象只保存按该路径创建的资源，不维护可独立修改的路径状态。
7. 所有涉及启动、切换和释放外部资源的 API 必须保持异步，不允许使用 `GetAwaiter().GetResult()`、`.Result` 或 `.Wait()` 转为同步。
8. 角色模板和会话 JSON 只保存可序列化配置，不保存 `AITool`、Language Server 进程或其他运行时对象。
9. 运行时模板允许正常序列化；同 ID 内存模板覆盖磁盘模板的行为由 `RoleTemplateService` 管理。
10. 默认文件工具、Roslyn 工具和 .NET CLI 工具应使用同一个规范化工作区路径，并在切换失败时保持旧工作区可用。
11. 公共 API 保持可空安全并提供 XML 文档注释；路径拼接使用 `Path.Join`。
12. 修改保持最小职责边界，不为未来未知角色引入复杂继承体系。

## 当前调用链分析

### 普通角色创建

当前普通角色创建链为：

```text
ViewModel / ChatRoomService 调用方
  → ChatRoomService.AddRoleAsync(ChatRoomRoleDefinition)
  → new ChatRoomRole(definition)
  → ChatRoomManager.AddRoleAsync(role)
  → role.InitializeAsync()
```

该链路假设 `ChatRoomRoleDefinition` 足以描述角色的全部行为。对只包含提示词、模型和技能目录的普通角色成立，但无法承载需要异步创建和释放的运行时工具。

### 模板创建角色

当前角色大厅创建链为：

```text
RoleTemplate
  → RoleTemplateService.ToDefinition
  → ChatRoomService.AddRoleAsync(definition)
  → new ChatRoomRole(definition)
```

`ToDefinition` 会丢失所有非序列化运行时信息。因此，运行时模板即使在内存中由工厂创建，加入会话时仍会退化为普通定义。

### 历史会话恢复

当前恢复链为：

```text
ChatRoomPersistence 加载角色定义
  → ChatRoomManager.LoadAsync
  → new ChatRoomRole(roleDefinition)
  → ChatRoomService 根据 ChatRoomRoleKind 再次 BindRole
```

删除 `ChatRoomRoleKind` 后，`ChatRoomService` 不应再猜测角色运行时能力。恢复能力必须来自通用、可序列化的工具配置，而不是角色名称、模板 ID或系统提示词。

### 工作区传播

当前工作区变化链为：

```text
WorkspacePathTools
  → ChatRoomManager.SetWorkspacePathAsync
  → WorkspacePathChanging 事件
  → ChatRoomService
  → CodingAssistantRoleFactory.SetWorkspaceAsync
  → 更新绑定角色的 RuntimeTools
  → ChatRoomManager 更新自身 _workspacePath
  → 逐个设置 ChatRoomRole.WorkspacePath
  → CopilotChatManager.WorkspacePath
```

该链路有两个问题：

1. 新编程工具会话与角色默认文件工具通过两条路径更新，存在部分成功和状态不一致风险。
2. `ChatRoomService` 被迫成为具体工具的协调器。

### 关闭和移除

当前 `ChatRoomService.CloseCurrentSessionAsync` 同时停止循环、遍历编程助手、解绑工具、清空工厂工作区并分离事件。角色移除则通过 `RoleRemoved` 事件通知工厂解绑。

这说明具体运行时资源没有归属于角色或聊天室管理器，必须依赖外部补偿逻辑才能释放。

## 目标架构

### 总体原则

目标结构为：

```text
ChatRoomManager
  ├─ 唯一 WorkspacePath 状态
  ├─ 管理 ChatRoomRole 生命周期
  └─ 异步把工作区变化应用到每个角色

ChatRoomRole
  ├─ ChatRoomRoleDefinition（持久化配置）
  ├─ 默认文件工具运行时
  └─ 通用角色运行时扩展集合
       └─ 编程工作区工具扩展（Roslyn + .NET CLI）

ChatRoomService
  ├─ 会话创建/加载/保存
  ├─ 模型提供商注册
  └─ 调用 ChatRoomManager 的通用 API

RoleTemplateService
  ├─ 磁盘模板
  ├─ 同 ID 内存覆盖模板
  └─ 通用模板角色创建入口
```

具体工具资源归属于角色运行时扩展；工作区权威状态归属于 `ChatRoomManager`；`ChatRoomService` 不参与工具装配。

## 通用角色运行时扩展

### 建议增加 IChatRoomRoleRuntime

在 `AgentLib.ChatRoom` 内增加内部运行时扩展接口，名称可在实施时按现有命名调整：

```text
IChatRoomRoleRuntime : IAsyncDisposable
  InitializeAsync(ChatRoomRole role, CancellationToken)
  PrepareWorkspaceAsync(string? workspacePath, CancellationToken)
  CommitPreparedWorkspace()
  DiscardPreparedWorkspaceAsync()
```

接口需要表达以下能力：

- 随角色初始化。
- 根据目标工作区异步准备新资源。
- 在新资源准备成功后原子发布。
- 清理旧工作区资源。
- 随角色移除或聊天室关闭异步释放。

不建议让扩展直接拥有可独立设置的 `WorkspacePath` 属性。它只接收 `ChatRoomManager` 当前目标路径，并保存由该路径创建的资源会话。

### 两阶段工作区切换

为避免部分角色已切换而另一角色失败，工作区变化需要采用准备和提交分离的两阶段流程：

```text
规范化目标路径
  → 所有角色 PrepareWorkspaceAsync
  → 任一准备失败：释放本次已准备资源，保留旧状态
  → 所有角色执行保证不失败的纯内存 CommitPreparedWorkspace
  → 全部提交完成后更新 ChatRoomManager.WorkspacePath
  → 异步释放各角色旧资源
```

所有可能失败的 I/O、进程启动和工具创建必须在 Prepare 阶段完成。Commit 阶段只能交换已经准备好的内存状态，不执行可能失败的操作。最终验收不接受只能保证单角色一致、但允许多角色部分切换的实现。

工作区切换还必须与角色发言和工具调用协调。优先让管理器在切换前停止或等待当前自动循环和角色工具调用完成；如果需要允许并发切换，则旧运行时状态必须通过租约延迟到最后一个调用完成后再释放，不能在仍有调用者时终止 Language Server。

### 角色内部工具合并

`ChatRoomRole` 继续在发言时合并：

```text
默认文件工具
  + 角色运行时扩展提供的工具
  + 本轮聊天室附加工具
```

冲突优先级保持明确：

```text
本轮工具 > 角色运行时工具 > 默认工具
```

按 `AITool.Name` 使用 `StringComparer.Ordinal` 去重。运行时扩展只提供当前已提交工作区对应的工具快照。

## 工作区单一状态来源

### ChatRoomManager 是唯一权威状态

`ChatRoomManager.WorkspacePath` 是聊天室当前工作区的唯一权威值。规范化、相同路径比较和切换串行化都在 `ChatRoomManager` 完成。

以下状态不再作为独立工作区配置存在：

- `CodingAssistantRoleFactory.WorkspacePath`
- 可独立设置的 `ChatRoomRole.WorkspacePath` 公共 setter
- `WorkspacePathChanging` 事件
- 由 `ChatRoomService` 订阅并转发的工作区路径

`CodingWorkspaceToolSession.WorkspacePath` 可以保留为资源创建结果的只读诊断值，但不能成为另一个可写状态源。

### ChatRoomRole 的工作区 API

将当前同步属性设置改为内部异步生命周期 API，例如：

```text
PrepareWorkspaceAsync(string? workspacePath, CancellationToken)
CommitPreparedWorkspace()
DiscardPreparedWorkspaceAsync()
```

默认文件工具也应在该流程中更新，不能先设置 `CopilotChatManager.WorkspacePath` 再单独创建编程工具。

如果 `CopilotChatManager.WorkspacePath` 只能同步设置，应把它视为提交阶段的派生状态，由 `ChatRoomRole` 内部统一赋值，外部不能直接修改。

### 新增角色

`ChatRoomManager.AddRoleAsync` 在角色初始化后，应使用当前 `WorkspacePath` 完成该角色工作区准备和提交，再将角色视为成功加入。

若初始化或工作区应用失败：

- 不保留半初始化角色。
- 异步释放角色运行时资源。
- 不修改聊天室已有角色和工作区状态。

### 加载角色

`ChatRoomManager.LoadAsync` 创建角色时必须经过统一角色创建机制，而不是直接 `new ChatRoomRole(roleDefinition)` 后绕过运行时装配。

## 通用运行时工具配置与历史恢复

### 不使用角色种类，使用现有 Tools 配置

`ChatRoomRoleDefinition.Tools` 已经是通用、可序列化的工具配置集合。本次应复用该属性描述角色需要的运行时工具能力，不新增角色种类字段。

`Tools` 当前类型为 `List<ToolDefinition>`。使用 `ToolDefinition.Name` 作为稳定能力 ID，`Description` 只用于展示，不参与运行时匹配。工具 ID 应集中定义为常量。未知 ID 默认不创建运行时扩展，并通过现有诊断渠道报告。

为避免 Roslyn 和 .NET CLI 分别创建重复的工作区会话，建议使用单一聚合能力 ID：

```text
coding_workspace
```

该配置只表达“需要编程工作区能力”，不表达“角色是谁”。任何角色只要包含该配置，都可获得相同能力。Roslyn 是否启用、Language Server 命令、超时和失败降级策略由宿主应用设置及组合根注入，不能由磁盘模板提供可执行命令。

### 建议增加 IChatRoomRoleRuntimeFactory

增加通用运行时工厂注册机制：

```text
IChatRoomRoleRuntimeFactory
  SupportedToolIds
  CreateRuntime(ChatRoomRoleDefinition definition)
```

角色创建器根据 `definition.Tools` 查找匹配工厂，并按工厂身份去重后创建运行时扩展。同一工厂即使匹配多个工具配置也只能创建一个运行时实例。该过程不判断角色名称、模板 ID 或角色种类。

注册位置应位于应用组合根或 `ChatRoomManager` 的通用运行时工厂集合。`ChatRoomService` 可以把预先构造的通用角色创建器传给新管理器，但不得引用具体编程工具类型或包含针对某个 ToolId 的分支。

### 建议增加 ChatRoomRoleFactory

为统一普通创建、模板创建和历史恢复，增加通用 `ChatRoomRoleFactory` 或 `IChatRoomRoleFactory`：

```text
CreateRole(ChatRoomRoleDefinition definition)
```

职责：

- 创建普通 `ChatRoomRole`。
- 根据 `definition.Tools` 创建零到多个运行时扩展。
- 设置主线程调度器等通用依赖。
- 不启动工作区资源；工作区资源由 `ChatRoomManager.AddRoleAsync` 或加载流程异步应用。

`ChatRoomManager.LoadAsync`、`ChatRoomService.AddRoleAsync`、角色管理工具和模板加入会话都必须使用同一个角色工厂，避免某条路径漏装运行时能力。

## CodingAssistantRoleFactory 重构

### 保留职责

`CodingAssistantRoleFactory` 只负责提供编程助手预设：

- 创建编程助手 `ChatRoomRoleDefinition`。
- 在定义的 `Tools` 中写入聚合的编程工作区能力配置。
- 创建固定 ID 的角色大厅模板。
- 维护编程助手系统提示词和默认参与模式。

### 删除职责

删除以下状态和 API：

- `_boundRoles`
- `_workspaceSession`
- `_workspaceLock`
- `WorkspacePath`
- `RoslynInitializationException`
- `BindRole`
- `UnbindRole`
- `SetWorkspaceAsync`
- 工厂级 `IAsyncDisposable`

工厂不再持有任何会话级或工作区级资源。

### CreateDefinition

`CreateDefinition` 只负责创建包含稳定工具配置的可序列化定义。它本身不能也不应该调用 `SetRuntimeTools`，因为定义不是运行时角色。

删除 `CodingAssistantRoleFactory.CreateRole`，避免形成第二个角色创建入口。包括编程助手在内的所有角色都由唯一的通用角色工厂创建；运行时扩展在角色构造阶段已经存在，因此不再需要后续 `BindRole`。真正依赖路径的工具会话在角色加入 `ChatRoomManager` 并获得当前工作区时异步创建。

## 编程工作区运行时

### 重构 CodingWorkspaceToolSession

`CodingWorkspaceToolSession` 继续负责单个工作区的具体资源：

- `RoslynAgentTools`
- `DotNetCliTools`
- 当前工具快照
- Roslyn 初始化异常
- 幂等异步释放

它不负责寻找或更新角色，也不与 `ChatRoomService` 交互。

### 增加 CodingWorkspaceRoleRuntime

建议增加内部 `CodingWorkspaceRoleRuntime`，实现通用角色运行时扩展接口。职责如下：

- 根据工具配置判断启用 Roslyn、.NET CLI 或两者。
- 为目标工作区创建候选 `CodingWorkspaceToolSession`。
- 原子替换当前会话并发布新的 `AITool` 快照。
- 释放旧会话。
- 工作区为空时清除工具并释放资源。
- 角色释放时终止 Language Server。

Roslyn 初始化失败降级策略来自宿主应用设置，而不是角色模板或 `ChatRoomService`。

## ChatRoomService 解耦

### 应保留的职责

`ChatRoomService` 只负责：

- 创建和加载聊天室会话。
- 持有当前 `ChatRoomManager`。
- 注册模型提供商。
- 转发通用会话事件。
- 调用管理器添加、移除、保存和关闭角色。

### 应删除的内容

删除：

- `_codingAssistantRoleFactory`
- `CreateCodingAssistantRuntimeTemplate`
- 加载会话后的角色种类判断和绑定
- 添加角色时的角色种类判断和绑定
- 关闭会话时的编程助手遍历和工作区清理
- `WorkspacePathChanging` 订阅
- `RoleRemoved` 订阅
- 对 `CodingAssistantRoleFactory.DisposeAsync` 的调用

编程助手模板由应用组合根直接通过 `CodingAssistantRoleFactory` 创建并注册到 `RoleTemplateService`。

### 添加角色 API

保留 `AddRoleAsync(ChatRoomRoleDefinition, CancellationToken)` 作为普通便利入口，但内部使用通用角色工厂。

同时增加：

```text
AddRoleAsync(ChatRoomRole role, CancellationToken)
```

用于已经由通用角色工厂完成运行时扩展装配的角色。两个入口最终都调用 `ChatRoomManager.AddRoleAsync`，不包含具体角色分支。

## 角色模板重构

### 删除 IsRuntimeBuiltIn

从 `RoleTemplate` 删除 `IsRuntimeBuiltIn` 和对应 `JsonIgnore`。

`RoleTemplateService.RegisterRuntimeTemplate` 只验证：

- 模板不为 null。
- `TemplateId` 非空。

运行时身份始终由 `_runtimeTemplates` 是否包含该 `TemplateId` 决定，不按对象实例判断。

### 保存和删除语义

建议保持以下规则：

- `LoadAll`：磁盘模板先加载；同 ID 项被 `_runtimeTemplates` 覆盖。
- `SaveAsync`：如果该 ID 已注册为运行时模板，只更新内存项；否则正常写入磁盘。
- `Delete`：如果该 ID 是运行时模板，只从当前进程隐藏；不删除同 ID 磁盘文件。
- 普通模板无论内容是否与编程助手相似，都按普通磁盘模板处理。
- 模板对象可以正常序列化，不需要在模型中保留运行时标记。

为避免把一个新对象误判为运行时编辑，运行时判断以已注册 ID 为准；固定 ID 的运行时覆盖本身就是该服务实例的明确状态。保留 `_hiddenRuntimeTemplateIds` 或等价 tombstone 状态，使运行时模板被删除后，同 ID 磁盘模板不会在当前服务实例中立即重新出现；重新注册同 ID 时清除 tombstone。

### 模板创建角色

`RoleTemplateService.ToDefinition` 继续负责通用配置复制。

角色大厅加入会话时应采用统一流程：

```text
RoleTemplate
  → ToDefinition
  → 通用 IChatRoomRoleFactory.CreateRole
  → ChatRoomService.AddRoleAsync(role)
```

因为编程能力由 `Definition.Tools` 的通用配置恢复，不再需要按模板 ID 选择专用工厂。

## 会话持久化与恢复

删除 `ChatRoomRoleKind` 后，持久化恢复依赖 `ChatRoomRoleDefinition.Tools`：

```text
角色定义 JSON
  → ChatRoomManager.LoadAsync
  → IChatRoomRoleFactory.CreateRole(definition)
  → 根据 Tools 创建运行时扩展
  → 应用 ChatRoomManager 当前工作区
```

不持久化以下内容：

- 当前工作区绝对路径，除非聊天室原有设计明确要求保存；本次默认不新增持久化。
- `AITool` 实例。
- Roslyn 进程。
- `CodingWorkspaceToolSession`。
- Roslyn 初始化异常。

如果历史数据中已存在暂存实现写出的 `Kind` 字段，不能只依赖 `System.Text.Json` 忽略未知字段，因为这会让仅通过 Kind 标识、Tools 为空的旧编程助手静默丢失能力。

在持久化读取边界增加一次性旧格式迁移：使用 `System.Text.Json` 读取原始角色 JSON；当旧 `Kind` 明确为 CodingAssistant 且 Tools 中尚无 `coding_workspace` 时，补充该通用工具配置。迁移逻辑只存在于持久化兼容层，不在领域模型中保留 `ChatRoomRoleKind` 或 `Kind` 属性。未知 Kind 不装配任何工具。迁移后的再次保存只输出新格式。

`ChatRoomManager.LoadAsync` 应采用候选集合事务：先通过通用角色工厂创建和初始化全部候选角色，应用工作区并恢复 AgentSession；任一步失败或取消都释放全部候选角色且不修改旧集合。全部成功后再替换角色集合并释放旧角色。如果 `LoadSessionAsync` 始终创建全新管理器，也应保留该事务语义，避免公共 `LoadAsync` 留下部分加载状态。

### ChatRoomManager 异步关闭

增加 `ChatRoomManager.CloseAsync(CancellationToken)`，并可同时实现 `IAsyncDisposable` 供不可取消的最终清理使用。关闭顺序为：

1. 阻止新增角色、工作区切换和新发言。
2. 停止并等待自动循环。
3. 尽最大努力释放所有角色运行时资源，单个角色失败不能阻止其他角色释放。
4. 清空角色集合和派生状态。
5. 聚合并向调用方报告释放异常。

取消令牌只允许取消进入关闭前的等待；一旦开始释放资源，必须继续尽力完成全部清理。`ChatRoomService.CloseCurrentSessionAsync` 只等待管理器关闭并清空当前管理器引用，不遍历角色，也不识别任何工具类型。

## 异步 API 清理

### ChatRoomService.CloseCurrentSession

删除同步 `CloseCurrentSession()`，所有调用点迁移到：

```text
await CloseCurrentSessionAsync(cancellationToken)
```

当前已知调用点主要位于 `ChatRoomServiceTests`，实施时仍必须进行仓库级搜索：

- 测试清理
- 关闭会话测试
- `SessionChanged` 测试
- 会话列表测试
- 删除会话测试

测试清理应改为 MSTest 支持的异步 `[TestCleanup] public async Task CleanupAsync()`，并等待 `DisposeAsync` 或 `CloseCurrentSessionAsync` 完成后再删除临时目录。

### ChatRoomManager.SetWorkspacePath

删除同步 `SetWorkspacePath(string?)`，只保留 `SetWorkspacePathAsync`。

重新搜索全部调用点并迁移，不允许保留同步兼容包装。`WorkspacePathTools` 已经是异步入口，应继续直接等待管理器完成切换。

### 角色移除

具体运行时资源要求异步释放，因此应优先删除或收缩同步 `RemoveRole`：

- `ChatRoomManager.RemoveRoleAsync` 负责从集合移除并等待角色释放。
- `ChatRoomService.RemoveRoleAsync` 转发异步调用。
- UI 命令和工具调用点全部等待异步移除。

如果同步 `RemoveRole` 存在外部兼容要求，应在本轮明确决定是否破坏 API；不得通过阻塞等待异步释放保留同步实现。建议直接删除同步入口。

### 应用退出

Avalonia 关闭事件本身是同步事件，不能继续使用 `DisposeAsync().AsTask().GetAwaiter().GetResult()`。

需要改为框架支持的异步退出协调方式，例如：

1. 首次收到关闭请求时取消本次关闭。
2. 异步等待 `ChatRoomService.DisposeAsync` 和其他资源释放。
3. 标记清理完成后再次发起关闭。

实施时先查明当前 Avalonia 版本的 `ShutdownRequestedEventArgs` 取消能力和应用生命周期 API，再选择与项目现有模式一致的实现。退出协调状态机必须防止重入；不允许无人观察的 fire-and-forget 任务。若框架事件必须使用 `async void`，处理器必须显式捕获并通过统一错误通道报告异常。

## 建议文件变化

### AgentLib.ChatRoom

- `Model/ChatRoomRoleDefinition.cs`
  - 删除 `ChatRoomRoleKind` 和 `Kind`。
  - 明确 `Tools` 的通用运行时工具配置语义。
- `Model/RoleTemplate.cs`
  - 删除 `IsRuntimeBuiltIn` 及不再需要的 JSON using。
- `ChatRoomRole.cs`
  - 管理通用运行时扩展。
  - 增加异步工作区准备、提交、回滚和释放。
  - 移除可由外部直接设置的工作区属性 setter。
- `ChatRoomManager.cs`
  - 成为工作区路径唯一状态源。
  - 统一角色创建、添加、加载、移除和释放流程。
  - 删除同步工作区设置和同步角色移除入口。
- `Services/ChatRoomRoleFactory.cs` 或等价新文件
  - 根据定义和工具注册表创建完整角色。
- `Services/CodingAssistantRoleFactory.cs`
  - 改为无状态预设工厂。
  - 删除绑定、工作区和释放职责。
- `Services/CodingWorkspaceToolSession.cs`
  - 保留单工作区资源会话职责。
- `Services/CodingWorkspaceRoleRuntime.cs` 或等价新文件
  - 承载编程工具的角色级工作区生命周期。
- `Services/RoleTemplateService.cs`
  - 通过内存注册状态管理覆盖模板。
  - 删除对 `IsRuntimeBuiltIn` 和 `Kind` 的依赖。
- `Services/ChatRoomService.cs`
  - 删除全部具体编程工具耦合。
  - 只依赖通用角色工厂和管理器 API。
- `Tools/WorkspacePathTools.cs`
  - 保持纯异步工作区设置。
- `Tools/ChatRoomRoleManagementTools.cs`
  - 使用通用角色工厂和异步移除 API。

### Avalonia 宿主

- `App.axaml.cs`
  - 在组合根创建并注册运行时工具工厂、通用角色工厂和编程助手模板。
  - 改造异步退出流程，删除同步阻塞。
- `RoleLobbyViewModel.cs`
  - 模板加入会话时通过通用角色工厂创建角色。
- 其他调用 `CloseCurrentSession`、`RemoveRole` 或同步工作区设置的 ViewModel
  - 迁移为异步命令。

### 测试

- 删除围绕 `ChatRoomRoleKind` 和 `IsRuntimeBuiltIn` 的测试。
- 替换为通用工具配置恢复、模板覆盖状态和异步生命周期测试。
- 将同步清理迁移为异步清理。

## 分阶段实施步骤

### 第一阶段：建立回归基线

1. 记录当前暂存变更和现有测试状态。
2. 运行 `AgentLib.ChatRoom.Tests`、`ChatRoom.Shell.Tests` 和 `ChatRoom.slnx` 构建，区分已有失败与本轮引入失败。
3. 为工作区切换、角色移除和会话关闭补充必要的生命周期测试骨架。

### 第二阶段：移除模型特例

1. 删除 `ChatRoomRoleKind` 及 `ChatRoomRoleDefinition.Kind`。
2. 删除所有 Kind 复制、序列化、判断和测试。
3. 删除 `RoleTemplate.IsRuntimeBuiltIn`。
4. 将 `RoleTemplateService` 改为依据内存注册表管理覆盖、保存和删除。
5. 在持久化边界迁移旧 `Kind = CodingAssistant` 且 Tools 为空的数据，并增加未知 Kind 不装配工具的兼容测试。

### 第三阶段：建立通用角色运行时工厂

1. 明确 `ChatRoomRoleDefinition.Tools` 的配置格式和稳定工具 ID。
2. 增加通用运行时扩展接口。
3. 增加运行时扩展工厂注册表。
4. 增加通用 `IChatRoomRoleFactory`。
5. 将 `ChatRoomService`、`ChatRoomManager.LoadAsync` 和角色管理工具的创建路径统一到该工厂。

### 第四阶段：下沉编程工作区资源

1. 增加 `CodingWorkspaceRoleRuntime`。
2. 将 `CodingWorkspaceToolSession` 交由该运行时持有。
3. 将编程助手映射为单一 `coding_workspace` Tools 能力项。
4. 删除 `CodingAssistantRoleFactory` 的绑定集合、工作区状态和异步释放职责。
5. 让 `CodingAssistantRoleFactory.CreateDefinition` 写入工具配置。
6. 删除 `CodingAssistantRoleFactory.CreateRole`，让所有角色实例统一由通用角色工厂完成运行时扩展装配。

### 第五阶段：统一工作区状态

1. 将 `ChatRoomManager.WorkspacePath` 确立为唯一权威状态。
2. 删除 `WorkspacePathChanging` 事件及相关订阅。
3. 删除 `ChatRoomRole.WorkspacePath` 的外部 setter。
4. 实现角色工作区准备、提交和回滚。
5. 实现管理器级串行两阶段工作区切换。
6. 让新增角色在加入前应用当前工作区。
7. 验证切换失败后旧默认工具和旧编程工具仍保持一致可用。

### 第六阶段：清理 ChatRoomService

1. 删除 `CodingAssistantRoleFactory` 字段和创建逻辑。
2. 删除编程助手模板创建入口。
3. 删除编程角色绑定、解绑和工作区转发。
4. 增加接收完整 `ChatRoomRole` 的通用添加入口。
5. 保留 Definition 添加入口并委托通用角色工厂。
6. 确认服务只承担会话、模型提供商和通用事件职责。

### 第七阶段：完成异步生命周期

1. 删除 `ChatRoomService.CloseCurrentSession()`。
2. 迁移所有调用点到 `CloseCurrentSessionAsync`。
3. 删除 `ChatRoomManager.SetWorkspacePath()`。
4. 迁移所有工作区调用点到异步 API。
5. 删除或替换同步角色移除 API。
6. 让角色实现幂等异步释放。
7. 让 `ChatRoomManager` 在移除、加载失败和关闭时等待角色释放。
8. 改造 Avalonia 应用退出流程，删除同步阻塞异步释放。

### 第八阶段：宿主和模板接入

1. 在应用组合根注册通用运行时工具工厂。
2. 在应用组合根直接创建 `CodingAssistantRoleFactory`。
3. 注册固定 ID 的编程助手内存模板。
4. 将角色大厅加入会话链改为“模板 → 定义 → 通用角色工厂 → 完整角色”。
5. 验证同 ID 磁盘模板被内存模板覆盖但文件不被删除。
6. 验证运行时模板编辑和删除只影响当前服务实例。

### 第九阶段：测试与验证

1. 运行 `AgentLib.ChatRoom.Tests`。
2. 运行 `ChatRoom.Shell.Tests`。
3. 运行与工作区文件工具、Roslyn 工具和 .NET CLI 工具相关的测试。
4. 构建 `ChatRoom.slnx`。
5. 搜索并确认不存在 `ChatRoomRoleKind`、`IsRuntimeBuiltIn`、`BindRole`、`UnbindRole`。
6. 搜索并确认不存在对异步 API 的 `GetAwaiter().GetResult()`、`.Result` 或 `.Wait()`。
7. 审查 Git 变更，确认没有修改任务外代码或写入本地绝对路径。

## 重点测试场景

### 普通角色不受影响

预期：

- 没有 Tools 配置的角色仍按原流程创建和发言。
- 未设置工作区时不创建编程资源。
- 默认模型注册、技能目录和会话持久化行为保持不变。

### 任意角色声明编程工具

预期：

- 不依赖角色名称、模板 ID 或角色种类。
- 只要 Definition.Tools 包含对应配置，即创建编程工作区运行时。
- 历史会话恢复后能够由同一工具配置重新创建运行时扩展。

### 设置有效工作区

预期：

- 管理器保存唯一规范化路径。
- 默认文件工具、Roslyn 和 .NET CLI 使用同一路径。
- 所有角色准备成功后才提交新路径。
- 相同路径重复设置不会重启 Language Server。

### 工作区切换失败

预期：

- `ChatRoomManager.WorkspacePath` 保持旧值。
- 所有角色继续使用旧工作区工具。
- 已创建的候选资源被释放。
- 不留下半切换角色或额外 Language Server 进程。

### 新角色加入已有工作区

预期：

- 角色初始化后自动应用管理器当前工作区。
- 应用失败时角色不加入集合。
- 已创建的运行时资源被释放。

### 移除角色

预期：

- 角色从集合移除。
- 角色私有运行时资源被异步释放。
- 不依赖 `RoleRemoved` 事件让外部工厂补偿清理。

### 关闭会话

预期：

- 自动循环先停止。
- 所有角色运行时资源完成异步释放。
- 当前管理器清空。
- 临时目录可以立即删除，证明没有残留文件或进程占用。

### 模板覆盖

预期：

- `RoleTemplate` 不包含运行时来源字段。
- 注册的内存模板覆盖同 ID 磁盘模板。
- 内存模板本身可正常序列化。
- 保存已注册 ID 只更新内存。
- 删除已注册 ID 不删除磁盘文件。
- 新建 `RoleTemplateService` 后磁盘模板重新可见。

### 旧会话兼容

预期：

- 旧 JSON 中 `Kind = CodingAssistant` 且 Tools 为空时，在持久化兼容层迁移为 `coding_workspace` 配置。
- 迁移后的领域模型和再次保存结果不包含 Kind。
- 未知 Kind 不会误装配任何运行时工具。
- 新格式的工具恢复只依赖通用 Tools 配置。

### 应用退出

预期：

- UI 线程不执行同步阻塞等待。
- 关闭请求期间资源释放可等待且异常可观测。
- Roslyn Language Server 在进程退出前正常终止。

## 风险与待确认项

### Tools 配置边界

`ChatRoomRoleDefinition.Tools` 当前使用 `List<ToolDefinition>`。本轮只使用 `Name` 保存稳定能力 ID，不把 Language Server 命令等可执行配置写入模板或会话 JSON。若未来需要参数化工具，应单独设计受信任配置边界，不能把宿主命令直接交给磁盘模板控制。修改模型后必须同步更新 `System.Text.Json` 源生成上下文和兼容测试。

### 多角色工作区事务

完整两阶段提交会增加接口和状态管理复杂度，但能从根本上避免默认工具和编程工具路径不一致。若实施中发现 `CopilotChatManager` 无法支持候选状态，应先封装派生状态，再提交路径，不应退回事件转发方案。

提交阶段必须是不可失败的内存交换；如果无法满足该条件，应先改变内部状态模型，不允许先发布管理器路径再承担部分角色提交失败的风险。

### 运行时工具共享

本计划默认每个带编程工具的角色拥有自己的 `CodingWorkspaceToolSession`，职责最清晰但可能启动多个 Language Server。若需要共享，应另行引入由 `ChatRoomManager` 持有的通用工作区资源租约服务，通过引用计数管理，不能重新放回 `ChatRoomService` 或 `CodingAssistantRoleFactory`。

本轮优先保证所有权和释放正确；共享优化不应阻塞重构。

### Avalonia 异步关闭

同步关闭事件如何取消并重新发起关闭取决于当前 Avalonia 版本。实施时必须先验证实际 API，不猜测事件参数能力。若框架不提供直接异步关闭钩子，应在窗口层实现显式关闭协调状态机。

需要覆盖重复退出请求、释放失败、释放期间再次关闭和清理超时等行为。应用退出清理使用不可取消流程或明确超时策略，不能因普通取消令牌留下部分角色资源。

### API 兼容性

删除同步 `CloseCurrentSession`、`SetWorkspacePath` 和可能的 `RemoveRole` 是有意的破坏性修改。必须一次性迁移仓库内全部调用点，并通过搜索确认没有残留。

### 暂存变更基线

当前工作区已有大量暂存修改。本次重构应基于这些修改继续调整，不应重置或覆盖用户其他变更。每个阶段完成后都应审查差异，避免误删无关功能。

## 验收标准

1. `ChatRoomRoleDefinition` 中不存在 `ChatRoomRoleKind` 或等价角色种类分支。
2. `RoleTemplate` 中不存在 `IsRuntimeBuiltIn` 或等价来源标记。
3. `ChatRoomService` 不引用 `CodingAssistantRoleFactory`、Roslyn、.NET CLI 或编程工作区会话。
4. `CodingAssistantRoleFactory` 不包含 `BindRole`、`UnbindRole`、角色集合或工作区状态。
5. 编程助手通过普通 Definition.Tools 配置声明运行时能力。
6. 普通创建、模板创建和历史加载统一使用同一个通用角色工厂。
7. `ChatRoomManager.WorkspacePath` 是唯一可写工作区状态源。
8. 默认文件工具、Roslyn 和 .NET CLI 在任意时刻使用一致的已提交工作区。
9. 工作区切换失败不会破坏旧工作区或泄漏候选资源。
10. 角色移除、会话关闭和应用退出都会等待运行时资源异步释放。
11. 仓库内不存在 `CloseCurrentSession()` 和 `SetWorkspacePath()` 的同步阻塞包装。
12. 仓库内相关路径不存在 `GetAwaiter().GetResult()`、`.Result` 或 `.Wait()`。
13. 内存模板可正常序列化，并由 `RoleTemplateService` 的注册状态实现同 ID 覆盖。
14. 旧 `Kind = CodingAssistant` 且 Tools 为空的会话会在持久化边界迁移为 `coding_workspace`，再次保存不包含 Kind。
15. 同时声明多个由编程工作区工厂支持的工具配置时只创建一个工作区运行时实例。
16. 工作区切换不会在仍有工具调用使用旧会话时提前释放 Language Server。
17. `ChatRoomManager.CloseAsync` 幂等关闭并尽力释放全部角色资源，单个释放失败可观测且不阻塞其他清理。
18. `System.Text.Json` 源生成上下文覆盖所有新增或调整的持久化模型。
19. 所有新增公共 API 具有 XML 文档注释，修改的路径拼接使用 `Path.Join`。
20. `AgentLib.ChatRoom.Tests` 和 `ChatRoom.Shell.Tests` 通过。
21. `ChatRoom.slnx` 构建成功。
22. 应用退出后不存在遗留 Roslyn Language Server 进程。

## 结论

本次重构的核心不是把编程助手逻辑移动到另一个服务，而是建立通用的角色运行时扩展边界：持久化定义通过 Tools 描述能力，通用角色工厂恢复运行时扩展，角色拥有并释放具体资源，管理器统一控制工作区状态，应用服务只管理聊天室会话。

完成后，编程助手不会在聊天室模型中拥有特殊身份；它只是一个系统提示词和工具配置不同的普通角色。新增其他工作区角色能力时，也不需要修改 `ChatRoomService`、增加角色种类枚举或复制新的绑定生命周期。