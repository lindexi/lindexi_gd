# ChatRoom CodingAgent 角色分流设计

## 文档定位

本文只解决一个问题：系统如何根据可持久化角色定义稳定选择“应由 `AgentLib.Coding.CodingAgent` 执行”的角色运行时，并让 `ChatRoomRole.SpeakAsync` 在不持有专用 `CodingAgent` 字段的前提下进入对应执行路径。

本文不实现代码，但方案必须兼容本轮已经明确的整体重构方向：

- `AgentLib.Coding` 新增统一入口 `CodingAgent`
- `CodingAgent` 提供纯文本与完整有序 `IReadOnlyList<AIContent>` 两组运行重载
- 运行参数包含 `IManualSendMessageContext` 和工作路径
- 返回 `CodingAgentRunResult`，同时暴露可流式绑定的消息与最终完成任务
- `ChatRoomRole` 只持有一个统一角色执行器，不为 Coding、Writing 等执行引擎分别增加字段
- 切换工作路径只更新工具，不清空或新建 Agent 会话
- 编程提示词、编程工具选择、运行取消、历史补全、工作区资源租约和释放集中在 `AgentLib.Coding`
- `AgentLib.ChatRoom` 删除重复的编程提示词、编程工具适配器和手动发送循环
- 不修改 `AgentLib` 或 `IManualSendMessageContext`；Standard 路径继续使用标准发送完成工具审批绑定，Coding 路径不接收 ChatRoom 协调工具

本文形成的结论优先于已有文档中“删除角色种类、所有 AI 角色采用同一执行路径”的旧结论。已有文档关于工具不持久化、工作区资源安全切换、异步释放和并发保护的结论仍然有效。

## 当前实现事实

### 已经存在稳定的角色判据

当前 `ChatRoomRoleDefinition` 已包含：

```text
ChatRoomRoleKind
  - Standard
  - CodingAssistant
```

`Kind` 具有以下特征：

1. 属于可持久化角色定义
2. 会被角色模板复制
3. 会写入并恢复聊天室会话
4. 历史恢复后仍由 `ChatRoomRoleFactory` 读取
5. 当前工厂已经使用它决定是否装配编程运行时工具

因此，当前代码已经具备可靠分流所需的数据基础，不需要通过名称、提示词或工具集合重新推断。

### 当前 SpeakAsync 同时承担了过多职责

当前 `ChatRoomRole.SpeakAsync` 负责：

1. 校验增量消息
2. 构建角色和聊天室系统提示词
3. 创建 `IManualSendMessageContext`
4. 向可见会话追加用户和助手消息
5. 合并默认工具、编程工具和本轮工具
6. 创建 `ChatClientAgent`
7. 获取或创建 `AgentSession`
8. 运行流式发送循环
9. 补全 AgentSession 历史
10. 更新流式消息和 Token 用量
11. 处理取消

这正是本轮需要从 ChatRoom 中移出的重复编排。

### 当前人工审批绕过应通过移除错误发送路径解决

`WorkspacePathTools.CreateSetWorkspacePathTool` 返回的是经过 `HumanApprovalTool.Wrap` 配置的工具。

正常 `CopilotChatManager.SendMessage` 路径会在每次发送时调用运行时工具绑定逻辑，把配置型审批工具绑定到当前 `CopilotChatContext`、助手消息和取消令牌。

当前 `ChatRoomRole.RunManualSendAsync` 只是把工具直接写入 `ChatOptions.Tools`，没有执行这一步绑定。配置型审批工具的防御性后备行为会直接透传到内部函数，因此 `set_workspace_path` 可能没有等待人工审批就执行。

迁移后不通过扩展 `IManualSendMessageContext` 修补这条旧路径，也不把 ChatRoom 的 `set_workspace_path`、角色管理工具或其他本轮工具传给 `CodingAgent`：

- Standard 执行器回到 `CopilotChatManager.SendMessage`，由现有实现完成工具合并和 `HumanApprovalTool` 运行时绑定
- Coding 执行器调用 `CodingAgent` 时只使用 `AgentLib.Coding` 自己拥有的编程工具，不接收 `set_workspace_path`，因此不存在该审批工具被手动发送绕过的问题
- 工作区切换继续由 `ChatRoomManager.SetWorkspacePathAsync` 从聊天室外层显式发起，而不是由 Coding 模型通过 ChatRoom 协调工具发起

因此，本轮不修改 `AgentLib` 项目，也不为 `IManualSendMessageContext` 增加宿主指令、宿主工具或运行时工具绑定 API。

### 当前工作区工具快照并不稳定

`CodingWorkspaceToolProvider.SetWorkspacePathAsync` 当前执行：

1. 创建新 `CodingWorkspaceToolSession`
2. 替换 `_session`
3. 立即释放旧 Session

即使发言开始时已经复制了旧 `AITool` 列表，这些工具闭包仍然引用旧 `RoslynAgentTools` 和旧 `RoslynLspClient`。工作区切换后，进行中的 Agent 可能继续调用已经释放的对象。

因此，“复制工具列表”不是资源快照。稳定快照必须同时持有底层工作区 Session 的运行引用。

## 方案比较

| 方案 | 持久化恢复 | 扩展性 | 生命周期边界 | 结论 |
|------|------------|--------|--------------|------|
| 根据 `RoleName` 判断 | 名称可修改、可重复、本地化后不稳定 | 每种角色继续增加文本约定 | 无法表达运行时依赖 | 拒绝 |
| 根据固定模板 ID 判断 | 模板来源未随角色定义持久化 | 模板与运行引擎永久耦合 | 恢复后无法判断 | 拒绝 |
| 搜索系统提示词内容 | 提示词可编辑，文案升级会改变结果 | 每种引擎都要维护文本探测 | 把行为文本误当类型系统 | 拒绝 |
| 检查是否包含某个工具 | 工具是运行时资源，不应持久化 | 新引擎需要新的工具探测规则 | 形成身份与资源状态循环依赖 | 拒绝 |
| 检查工厂具体类型 | 工厂来源不属于角色数据 | 依赖组合根实现细节 | 历史恢复后无来源信息 | 拒绝 |
| 只检查专用 Agent 字段是否为空 | 运行时可用但无法恢复判据 | 每增加一种 Agent 都增加字段和分支 | 配置错误会被静默解释为其他角色 | 拒绝 |
| `ChatRoomRole` 子类或标记接口 | 仍需要持久化鉴别器才能恢复子类 | 角色继承体系随引擎增长 | 管理器和持久化更复杂 | 拒绝 |
| 只在 `SpeakAsync` 检查现有 `Kind` 并临时创建 Agent | 判据可靠 | `SpeakAsync` 持续膨胀 | 每次发言创建资源，生命周期错误 | 不完整 |
| 持久化执行种类 + 为每种 Agent 注入一个专用字段 | 可靠 | 字段、校验、切换和释放逻辑随引擎增长 | 单个引擎边界清晰，角色边界恶化 | 拒绝 |
| 持久化执行种类 + 工厂注入单一统一执行器 | 可靠 | 新引擎只新增执行器、执行器工厂和注册项 | 创建、运行、切换、释放边界清晰 | 推荐 |

## 核心决策

### 决策一：使用显式、可持久化的执行种类

推荐把当前含混的角色种类命名收敛为“执行引擎种类”：

```text
ChatRoomRoleExecutionKind
  - Standard
  - Coding
```

`ChatRoomRoleDefinition` 对应属性建议命名为：

```text
ExecutionKind
```

该属性应保持 `init`-only，并显式默认为 `Standard`。反序列化可以产生未定义的枚举数值，因此不能只依赖默认分支；角色工厂必须校验 `Enum.IsDefined` 或使用穷尽匹配拒绝未知值。

选择该命名的原因：

- `CodingAssistant` 容易被理解为角色显示身份或模板类别
- 本字段真正决定的是 `SpeakAsync` 使用普通对话编排还是 `CodingAgent` 编排
- 它不表示工具授权，不表示角色名称，也不表示 UI 分类
- 项目尚未发布，可以直接重命名，不需要保留旧枚举别名

若实施时为了缩小首个提交而暂时沿用 `ChatRoomRoleKind.CodingAssistant`，其语义也必须按本文限定为“执行引擎判据”；最终仍建议完成命名收敛。

### 决策二：执行种类是唯一主判据，但只在工厂解释

`Definition.ExecutionKind` 是选择运行引擎的唯一主判据。该判据应在 `ChatRoomRoleFactory.CreateRole` 创建角色时解释一次，而不是由 `SpeakAsync` 在每次发言时重复判断。

`SpeakAsync` 不搜索角色特征，也不按具体 Agent 类型编写分支。它只委托给构造时已经与 `ExecutionKind` 匹配的统一执行器。

以下信息不得参与判断：

- `RoleName`
- 模板 ID
- 模板分类或标签
- `SystemPrompt` 文本
- 当前工具名称或数量
- 工作区是否已设置
- 是否存在 Roslyn Language Server
- 当前执行器的具体 CLR 类型
- 执行器内部是否已经创建某种 Agent

执行器可以暴露其声明的 `ExecutionKind` 供构造时校验，但该属性只用于验证工厂装配结果，不反向参与运行引擎选择。

### 决策三：由角色工厂注入一个统一角色执行器

`ChatRoomRole` 不应持有 `CodingAgent?`、`WritingAgent?` 等一组互斥的专用字段，也不应在 `SpeakAsync` 中临时创建具体 Agent。

目标关系为：

```text
ChatRoomRoleDefinition.ExecutionKind
  → ChatRoomRoleFactory
      → Standard: 创建 StandardChatRoomRoleExecutor
      → Coding: 创建 CodingChatRoomRoleExecutor
          → 内部创建并独占 CodingAgent
      → 将唯一的 IChatRoomRoleExecutor 注入 ChatRoomRole
```

`ChatRoomRole` 内部只持有一个非空、与具体执行种类无关的运行时引用：

```text
IChatRoomRoleExecutor _executor
```

推荐的内部契约为：

```text
IChatRoomRoleExecutor : IAsyncDisposable
  - ExecutionKind
  - RunAsync(ChatRoomRoleExecutionContext, IReadOnlyList<AIContent>, CancellationToken)
      → ChatRoomRoleExecutionResult
  - SetWorkspacePathAsync(CopilotChatManager, string?, CancellationToken)
```

`ChatRoomRoleExecutionContext` 是 `AgentLib.ChatRoom` 内部的单次运行上下文，至少携带：

- 本角色一直使用的 `CopilotChatManager`
- Standard 执行器需要的首次系统提示词
- Standard 执行器需要的本轮额外工具

后两项只属于 Standard 发送配置。Coding 执行器只读取同一个 `CopilotChatManager` 以创建现有 `IManualSendMessageContext`，不会把系统提示词或额外工具转交给 `CodingAgent`。

该上下文不携带在 `SpeakAsync` 外层预先读取的工作路径快照。Coding 执行器必须从自身已经提交的工作区状态中读取路径，并把该路径传给 `CodingAgent.RunAsync`；否则发言启动与工作区切换交错时，迟到的旧路径可能把 Provider 切回旧工作区。

内部统一结果建议命名为 `ChatRoomRoleExecutionResult`，至少包含同一个可流式绑定的 `CopilotChatMessage` 和完整生命周期任务。`ChatRoomRole` 再统一补充模型显示名、`_hasSpoken`、`LastUsageDetails` 和空回复语义，避免这些 ChatRoom 状态进入 `AgentLib.Coding`。

具体实现职责如下：

- `StandardChatRoomRoleExecutor` 调用 `CopilotChatManager.SendMessage`，显式传入首次系统提示词和本轮额外工具；工作区更新时只同步该 ChatManager 的默认工具路径
- `CodingChatRoomRoleExecutor` 封装并独占一个 `CodingAgent`，只把 Manual Context、有序用户内容和已提交工作路径适配到 `CodingAgent.RunAsync`，并一致提交 Coding Session 与 ChatManager 默认工具路径
- 未来若增加写作执行引擎，只新增 `WritingChatRoomRoleExecutor`、对应执行器工厂及注册项；`ChatRoomRole` 的字段和 `SpeakAsync` 不变

这些执行器和契约应保持为 `AgentLib.ChatRoom` 内部实现，不需要暴露为跨程序集公共插件 API。`CodingChatRoomRoleExecutor` 可以引用 `AgentLib.Coding`，但 `AgentLib.Coding` 不引用任何 ChatRoom 类型。

工厂负责提供 `CodingAgent` 所需配置和依赖，例如 Language Server 命令、工作区工具 Provider 或其他 `AgentLib.Coding` 内部选项。`ChatRoomRole` 不知道 `CodingAgent`、Roslyn、CLI 或工作区 Session 的具体类型。

### 决策四：执行器创建使用注册表，不把专用字段转移到工厂

统一执行器解决了 `ChatRoomRole` 的字段膨胀，但 `ChatRoomRoleFactory` 也不应继续演变为：

```text
CodingAgentFactory? _codingFactory
WritingAgentFactory? _writingFactory
ReviewAgentFactory? _reviewFactory
```

推荐增加内部执行器工厂契约，并由 `ChatRoomRoleFactory` 只持有一个按执行种类索引的注册表：

```text
IChatRoomRoleExecutorFactory
  - ExecutionKind
  - Create(ChatRoomRoleExecutorCreationContext)
      → IChatRoomRoleExecutor

ChatRoomRoleFactory
  - IReadOnlyDictionary<ChatRoomRoleExecutionKind, IChatRoomRoleExecutorFactory> _executorFactories
```

内建注册项至少包括：

- `StandardChatRoomRoleExecutorFactory`
- `CodingChatRoomRoleExecutorFactory`，由它持有 Coding 所需配置并创建独立 `CodingAgent`

创建角色时按 `ExecutionKind` 查找恰好一个执行器工厂。未知枚举值、缺少注册项、重复注册同一种类或工厂返回错误种类的执行器都必须立即失败。未来增加 Writing 时新增执行器、执行器工厂和一条组合根注册，不给 `ChatRoomRole` 或 `ChatRoomRoleFactory` 增加 Writing 专用字段。

该注册表首先是 `AgentLib.ChatRoom` 内部的组合机制，不需要在本轮提前设计公共插件发现协议、字符串类型名或反射扫描。若未来确有外部插件需求，再单独提升必要契约的可见性；持久化数据仍只保存稳定的 `ExecutionKind`。

### 决策五：构造时立即校验数据与运行时对象一致

角色创建必须满足以下不变量：

| 角色状态 | 要求 |
|----------|------|
| 所有角色 | 必须恰好具有一个非空 `IChatRoomRoleExecutor` |
| `IsHuman == true` | `ExecutionKind` 必须为 `Standard`，执行器也必须声明 `Standard` |
| `ExecutionKind == Standard` | 必须装配 `StandardChatRoomRoleExecutor` 语义的执行器 |
| `ExecutionKind == Coding` | `IsHuman` 必须为 `false`，并装配声明为 `Coding` 的执行器 |
| 定义与执行器 | `Definition.ExecutionKind` 必须等于执行器声明的 `ExecutionKind` |
| 未知枚举值 | 创建角色时立即失败，不静默回退到 Standard |

这些约束应在构造或工厂提交角色前验证，而不是等到首次发言后才失败。

这使以下错误能尽早暴露：

- 持久化定义是 Coding，但注册表遗漏 Coding 执行器工厂
- Standard 定义被错误装配为 Coding 执行器
- Coding 执行器创建 `CodingAgent` 失败
- 人类角色被错误标记为 Coding
- 新增枚举值后遗漏执行器工厂注册项

为了守住该不变量，只接收 `ChatRoomRoleDefinition`、却无法接收执行器的公共构造函数不能继续接受任意执行种类。推荐将实际运行时构造函数收敛为 `internal`，统一由 `ChatRoomRoleFactory` 创建；若仍保留便捷公共构造函数，则必须明确限定为 Standard，并在收到其他 `ExecutionKind` 时立即失败，不能静默创建错误执行器。

## SpeakAsync 推荐委托结构

### 分流时机

运行引擎分流发生在角色创建阶段：

```text
ChatRoomRoleFactory.CreateRole
  → 校验 ExecutionKind 是已定义值
  → 从执行器工厂注册表按 ExecutionKind 查找唯一工厂
      Standard 工厂 → StandardChatRoomRoleExecutor
      Coding 工厂   → CodingChatRoomRoleExecutor(CodingAgent)
  → 校验 Definition 与 Executor 一致
  → 创建 ChatRoomRole
```

`SpeakAsync` 只完成每种运行引擎都需要的通用准备，再委托给 `_executor`：


```text
SpeakAsync
  → 校验输入
  → ThrowIfDisposed
  → EnsureModelAvailable
  → 将输入转换为有序 AIContent
  → 构建 ChatRoomRoleExecutionContext
  → _executor.RunAsync
  → 包装完成任务，统一更新角色状态
  → 映射为 ChatRoomSpeakResult
```

因此 `SpeakAsync` 不包含 `switch (ExecutionKind)`，也不包含 `if (_codingAgent != null)`。新增执行引擎时不修改该方法，从结构上避免角色种类增长导致分支和字段持续膨胀。

不建议先创建一套通用手动发送循环再在执行器内部复用。那会继续让 ChatRoom 持有本应属于具体执行引擎的工具、提示词和取消编排。

### Standard 执行器

`StandardChatRoomRoleExecutor` 应回到 `CopilotChatManager.SendMessage` / `SendMessageRequest` 的标准发送路径：

- 使用当前 `CopilotChatManager.SelectedSession`
- `WithHistory = true`
- 首次发言时提供聊天室上下文、角色身份、角色自定义提示词和记忆
- 将 ChatRoom 本轮协调工具直接作为 `SendMessageRequest.Tools`
- 由 `CopilotChatManager` 负责流式消息、工具运行时绑定、审批、取消和会话历史
- 将 `SendMessageResult.AssistantChatMessage` 和完成任务映射为统一执行结果

这样可以删除 ChatRoom 为普通角色复制的手动发送循环，并自动恢复 `HumanApprovalTool` 的正确运行时绑定。

迁移时还必须保留 ChatRoom 的失败可观察性。当前 `CopilotChatManager.SendMessage` 会把非取消异常转换为失败 `SendMessageRunState` 并向私有会话追加错误消息，因此 Standard 执行器不能把任何失败都映射成普通空回复。它应根据 `RunTask` 的 `IsSuccess` / `WasCanceled` 明确区分成功、取消和失败；非取消失败需转换为异常或等价的失败结果，使 `ChatRoomManager.StepAsync` 仍能触发 `OnRoleSpeakFailed` 和聊天室系统错误消息。

### Coding 执行器

编程角色流程为：

```text
ChatRoomRole.SpeakAsync
  → CodingChatRoomRoleExecutor.RunAsync
      → 从执行上下文中的 CopilotChatManager 创建 IManualSendMessageContext
      → 不创建新 CopilotChatSession
      → 不清空当前 AgentSession
      → 不传入 ChatRoom 系统提示词或本轮协调工具
      → 调用本执行器独占的 CodingAgent.RunAsync
      → 将 CodingAgentRunResult 映射为统一执行结果
```

关键点：

1. `IManualSendMessageContext` 必须来自本角色一直使用的同一个 `CopilotChatManager`
2. `CodingAgent` 使用该 Context 中的当前 `AgentSession`
3. Coding 执行器不能创建自己的 ChatRoom 会话，也不能调用 `CreateNewSession`
4. 工作路径变化只替换编程工具，不影响 `SelectedSession` 和 AgentSession
5. 统一执行结果和最终 `ChatRoomSpeakResult` 必须直接引用 `CodingAgentRunResult` 中同一个助手消息对象，不能复制文本生成新对象，否则 UI 会失去流式绑定
6. 最终任务可以由 ChatRoom 包装一次，用于更新 `_hasSpoken`、`LastUsageDetails` 和空回复语义，但不能重新执行或等待后再返回消息对象
7. 非取消异常继续向 `ChatRoomManager.StepAsync` 传播，由现有失败事件和系统消息逻辑统一处理
8. `Definition.SystemPrompt`、角色身份、角色记忆、聊天室协作指令、角色管理工具和 `set_workspace_path` 均不进入 Coding 模型运行

`ChatRoomRole` 只负责创建统一执行上下文，不创建 Manual Context，也不自行填充 `UserChatMessage`、追加可见会话消息、调用 `StartChatting`、创建 `ChatClientAgent` 或运行流式循环。Coding 专用适配由 `CodingChatRoomRoleExecutor` 完成，完整手动发送编排由 `CodingAgent` 完成。

### 推荐的概念调用关系

`CodingAgent` 的公共运行入口保持用户已确定的最小参数面：

```text
RunAsync(
    IManualSendMessageContext context,
    string prompt,
    string? workspacePath,
    CancellationToken cancellationToken)

RunAsync(
    IManualSendMessageContext context,
    IReadOnlyList<AIContent> contents,
    string? workspacePath,
    CancellationToken cancellationToken)
```

`CodingChatRoomRoleExecutor` 应优先调用 `IReadOnlyList<AIContent>` 重载。当前聊天室公开消息仍是文本时，`ChatRoomRole` 把增量消息按原顺序转换为 `TextContent`；未来聊天室支持图片等附件时，可以原样保留多模态顺序，不需要再次修改执行器契约。

`CodingAgentRunResult` 建议保持最小且与现有 ChatRoom 流式模型可直接映射：

```text
CodingAgentRunResult
  - CopilotChatMessage AssistantChatMessage
  - Task<string?> CompletionTask
```

`CompletionTask` 表示完整运行生命周期，而不只是模型文本结束：它必须在 AgentSession 历史补全、Token 用量更新、聊天状态恢复、工作区 Lease 释放和内部取消源释放之后才完成。调用方必须观察该任务；`CodingAgent` 不应启动无法追踪的后台运行。

每个 Coding 类型的角色默认获得一个独立的 `CodingChatRoomRoleExecutor`，该执行器独占一个 `CodingAgent`。`ChatRoomRole` 只拥有并释放统一执行器，不直接知道或释放 `CodingAgent`。若未来共享底层 Roslyn 资源，应在 `AgentLib.Coding` 内部通过共享 Session/Lease 实现，不能通过多个角色共享同一个带可变工作路径状态的 `CodingAgent` 实例。

执行器所有权规则必须统一：`ChatRoomRole` 是 `_executor` 的唯一所有者，并在 `DisposeAsync` 中只调用一次执行器的异步释放。`CodingChatRoomRoleExecutor.DisposeAsync` 再转发给其 `CodingAgent`。释放开始后应拒绝新运行和新工作区切换；已经取得的工作区 Lease 继续保活底层资源，直到对应运行的完成任务在 `finally` 中释放 Lease。这样角色层不需要知道某个具体 Agent 是否仍有运行引用。

### 发言启动与工作区切换的线性化边界

允许“发言期间切换工作区”不等于运行启动和 Session 发布可以完全无序。Coding 执行器必须提供一个很短的线性化边界，仅保护以下启动阶段：

```text
读取执行器已提交的工作路径
  → 确认或发布对应 CodingWorkspaceToolSession
  → 获取该 Session 的 Lease
  → 创建 CodingAgentRunResult，并把 Lease 释放责任转移给 CompletionTask
  → 退出短临界区
```

`CodingChatRoomRoleExecutor.SetWorkspacePathAsync` 与 `RunAsync` 的上述启动阶段必须使用同一个内部生命周期门，或由 `CodingAgent` / `CodingWorkspaceToolProvider` 提供等价的原子能力。工作区切换中耗时的候选 Session 创建发生在门外；只有候选发布、`ChatManager.WorkspacePath` 提交和运行 Lease 获取进入短临界区。要求如下：

1. `RunAsync` 返回 `CodingAgentRunResult` 前，工作区 Lease 必须已经取得，不能把获取 Lease 延迟到无法观察的后台任务
2. 一旦结果已经返回，生命周期门立即释放；模型流、工具调用和 `CompletionTask` 不持有该门
3. 工作区切换最多等待正在发生的“运行启动/Lease 获取”，不能等待整次发言完成
4. 先完成启动的运行使用旧 Session Lease；先完成切换的新运行使用新 Session Lease
5. 迟到的旧路径快照不能重新发布旧 Session，也不能把 Provider 从新工作区切回旧工作区
6. 外层工作区切换与 Coding 运行启动交错时不得形成互相等待；Coding 模型和编程工具本身不发起 ChatRoom 工作区切换

不得用覆盖整次 `SpeakAsync`、模型流或工具执行的角色级 `SemaphoreSlim`、异步锁或队列实现该边界。同步只属于 Coding 工作区状态发布与 Lease 获取的短生命周期，不恢复已被明确移除的 `ChatRoomRole` 全操作门。

## 提示词职责边界

### CodingAgent 拥有编程基线提示词

以下内容从 `CodingAssistantRoleFactory` 和 `ChatRoomRole` 移入 `AgentLib.Coding`：

- 探索工作区后再修改
- 修改前读取文件
- 优先使用代码理解工具
- 最小修改原则
- 构建和测试验证
- Git 变更审查
- 失败和取消处理
- 最终完成条件

ChatRoom 不再保存或拼接这套通用编程工作流，避免同一规则出现两份并逐渐分叉。

### ChatRoom 仍拥有聊天室语义

以下内容仍属于 ChatRoom：

- 当前角色名称和 RoleId
- 当前聊天室的角色列表
- `@角色名` 协作规则
- 管理者和 MentionOnly 参与语义
- 角色自定义补充指令
- 角色记忆

这些语义继续由 Standard 执行器在调用 `CopilotChatManager.SendMessage` 时作为 `SystemPrompt` 传入。Coding 执行器不向 `CodingAgent` 传入这些内容；选择 `ExecutionKind = Coding` 表示该角色使用固定的编程代理工作流，而不是把聊天室人设叠加到编程代理之上。

`CodingAgent` 的模型输入按固定顺序组合：

```text
AgentLib.Coding 编程基线指令
  + 本轮用户 AIContent
```

不得把编程提示词重新塞回 `ChatRoomRoleDefinition.SystemPrompt`，也不得通过搜索提示词文本判断执行分支。`IManualSendMessageContext` 保持现有职责和 API，不增加 `HostInstructions`、`HostTools` 或等价配置。

## ChatRoom 工具与人工审批接入

### CodingAgent 不接收 ChatRoom 专用工具

`CodingAgent` 不应引用：

- `ChatRoomManager`
- `ChatRoomRole`
- `ChatRoomRoleManagementTools`
- `WorkspacePathTools`

因此不应为了 ChatRoom 增加 `additionalTools`、`hostTools` 或等价参数。依赖方向必须保持：

```text
AgentLib.ChatRoom → AgentLib.Coding → AgentLib
```

不能出现 `AgentLib.Coding → AgentLib.ChatRoom`。

### 本轮协调工具只进入 Standard 标准发送路径

`ChatRoomAutoLoopRunner` 仍可创建本轮协调工具：

- 角色管理工具
- `set_workspace_path`

Standard 执行器把这些工具直接放入 `SendMessageRequest.Tools`，由 `CopilotChatManager.SendMessage` 与默认工具合并。Coding 执行器忽略统一执行上下文中的本轮额外工具，`CodingAgent` 也不读取 `IManualSendMessageContext.DefaultTools`；它会用当前工作区租约中的完整编程工具列表替换 `ChatClientAgentOptions.ChatOptions.Tools`。

因此 Coding 工具不存在跨来源优先级：一次 Coding 运行只使用该次 Lease 固定的工具集合。文件读写、代码理解、CLI 等编程能力都必须由 `AgentLib.Coding` 的工作区 Session/Lease 自己提供。

### 人工审批边界

Standard 执行器继续使用 `CopilotChatManager.SendMessage`，因此 `set_workspace_path` 等配置型审批工具会沿用现有运行时绑定，不需要新增 API。

Coding 执行器不接收 `set_workspace_path` 或其他 ChatRoom 工具，工作区租约中的编程工具也不依赖 ChatRoom 的 `HumanApprovalTool` 配置。它可以使用现有 `IManualSendMessageContext.GetChatClientAgentAsync` 配置回调直接替换工具列表，无需请求 Manual Context 绑定宿主工具。

本轮明确不修改 `AgentLib`、`IManualSendMessageContext`、`HumanApprovalTool` 或内部 `CopilotChatContext`。Coding 路径没有 ChatRoom 审批展示语义；需要切换工作区时，由外层 `ChatRoomManager.SetWorkspacePathAsync` 或 UI 显式调用完成。

## 工作区切换与工具租约

### 目标行为

迁移后必须继续满足现有行为：

- 角色发言期间允许切换工作区
- 切换操作不等待发言完成
- 已开始的发言继续使用其开始时获得的稳定工具快照
- 新发言使用新工作区工具
- 旧 Roslyn Session 只在没有任何运行引用后释放
- 工作区切换不清空或新建 AgentSession

工作区更新仍由 `ChatRoomManager.SetWorkspacePathAsync` 统一发起。迁移后的角色转发关系应为：

```text
ChatRoomManager.SetWorkspacePathAsync
  → ChatRoomRole.SetWorkspacePathAsync
      → _executor.SetWorkspacePathAsync(ChatManager, workspacePath)
          → StandardChatRoomRoleExecutor：更新 ChatManager.WorkspacePath
          → CodingChatRoomRoleExecutor：一致提交 Coding 工作区 Session 与 ChatManager.WorkspacePath
```

`ChatRoomRole` 不知道当前执行器内部是 Coding、Writing 还是其他 Agent，也不再自己决定 ChatManager 路径与专用运行时的提交顺序。这里的 `CodingAgent` 工作区更新能力只负责工具 Session，不接触 `CopilotChatManager.SelectedSession` 或 `AgentSession`；`CodingChatRoomRoleExecutor` 在自己的短生命周期边界内协调该能力与 `ChatManager.WorkspacePath` 的发布。无论 `CodingAgent` 采用辅助生命周期 API 还是内部 Provider，`ChatRoomRole` 都不能重新持有 `CodingAgent`、Roslyn/CLI Provider 或工具适配器。

管理器现有的串行切换和失败回滚语义继续保留：某个工作区感知执行器创建新工作区候选失败时，管理器不提交新路径，并通过同一角色转发链恢复此前已更新角色。租约只解决旧 Session 的保活，不替代管理器的跨角色一致性事务。

`IChatRoomRoleExecutor.SetWorkspacePathAsync` 必须以“全成功或保持旧状态”的方式提交本角色的工作区状态。Coding 执行器应先在未发布状态创建候选 Coding Session，再进入短临界区同时发布候选 Session 和 `CopilotChatManager.WorkspacePath`；若候选创建或提交失败，两者都保持旧路径。管理器随后仍按既有事务逻辑回滚先前已更新的其他角色。

### 推荐租约模型

`CodingWorkspaceToolProvider` 不再只暴露一个可变的 `AITools` 属性供调用方复制，而是提供工作区工具租约：

```text
AcquireLeaseAsync
  → CodingWorkspaceToolLease
      - WorkspacePath
      - IReadOnlyList<AITool> Tools
      - DisposeAsync / Dispose
```

每个 `CodingWorkspaceToolSession` 至少持有：

- Provider 当前引用
- 正在运行的租约引用计数
- 是否已经从当前 Session 退役
- 最终异步释放任务

切换流程：

```text
创建候选 Session
  → 原子发布为当前 Session
  → 旧 Session 标记为 Retired
  → 释放 Provider 对旧 Session 的当前引用
  → 若运行引用为 0，立即异步释放
  → 否则由最后一个 Lease 释放时触发最终释放
```

CodingAgent 每次运行：

```text
同步目标工作路径
  → 获取当前 Session 的 Lease
  → 使用 Lease.Tools 完成整个 Agent 运行
  → 正常、取消或异常时在 finally 中释放 Lease
```

不得在模型流开始后再次读取 Provider 的实时 `AITools` 属性。否则同一次运行可能混用两个工作区。

仅复制 `IReadOnlyList<AITool>` 不足以满足租约语义。租约必须保活工具闭包所依赖的 Roslyn、CLI、文件工具或其他工作区资源。

### 编程工具必须完整纳入租约

CodingAgent 不使用 Manual Context 的默认工具，因此不会把引用可变 `WorkspaceToolProvider` 的默认文件工具混入一次 Coding 运行。

所有依赖工作路径的编程能力都必须由 `CodingWorkspaceToolSession` 创建并纳入同一个不可变 Lease，包括文件读取、文件修改、代码理解和 CLI 工具。不能只保护 Roslyn 进程，却让同一编程发言中的文件操作读取其他可变 Provider 的实时路径。

## 持久化、模板和角色编辑

### 持久化规则

角色定义只持久化 `ExecutionKind`，不持久化：

- `IChatRoomRoleExecutor` 或具体执行器类型
- `CodingAgent` 实例
- 工具列表
- 工作区工具租约
- Roslyn Session 或进程信息
- 当前工作路径
- 运行中任务或取消源

历史恢复流程：

```text
读取 ChatRoomRoleDefinition
  → 读取 ExecutionKind
  → ChatRoomRoleFactory 从注册表查找对应 IChatRoomRoleExecutorFactory
  → 执行器工厂创建对应 IChatRoomRoleExecutor
  → Coding 时创建新的 CodingChatRoomRoleExecutor 和其独占 CodingAgent
  → 将唯一执行器注入 ChatRoomRole
  → 恢复原 Copilot AgentSession
```

恢复流程不得序列化或反射具体执行器类型。`ExecutionKind` 是稳定数据契约，具体类名是当前版本的实现细节；未来重命名执行器不需要迁移会话 JSON。

### 模板规则

编程助手模板设置：

```text
ExecutionKind = Coding
```

模板名称、描述、分类和标签只用于展示，不参与运行分流。

`RoleTemplateService.ToDefinition`、`FromDefinition` 和 `UpdateFromDefinition` 必须完整复制 `ExecutionKind`。

### 角色编辑规则

当前 Avalonia `RoleEditViewModel.SaveAsync` 通过“删除旧角色，再创建新定义”保存编辑，且没有复制现有 `Kind`、`IsManagerRole` 等字段。按当前实现，编辑编程角色后可能退化为普通角色，并丢失原 AgentSession。

迁移时必须修复该路径：

- `ExecutionKind` 不作为普通名称/提示词编辑项随意改变
- 编辑现有角色时优先原位更新可编辑字段
- 若确实需要重建角色，必须从原定义完整复制不可编辑元数据，包括 `ExecutionKind`
- 重建时还要明确 AgentSession 的保留或迁移，不能静默清空历史
- 动态 `create_character` 默认显式创建 `Standard`
- 只有专门的编程角色创建入口或编程模板创建 `Coding`

## 类型去留建议

### 保留或新增

| 类型 | 处理 |
|------|------|
| `ChatRoomRole` | 保留单一运行时角色类型，只持有并委托一个 `IChatRoomRoleExecutor`，不按具体执行种类保存字段 |
| `ChatRoomRoleDefinition` | 保留，增加或重命名为明确的 `ExecutionKind` |
| `ChatRoomRoleFactory` | 保留为定义到运行时对象的组合边界，只持有执行器工厂注册表，按 `ExecutionKind` 创建并注入唯一执行器 |
| `IChatRoomRoleExecutor` | 在 `AgentLib.ChatRoom` 内部新增，统一表达发言与异步释放 |
| `IChatRoomRoleExecutorFactory` | 在 `AgentLib.ChatRoom` 内部新增，统一表达一种 ExecutionKind 的执行器创建逻辑 |
| `ChatRoomRoleExecutorCreationContext` | 在 `AgentLib.ChatRoom` 内部新增，携带执行器创建所需的 ChatRoom 基础依赖 |
| `ChatRoomRoleExecutionContext` | 在 `AgentLib.ChatRoom` 内部新增，携带 ChatManager 以及仅供 Standard 使用的首次系统提示词和本轮额外工具；不携带外层预读的工作路径快照 |
| `ChatRoomRoleExecutionResult` | 在 `AgentLib.ChatRoom` 内部新增，统一承载流式消息和完整运行任务 |
| `StandardChatRoomRoleExecutor` | 在 `AgentLib.ChatRoom` 内部新增，封装 `CopilotChatManager.SendMessage` 标准路径 |
| `CodingChatRoomRoleExecutor` | 在 `AgentLib.ChatRoom` 内部新增，封装并独占 `CodingAgent`，完成 ChatRoom 到 Coding 的适配 |
| `CodingAgent` | 在 `AgentLib.Coding` 新增，承担编程 Agent 编排 |
| `CodingAgentRunResult` | 在 `AgentLib.Coding` 新增，提供流式消息和完成任务 |
| `CodingWorkspaceToolProvider` | 保留并改造成租约安全的工作区资源提供器 |
| `CodingWorkspaceToolSession` | 保留在 Coding 内部，增加引用计数/退役状态 |
| `WorkspacePathTools` | 保留 ChatRoom 协调工具，仅通过 Standard 的 `SendMessageRequest.Tools` 进入模型运行并沿用现有审批绑定 |
| `ChatRoomRoleManagementTools` | 保留 ChatRoom 领域工具，仅通过 Standard 的 `SendMessageRequest.Tools` 传入 |

### 删除或收敛

| 类型或成员 | 处理 |
|------------|------|
| `CodingWorkspaceRoleToolAdapter` | 删除，ChatRoom 不再适配和拥有具体编程工具 |
| `IChatRoomRoleTool` | 若迁移后没有其他真实用途则删除 |
| `IChatRoomWorkspaceAwareTool` | 删除；工作区切换直接委托统一执行器 |
| `ChatRoomRole._roleTools` / `WorkspaceTools` | 删除编程运行时用途 |
| `ChatRoomRole.MergeTools` | 删除；CodingAgent 直接使用单一 Lease 工具集合，普通工具合并使用 CopilotChatManager |
| `ChatRoomRole.RunManualSendAsync` | 删除 |
| `ChatRoomRole` 中任何 `_codingAgent` / `_writingAgent` 等专用字段 | 不新增；具体 Agent 只能存在于对应执行器实现内部 |
| `CodingAssistantRoleFactory` 中的编程 SystemPrompt | 删除并迁入 AgentLib.Coding |
| `CodingAssistantRoleFactory` 的工具 Provider 创建职责 | 删除；该类型可收敛为模板提供者，或并入预置模板服务 |

是否删除 `CodingAssistantRoleFactory` 本身取决于它迁移后是否还承担独立的模板创建职责。如果只剩固定模板数据，建议改名为 `CodingAssistantTemplateProvider` 或直接并入现有模板定义，避免继续使用 `Factory` 暗示运行时编排。

## 与既有设计文档的关系

### 继续有效

以下结论继续有效：

- 运行时工具不进入角色定义、模板或会话 JSON
- 工作区资源采用候选 Session 先建后换
- 初始化失败保留旧工作区资源
- 外部进程必须异步释放且重复释放安全
- 发言期间允许切换工作区
- 工具调用不能访问已释放 Session
- 输出中尽量使用相对路径，避免不必要的本地绝对路径泄露

### 被本方案覆盖

`ChatRoom-共享工作区工具架构设计.md` 中以下结论与本轮明确需求冲突，实施时以本文为准：

- 删除 `ChatRoomRoleKind`
- 不识别编程角色
- 所有 AI 角色采用同一运行时执行路径
- 编程助手只是一份提示词模板
- `ChatRoomRoleFactory` 不再按角色运行策略创建运行时

原因是本轮已经明确引入独立的 `CodingAgent` 编排入口，并要求 Coding 角色使用不同于 Standard 的运行引擎。只要存在两个不同执行引擎，系统就必须保留稳定且可持久化的执行判据；但该判据由工厂解释为统一执行器，不能演变为 `ChatRoomRole` 内的一组专用 Agent 字段或不断增长的 `SpeakAsync` 分支。

这不等于用 `ExecutionKind` 承担工具授权。它只选择运行引擎；未来若需要工具权限，应单独设计授权模型。

## 建议实施顺序

1. 在 `AgentLib.Coding` 完成 `CodingAgent`、`CodingAgentRunResult` 和两组输入重载
2. 为 `CodingWorkspaceToolProvider` 与 Session 增加租约/引用计数，并把完整编程工具纳入租约
3. 使用现有 `IManualSendMessageContext` 完成编程提示词、运行取消和历史补全；不修改 AgentLib
4. 将角色判据重命名为 `ChatRoomRoleExecutionKind` / `ExecutionKind`
5. 在 `AgentLib.ChatRoom` 内部新增统一执行器、执行器工厂、执行上下文和执行结果契约
6. 实现 Standard 执行器及其工厂，使用 `CopilotChatManager.SendMessage` 显式传入 ChatRoom 提示词和本轮工具
7. 实现 Coding 执行器及其工厂，封装 `CodingAgent` 并只适配 Manual Context、用户内容和工作路径
8. 修改 `ChatRoomRoleFactory`，使用执行器工厂注册表按 ExecutionKind 创建唯一执行器并校验不变量
9. 修改 `ChatRoomRole.SpeakAsync`，只构建统一上下文并委托执行器
10. 将工作区切换与释放委托统一执行器，删除手动发送循环、工具合并和编程工具适配器
11. 修复角色模板复制、会话恢复和角色编辑对 ExecutionKind 的保留
12. 增加执行器/工厂映射、Standard 审批、Coding 工具隔离、租约、切换、恢复和扩展性测试
13. 最后审查公共 API 文档、依赖方向、资源释放、路径脱敏和 Git 变更范围，确认 AgentLib 无改动

## 测试矩阵

### 角色判据

1. 每个角色只持有一个非空 `IChatRoomRoleExecutor`
2. Standard 定义创建声明为 Standard 的执行器
3. Coding 定义创建声明为 Coding 的 `CodingChatRoomRoleExecutor`
4. `CodingAgent` 只存在于 Coding 执行器内部，`ChatRoomRole` 不暴露专用字段
5. 定义与执行器声明的 ExecutionKind 不一致时创建立即失败
6. Human + Coding 组合被拒绝
7. 未知 ExecutionKind 被拒绝，不回退
8. 修改角色名称、提示词、模型和模板标签不会改变执行器选择
9. 缺少注册项、重复注册或工厂返回错误 ExecutionKind 时创建立即失败
10. 增加一个测试用执行种类时，只需增加执行器、执行器工厂和注册项，`ChatRoomRole`、`ChatRoomRoleFactory` 的专用字段及 `SpeakAsync` 不变

### 持久化与编辑

1. ExecutionKind 在模板转换中完整往返
2. ExecutionKind 在会话 JSON 中完整往返
3. 会话 JSON 不包含具体执行器类名或 CodingAgent 状态
4. 加载历史 Coding 角色后重新创建 Coding 执行器及其 CodingAgent
5. 历史恢复后继续使用原 AgentSession 上下文
6. 编辑 Coding 角色后仍为 Coding
7. 编辑角色不静默清空 AgentSession
8. 动态 `create_character` 创建 Standard

### 执行器委托

1. `SpeakAsync` 不读取 ExecutionKind，也不检查具体执行器类型
2. `SpeakAsync` 对所有 AI 角色只调用统一执行器契约
3. Standard 执行器不调用 CodingAgent，并使用 `CopilotChatManager.SendMessage`
4. Coding 执行器调用 CodingAgent 的 `IReadOnlyList<AIContent>` 重载
5. Coding 执行器把原始 AIContent 顺序完整传递
6. 两个执行器都立即返回可绑定的助手消息
7. `ChatRoomSpeakResult` 与底层运行结果引用同一个 `CopilotChatMessage`
8. 最终任务未完成时流式消息仍持续更新
9. 空回复和取消保持现有 `null` 语义
10. 非取消异常继续触发 `OnRoleSpeakFailed`

### 会话连续性

1. Coding 执行器使用角色现有 CopilotChatManager
2. 第一次运行后第二次运行继续同一个 AgentSession
3. 工作区切换前后 AgentSession 引用不被替换
4. 清空工作区只移除工具，不清空对话历史
5. 序列化并恢复 Coding 角色后仍包含历史用户和助手消息

### 工具与审批

1. Standard 执行器把本轮协调工具交给 `SendMessageRequest.Tools`
2. `set_workspace_path` 在 Standard 执行路径中会显示审批项并等待
3. 拒绝审批时不会切换工作区
4. 同意审批后只执行一次切换
5. 取消审批等待会终止当前 Standard 运行且不执行内部工具
6. Coding 执行器不向 CodingAgent 传入 ChatRoom 本轮工具
7. CodingAgent 不读取 `IManualSendMessageContext.DefaultTools`
8. CodingAgent 一次运行只使用工作区 Lease 中的完整编程工具集合
9. Coding 路径中不存在 `set_workspace_path` 审批项，工作区切换只能由外层显式发起

### 工作区租约

1. 发言期间切换工作区不等待发言完成
2. 旧运行在切换后仍可调用旧 Session 工具
3. 新运行获得新工作区工具
4. 旧 Session 在最后一个 Lease 释放前不会 Dispose
5. 最后一个 Lease 释放后旧 Roslyn 进程只释放一次
6. 连续多次切换不会让较旧候选覆盖较新 Session
7. 候选创建失败或取消时保留旧 Session
8. 运行取消、异常和正常完成都会释放 Lease
9. Provider 重复 Dispose 保持幂等
10. 默认文件工具在一次运行内不会悄悄切换到新工作区
11. 运行启动与工作区切换交错时，迟到的旧路径不会把 Provider 切回旧 Session
12. 工作区切换只可能等待 Lease 获取的短启动阶段，不等待模型流或最终完成任务
13. 外层工作区切换与 Coding 运行启动交错时不会因执行器生命周期门形成死锁

### 关闭行为

1. 正常关闭通过统一执行器等待运行结束并释放资源
2. 模型流忽略取消时，聊天室仍按现有超时策略返回
3. 超时返回后，运行结束时由 Coding 执行器再释放其 CodingAgent 和旧 Session
4. 关闭后拒绝新的发言和工作区切换
5. 不遗留 Roslyn Language Server 进程
6. 移除正在发言的 Coding 角色时，已获得的 Lease 仍保活旧 Session，最终完成任务结束后再完成资源释放
7. `ChatRoomRole.DisposeAsync` 对统一执行器只释放一次，具体 Agent 的幂等释放由执行器负责

## 验收标准

1. 编程角色的唯一分流判据是可持久化的 ExecutionKind
2. ExecutionKind 只由 `ChatRoomRoleFactory` 解释为统一执行器，`SpeakAsync` 不再进行具体种类分支
3. `ChatRoomRole` 只持有一个 `IChatRoomRoleExecutor`，不存在 `_codingAgent`、`_writingAgent` 等专用字段
4. `ChatRoomRoleFactory` 只持有统一执行器工厂注册表，不存在 Coding、Writing 等专用工厂字段
5. CodingAgent 由 `CodingChatRoomRoleExecutor` 独占，不在 `SpeakAsync` 临时创建
6. 数据判据、执行器工厂和执行器声明在角色创建时完成一致性校验
7. Standard 和 Coding 两个执行器都复用角色原有 CopilotChatManager 会话
8. 工作区切换不创建或清空 AgentSession
9. ChatRoom 不再包含编程提示词、编程工具合并或手动流式循环
10. Standard 路径中的 `set_workspace_path` 沿用现有人工审批；Coding 路径不接收该工具
11. 发言期间切换工作区不等待，但旧工具资源不会提前释放
12. 角色编辑、模板复制和历史恢复不会丢失执行种类
13. 新增写作等执行引擎时不需要给 `ChatRoomRole` 或 `ChatRoomRoleFactory` 增加专用字段，也不修改 `SpeakAsync`
14. `AgentLib.Coding` 不引用任何 AgentLib.ChatRoom 类型
15. `AgentLib` 与 `IManualSendMessageContext` 没有因本方案发生修改
16. 公共 API 具有完整 XML 文档，内部执行器契约的异常、可空性和资源所有权清晰
17. 最终 Git 变更不包含无关重构、生成物、本地绝对路径或遗留未使用旧类型

## 结论

`ChatRoomRole.SpeakAsync` 不应“猜”哪个角色是编程角色，也不应为 Coding、Writing 等每种 Agent 保存一个字段。推荐使用可持久化的 `ExecutionKind` 作为唯一主判据，由 `ChatRoomRoleFactory` 在角色创建时通过执行器工厂注册表映射为一个统一的 `IChatRoomRoleExecutor`。`SpeakAsync` 完成通用校验和上下文构建后只调用该执行器，不再包含具体执行种类分支。

Standard 执行器复用 `CopilotChatManager.SendMessage`，并在发送请求中显式携带聊天室提示词和本轮协调工具；Coding 执行器基于同一个 `CopilotChatManager` 创建现有 `IManualSendMessageContext`，但只把用户内容和已提交工作路径交给其内部独占的 `CodingAgent`。CodingAgent 只使用自身工作区 Lease 中的编程工具，不接收 ChatRoom 提示词、默认工具或额外工具。本轮无需修改 AgentLib。未来增加写作等执行引擎时，只增加新的执行器、执行器工厂和注册项，`ChatRoomRole` 与 `ChatRoomRoleFactory` 都不增加专用字段，角色的状态布局、发言方法、工作区通用委托和释放框架保持不变。

这种三层契约同时解决了恢复、扩展和运行问题：角色定义回答“应该使用哪个执行引擎”，工厂回答“当前版本用哪个执行器实现该引擎”，统一执行器回答“如何执行、切换工作区和释放”。工具是否存在、提示词写了什么、模板从哪里创建，都不再承担类型判断职责。
