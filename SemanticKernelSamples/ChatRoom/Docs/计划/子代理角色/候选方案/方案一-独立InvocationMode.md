# ChatRoom 子代理角色候选方案一：独立 InvocationMode

共同产品约束见[需求功能](../ChatRoom-子代理角色需求功能.md)，方案比较见[候选方案汇总](ChatRoom-子代理角色候选方案汇总.md)。本文档保留推荐方案的完整实施设计，实施时应按当前主题分段读取，避免一次加载全文。

## 文档状态

- 状态：已按评审意见修订，待实施
- 交付类型：角色模型、Mention 元数据、调度、Standard 工具、持久化与 Avalonia UI 的改造方案
- 适用范围：`SemanticKernelSamples/ChatRoom/ChatRoom.slnx`
- 当前生产入口：legacy `ChatRoomManager`
- 并行演进入口：`Domain` + `ChatRoomCoordinator` + `Runtime`
- 数据策略：产品尚未发布，持久化结构直接升级，不增加旧 schema、旧模板或旧消息兼容分支

## 1. 背景

当前聊天室只区分以下角色维度：

- 人类角色与非人类角色
- `AlwaysParticipate` 与 `MentionOnly`
- 管理者与非管理者
- Standard 与 Coding 执行引擎

这些维度仍不能表达“角色平时不自动参与，但可以被任意非 preset 消息在开头 @，或被 Standard AI 通过工具同步调用”的触发语义。

本方案保留独立 `InvocationMode`，但不把子代理建设成第二套 Agent。子代理仍是普通 `ChatRoomRole`，继续使用现有 `StepAsync`、执行器、`CopilotChatManager`、`AgentSession`、checkpoint、流式消息和 UI。差异集中在两处：

- 调度器根据 Mention 的位置和来源消息是否为 preset 决定是否触发子代理。
- 子代理公开输出设置 `IsPresetInfo = true`，可见但不进入其他角色的后续上下文。

## 2. 需求定义

### 2.1 核心需求

1. 角色可以被定义为普通聊天室参与者或子代理。
2. 子代理不能进入普通自动发言队列。
3. 继续使用现有 Mention 解析逻辑和语法，不增加独立的用户子代理解析器。
4. Mention 结果必须结构化记录来源消息、匹配位置和是否位于消息开头。
5. 任意非 preset 消息中，只有位于索引 0 的子代理 Mention 才触发目标角色；消息中段 Mention 不触发子代理，不区分消息发送者是人类还是 AI。
6. Standard AI 既可以在公开消息开头 Mention 子代理以触发后续角色执行，也可以在需要同步获得结果并于当前轮继续推理时使用聊天室提供的 AITool。
7. Mention 与 AI 工具最终都调用现有 `StepAsync` / 普通角色 runtime，不增加子代理专用执行入口。
8. 子代理复用并保存现有 `AgentSession`、checkpoint 和角色上下文，不创建临时会话。
9. Standard 子代理被触发后必须通过 `ReturnOutputToCaller` 提交完成结果；第一次未提交时提醒一次，第二次仍未提交则失败。Coding 子代理沿用现有 Coding/AgentLib 完成结果，并将其视为等价提交。
10. 子代理原始输出按普通角色消息写入聊天室并正常展示，同时设置 `IsPresetInfo = true`。
11. AI 调用时，Standard 返回工具值或 Coding 现有完成结果作为 AITool 结果交给父 AI；父 AI 自行消化后继续生成公开回复。
12. 普通角色后续上下文与 Mention 调度跳过子代理的 preset 输出。
13. AgentLib 现有 `AgentLib.Tools.SubAgentToolProvider` 完全不改；ChatRoom 新建自己的工具机制。
14. 当前只改 Standard 执行引擎的通用逻辑，Coding 执行链路保持现状。
15. 不增加自调用、环路、嵌套深度、父子取消树或跨字段角色不变量等专用保护。

### 2.2 Mention 触发示例

假设存在名为“代码审查子代理”的子代理角色。

下表对 Human 与 Assistant 来源消息采用相同判断，不再区分发送者类型。

| 输入 | 是否触发子代理 | 原因 |
|------|----------------|------|
| `@代码审查子代理 检查当前改动` | 是 | 现有 Mention 匹配成功，且起始索引为 0 |
| `Xxxx。@代码审查子代理 检查当前改动` | 否 | 不在消息开头 |
| `Xxxx。 @代码审查子代理 检查当前改动` | 否 | 不在消息开头 |
| ` @代码审查子代理 检查当前改动` | 否 | 存在前导空白 |
| `@代码审查子代理	检查当前改动` | 按现有规则 | 不为子代理收紧 Mention 空白语法 |
| `@[代码审查子代理] 检查当前改动` | 按现有规则 | 保留现有方括号 Mention 语法 |
| `@普通助手 检查当前改动` | 否 | 目标不是子代理，继续按普通角色规则调度 |

是否触发由调度器判断；解析器只报告“匹配了谁、匹配发生在哪条消息的哪个位置”。

### 2.3 非目标

本轮不包含以下能力：

- 让子代理自动参与聊天室讨论。
- 通过角色名称、提示词、模板名称或工具集合推断子代理身份。
- 把子代理类别合并到 `ParticipationMode` 或 `ExecutionKind`。
- 新建严格的用户子代理消息协议。
- 新建 `IChatRoomSubAgentInvoker`、`IChatRoomSubAgentRuntime` 或临时运行上下文。
- 修改或复用 AgentLib 中现有的 `SubAgentToolProvider`。
- 修改 Coding 执行引擎或 `AgentLib.Coding` 的工具接口。
- 为子代理增加环路、深度、自调用等嵌套限制。
- 兼容读取当前开发阶段的旧持久化格式。

## 3. 当前实现事实

### 3.1 两套聊天室架构同时存在

当前应用主要使用：

```text
ChatRoomService
  → ChatRoomManager
      → ChatRoomAutoLoopRunner
          → ChatRoomRole
              → StandardChatRoomRoleExecutor / CodingChatRoomRoleExecutor
```

同时，项目中已经存在目标架构：

```text
ChatRoomCoordinator
  → ChatRoomState
  → ChatRoomRoleRuntimeRegistry
  → IChatRoomRoleRuntime
  → IsolatedChatRoomRoleRuntime
```

方案必须同时定义两条链路的行为，避免 legacy 与新 Coordinator 对同一角色定义产生不同解释。

### 3.2 普通 mention 语义与子代理需求冲突

legacy `MentionParser` 会扫描整条消息中的 `@角色名`，不区分发送者是人类还是 AI。新 Coordinator 的私有解析器语义更宽松，使用 `IndexOf("@" + RoleName)` 搜索整条消息。

现有问题不是 Mention 语法不足，而是解析结果只有 RoleId，调度器拿不到匹配位置和来源消息。因此应保留现有语法并把解析结果结构化，再由调度器区分：

- 普通角色 Mention：保持现有行为。
- 任意非 preset 消息开头的子代理 Mention：允许触发子代理。
- 任意消息中段的子代理 Mention：保留解析信息，但不加入发言队列。
- preset 消息中的 Mention：不加入发言队列。

### 3.3 AgentLib 子代理工具不是本方案的复用目标

`AgentLib.Tools.SubAgentToolProvider` 服务于 AgentLib 自身的子智能体机制，其运行模型和 ChatRoom 角色语义不同。无论当前实现是否存在 collector、重试或显示细节，本方案都不修改、不抽取也不复用该类型。

ChatRoom 在 `AgentLib.ChatRoom` 内新增 `ChatRoomSubAgentToolProvider`，面向已有角色列表、`StepAsync`、公开消息和 `IsPresetInfo` 语义。使用独立类型名避免与 `AgentLib.Tools.SubAgentToolProvider` 产生引用歧义。

### 3.4 现有角色执行链已经满足大部分需求

legacy `StepAsync` 已经完成：

- 构建角色增量输入。
- 通过 `ChatRoomRole.SpeakAsync` 复用角色自己的会话。
- 创建流式 `ChatRoomMessage` 并立即加入 `Session.Messages`。
- 完成后持久化角色 AgentSession。
- 让 Avalonia 使用普通角色消息模板展示输出。

因此子代理不需要专用 invoker。真正需要补充的是调用来源信息、Standard 返回工具，以及让 `StepAsync` 可按本次调用附加任务、工具和 preset 标记。

### 3.5 `IsPresetInfo` 不是自动上下文过滤器

`IsPresetInfo` 当前只存在于 `CopilotChatMessage`，主要用于：

- UI 和日志保留提示、错误、取消等消息。
- 排除自动标题和部分消息计数。
- 标记不应被当作普通用户对话的显示内容。

聊天室公开消息模型没有该字段。普通角色输入来自 `ChatRoomMessage` 和角色私有 AgentSession，所以只给底层 `CopilotChatMessage` 设置 `IsPresetInfo = true` 不能完成聊天室上下文隔离。

本方案必须把 `IsPresetInfo` 提升到聊天室消息语义，并在上下文构造、Mention 调度和持久化中真正执行过滤。它不改变消息的 UI 类型，子代理输出仍是普通 Assistant 角色消息。

## 4. 术语

### 4.1 普通参与者

按现有 `ParticipationMode`、普通 Mention、管理者兜底或显式 `StepAsync` 参与聊天室的角色。

### 4.2 子代理角色

`InvocationMode == SubAgent` 的普通聊天室角色。它仍拥有长期角色实例、模型、人设、技能、执行引擎、AgentSession、checkpoint 和普通消息展示；调度器只允许任意非 preset 消息开头的 Mention 或 Standard AITool 触发它。

### 4.3 调用者

发起子代理调用的一方：

- 写出消息开头 Mention 的 Human 或 Assistant 角色
- 通过 AITool 同步调用的 Standard AI 角色，包括 Standard 子代理角色

### 4.4 结构化 Mention

现有 Mention 语法的解析结果。除目标 RoleId 外，还记录来源 MessageId、起始位置、匹配长度和 `IsAtMessageStart`，供调度器决定是否触发子代理。

### 4.5 子代理调用

一次通过现有角色执行链完成的任务。Mention 路径由任意非 preset 消息开头的结构化 Mention 触发，AITool 路径由 Standard AI 同步调用；两者最终都在 `StepAsync` 中执行目标角色。

### 4.6 预置信息消息

与普通角色消息使用相同数据与 UI 模板、可以显示和持久化，但不进入其他角色后续模型输入，也不参与 Mention 调度。子代理输出使用 `IsPresetInfo = true` 标识。

## 5. 核心设计决策

### 决策一：保留独立角色调用模式

角色定义新增：

```text
ChatRoomRoleInvocationMode
  - Participant
  - SubAgent
```

`InvocationMode` 只回答“该角色按普通聊天室规则触发，还是按子代理规则触发”。它不切换角色实现，也不改变模型、人设、技能、`ExecutionKind`、AgentSession 或 UI。

- `ParticipationMode`：普通参与者何时进入自动队列。
- `ExecutionKind`：Standard 或 Coding 执行引擎。
- `IsManagerRole`：普通自动循环中的管理者身份。
- `InvocationMode`：调度器采用普通触发规则还是子代理触发规则。

除自动调度必须跳过 `SubAgent` 外，不为这些字段建立额外组合不变量。角色编辑器可以给出合理默认值，但不以防御性校验强制改写其他字段。

`InvocationMode` 是纯调度配置：仅修改该字段时不递增 `RuntimeVersion`、不替换角色 runtime、不删除 checkpoint，也不重置 `ConsumedThroughSequenceByRole`。只有模型、执行引擎、人设、技能、工作区等真正影响运行时的字段变化才沿用现有 runtime replacement 规则。

### 决策二：扩展现有 Mention 结果，不新增解析器

legacy 与 Coordinator 都统一返回类似以下的结构：

```text
ChatRoomMention
  - string TargetRoleId
  - string SourceMessageId
  - int StartIndex
  - int Length
  - bool IsAtMessageStart
```

要求：

1. 保持现有 `@角色名`、`@[角色名]`、大小写和空白匹配行为。
2. 按文本出现顺序返回，保留同一角色首次匹配的现有去重语义。
3. `IsAtMessageStart` 等价于 `StartIndex == 0`，不在解析前 Trim 输入。
4. `ChatRoomMessage` 保存结构化 Mention，而不是只保存无法追溯位置的 RoleId。
5. 如现有调用点只需要 RoleId，可从结构化结果投影，不再二次解析文本。

### 决策三：角色匹配与触发由调度器处理

调度规则如下：

| 来源消息 | 目标 InvocationMode | Mention 位置 | 调度行为 |
|----------|---------------------|--------------|----------|
| 任意非 preset 消息 | Participant | 任意有效位置 | 保持现有普通 Mention 行为 |
| 任意非 preset 消息 | SubAgent | 消息开头 | 通过现有 `StepAsync` 执行目标角色 |
| 任意非 preset 消息 | SubAgent | 非消息开头 | 不调度 |
| preset 消息 | 任意 | 任意位置 | 不调度 |

子代理仍然保留在结构化 Mention 结果中，过滤发生在“Mention 与角色匹配并入队”的阶段，而不是解析阶段。调度器不读取发送者类型，只检查来源消息是否为 preset 以及 `IsAtMessageStart`。

### 决策四：所有目标角色都走现有执行入口

不新增 `IChatRoomSubAgentInvoker`、`InvokeUserSubAgentCommand` 或子代理 runtime。legacy 最终调用：

```text
ChatRoomManager.StepAsync(targetRole, executionOptions, ct)
```

Coordinator 最终仍使用现有普通角色 execution/runtime 契约。为了传递调用差异，可以给现有执行请求增加少量通用选项，例如：

```text
ChatRoomRoleStepOptions
  - IReadOnlyList<string> AdditionalUserMessages
  - IReadOnlyList<AITool> AdditionalTools
  - bool MarkOutputAsPresetInfo
  - ChatRoomReturnOutputCollector? ReturnOutputCollector
```

普通角色和子代理共享同一条消息创建、流式更新、错误处理、会话保存和 UI 通知逻辑。

`MarkOutputAsPresetInfo` 是通用覆盖项；当目标角色 `InvocationMode == SubAgent` 时，即使调用方未显式设置，该次角色公开输出也必须默认标记为 preset。这样手动 `StepAsync` / `StartRoleExecutionCommand` 执行子代理时仍保持相同上下文可见性，而不需要禁止入口。

### 决策五：复用角色长期上下文

子代理按普通角色处理：

1. 使用聊天室中已有的 `ChatRoomRole` / runtime 实例。
2. 读取现有增量聊天室输入。
3. 复用角色自己的 `CopilotChatManager` 与 AgentSession。
4. 完成后保存 AgentSession 或提交正常 checkpoint。
5. 角色更新、工作区切换、压缩和清空记忆沿用现有角色机制。

不创建临时上下文，不清空聊天记录，也不跳过 checkpoint。

### 决策六：ChatRoom 自己实现 Standard 子代理工具

在 `AgentLib.ChatRoom.Tools` 中新增 `ChatRoomSubAgentToolProvider`。它提供调用工具，例如：

```text
InvokeChatRoomSubAgent(targetRoleId, prompt)
```

工具行为：

1. 从当前聊天室角色列表查找 `InvocationMode == SubAgent` 的目标。
2. 调用现有 `StepAsync`，把 `prompt` 作为本次附加 User 输入。
3. 若目标是 Standard，附加 `ReturnOutputToCaller`；若目标是 Coding，保持其现有工具和完成契约。
4. 将目标角色产生的 `ChatRoomMessage` 及其关联 `CopilotChatMessage` 标为 `IsPresetInfo = true`。
5. 将 Standard 提交值或 Coding 现有完成结果作为 AITool 返回值交给父 AI。

AgentLib 中已有的 `AgentLib.Tools.SubAgentToolProvider` 不做任何修改，也不抽取公共 runner 或 collector。

### 决策七：Standard 结果必须通过 `ReturnOutputToCaller` 提交

每次 Standard 子代理触发都创建一个仅服务于本次调用的轻量结果收集器，并将返回工具加入目标角色本轮 `AdditionalTools`；这同时适用于消息开头 Mention 和 AI AITool 两条路径。

协议规则：

1. Standard 子代理正常文本仍流式写入自己的普通聊天室消息，并在 UI 中展示。
2. 正式工具返回值只接受 `ReturnOutputToCaller(output)` 的参数。
3. `output` 不能为空白。
4. 第一次执行结束仍未提交时，在同一角色会话中追加一次明确提醒并再次执行。
5. 第二次仍未提交时，当前 Mention 调用或 AITool 调用明确失败。
6. 普通文本不作为隐式工具返回值。

通过消息开头 Mention 触发 Standard 子代理时也注入该工具并检查提交状态；返回值只表示本次任务已正式完成，不额外生成消息。AI 通过 AITool 调用时，同一个值还会作为工具结果交还父 AI。Coding 子代理不进入这套协议，直接采用现有 Coding/AgentLib 完成结果。

### 决策八：子代理输出是普通角色消息

无论由消息开头 Mention 还是 AI AITool 触发，目标角色产生的原始输出都：

- 使用普通 `ChatRoomMessage` / Assistant 消息类型。
- 使用现有角色头像、名称、模型信息、Token 信息和流式内容模板。
- 正常加入聊天室消息列表并持久化。
- 外层 `ChatRoomMessage` 与关联的 `CopilotChatMessage` 都设置 `IsPresetInfo = true`。
- 不根据返回工具值额外生成“最终结果”气泡，也不嵌入父角色的专用子代理卡片。

当本轮已有普通文本时保持模型原始文本，不用工具 output 覆盖；只有文本为空且已成功提交 output 时，才用 output 填充同一个普通消息。这样既保证工具提交是正式结果，也保证 Mention 直调不会得到空白 UI。

父 AI 同时得到 Standard 的 `ReturnOutputToCaller` 工具值或 Coding 的现有完成结果，可以在自己的同一轮模型运行中继续推理并输出普通公开回复。

### 决策九：仅 Standard 注入 ChatRoom 子代理工具

Standard 执行器已经把 `ChatRoomRoleExecutionContext.AdditionalTools` 传入 `SendMessageRequest.Tools`。为了让 AgentLib 子代理调用机制在 ChatRoom 中完全不参与，同时保留工作区等其他 AgentLib 默认工具，给 `SendMessageRequest` / `CopilotChatManager.ResolveTools` 增加通用排除项，例如：

```text
ExcludedDefaultToolNames = ["InvokeSubAgent"]
```

ChatRoom Standard 请求使用该选项排除 AgentLib 默认 `InvokeSubAgent`，再通过 `AdditionalTools` 加入 `ChatRoomRoleManagementTools`、`WorkspacePathTools`、`InvokeChatRoomSubAgent`，以及 Standard 子代理本轮的 `ReturnOutputToCaller`。

该通用接缝只按工具名过滤默认工具，不引用 ChatRoom 类型，也不修改 `AgentLib.Tools.SubAgentToolProvider`。最终工具列表仍按函数名检查重复并在配置错误时明确失败，不能静默覆盖。

Coding 执行器当前通过 `CodingAgent.RunAsync` 的独立接口运行，不消费这组 Standard 工具。本轮不改 `CodingChatRoomRoleExecutor`、`CodingAgent`、`CodingWorkspaceToolProvider` 或 AgentLib.Coding 测试。

Coding 子代理角色仍可按现有角色方式被任意非 preset 消息开头的 Mention 触发；若 Coding 内部已有 AgentLib 子代理机制，则继续保持其当前行为，与 ChatRoom 角色调用工具互不关联。

ChatRoom 的 `InvokeChatRoomSubAgent` 调用 Coding 子代理时同样走现有 `StepAsync` / runtime，但不注入 `ReturnOutputToCaller`。工具直接等待 Coding 执行器现有完成结果，并将该结果作为等价的 AITool 返回值交给父 AI。

### 决策十：不增加专用嵌套限制

Standard 子代理拿到与其他 Standard 角色相同的 `InvokeChatRoomSubAgent` 工具，因此可以继续调用其他子代理，也不额外禁止调用自身或形成环路。本方案不引入 `InvocationStack`、`Depth`、最大层数或子代理专用取消树。Coding 子代理内部是否调用其他代理继续由其现有 Coding/AgentLib 机制决定。

调用只沿用当前工具调用、`StepAsync` 和执行器已有的取消与异常行为，不增加过度防御逻辑。

## 6. 目标数据模型

### 6.1 角色定义

legacy 和 Domain 角色定义都增加：

```text
ChatRoomRoleInvocationMode InvocationMode
```

创建新角色时由 UI、模板或调用方明确给值。当前产品尚未发布，不依赖缺省值兼容旧 JSON。

### 6.2 结构化 Mention

新增共享语义模型，legacy 可使用可序列化 class，Domain 使用不可变 record：

```text
ChatRoomMention
  - string TargetRoleId
  - string SourceMessageId / Guid SourceMessageId
  - int StartIndex
  - int Length
  - bool IsAtMessageStart
```

`SourceMessageId` 使 Mention 与具体消息稳定关联；位置来自原始消息内容。`IsPresetInfo` 直接从来源消息读取，无需复制到 Mention 上；发送者类型不参与子代理 Mention 调度。

### 6.3 当前格式版本

legacy `room.config.json` 和单文件角色模板都增加显式 `FormatVersion`；Domain snapshot 继续使用并提升 `SchemaVersion`。加载时必须先验证版本，再反序列化/映射角色和消息。版本缺失、低于当前值或高于当前值都明确拒绝，不能依赖 enum/bool 的 CLR 默认值判断数据来源。

### 6.4 聊天室消息

legacy 和 Domain 消息增加：

```text
bool IsPresetInfo = false
IReadOnlyList<ChatRoomMention> Mentions
```

语义：

- `IsPresetInfo == true`：可以显示和持久化，但不进入其他角色模型输入，也不参与 Mention 调度。
- `Mentions`：替代只有 RoleId 的 `MentionedRoleIds`；调度器可从中投影 RoleId。

不新增“子代理消息角色”或专用消息 kind：

- 用户输入仍是 Human 消息。
- 子代理输出仍是 Assistant 消息。
- `IsPresetInfo` 只控制上下文与调度，不改变 UI 模板。

### 6.5 通用 Step 选项

为复用 `StepAsync`，建议增加通用选项而非子代理专用 invoker：

```text
ChatRoomRoleStepOptions
  - IReadOnlyList<string> AdditionalUserMessages
  - IReadOnlyList<AITool> AdditionalTools
  - bool MarkOutputAsPresetInfo
  - ChatRoomReturnOutputCollector? ReturnOutputCollector
```

collector 由触发方创建，并被 `ReturnOutputToCaller` 工具闭包捕获；外层完成包装器最多调用两次无重试 core，并在两次之间加入提醒。不需要 Caller、Target、InvocationId、用于限制调用的 `InvocationStack`、`Depth`、临时 Workspace 或专用结果类型。AI AITool 路径成功时返回 collector 中的字符串；Mention 路径只检查完成状态。失败沿用现有异常/取消通道。

### 6.6 不新增子代理瞬态 UI 事件

Coordinator 与 legacy 都继续发布普通角色消息和现有 execution 状态。子代理的流式内容通过普通消息更新进入 UI，不增加 `SubAgentInvocationStarted`、调用卡片或父消息内嵌事件。

## 7. 调用流程

### 7.1 消息开头 Mention 直接调用

```text
Human 输入或普通 Assistant 输出写入 ChatRoomMessage
  → MentionParser.ParseMentions(message, roles)
  → 保存结构化 Mentions
  → 现有自动循环读取最新消息
      → 普通角色 Mention 按原逻辑入队
      → 任意非 preset 消息中 IsAtMessageStart 的 SubAgent Mention 入队
      → 其他 SubAgent Mention 不入队
  → ChatRoomManager.StepAsync(
        targetSubAgent,
        AdditionalTools = [ReturnOutputToCaller],
        MarkOutputAsPresetInfo = true,
        ReturnOutputCollector = collector)
      → 正常创建并流式更新一条角色消息
      → 检查 ReturnOutputToCaller；未提交则提醒一次并继续当前逻辑调用
      → 保存目标角色 AgentSession
```

不增加 `SubmitHumanInputAsync`、独立 Mention 调用命令或按发送者分流的调度分支。Human 消息仍按现有发送流程提交，Assistant 消息仍按现有角色输出流程写入；调度器统一根据结构化 Mention 决定是否执行被点名的子代理。

### 7.2 普通 AI 角色调用

```text
Standard 角色模型调用 InvokeChatRoomSubAgent(targetRoleId, prompt)
  → 验证目标是 SubAgent
  → 创建本次 ReturnOutput collector
  → ChatRoomManager.StepAsync(
        targetRole,
        AdditionalUserMessages = [prompt],
        AdditionalTools = [ReturnOutputToCaller],
        MarkOutputAsPresetInfo = true,
        ReturnOutputCollector = collector)
      → 复用目标角色现有 AgentSession
      → 目标角色消息正常显示在聊天室
      → 目标角色调用 ReturnOutputToCaller
      → 保存目标角色 AgentSession
  → collector 的 output 作为 function result 返回父角色
  → 父角色继续生成普通公开回复
```

### 7.3 子代理调用另一个子代理

Standard 子代理与其他 Standard 角色获得相同的 `InvokeChatRoomSubAgent` 工具，可按同一流程调用另一个子代理。没有额外 stack、深度、自调用或环路检查。

子代理公开文本里的 `@目标子代理` 不会触发另一个子代理，因为子代理输出标记为 preset，整条消息不参与 Mention 调度。普通非 preset AI 角色若在消息开头 Mention 子代理，则会按统一规则触发目标角色。

### 7.4 Standard 子代理未调用返回工具

```text
第一次运行结束且 collector 无结果
  → 通过同一目标角色的现有会话追加“必须调用 ReturnOutputToCaller”提醒
  → 调用第二次无重试 StepAsyncCore，输出仍是普通 preset 角色消息
      → 有结果：Completed
      → 仍无结果：AITool 调用失败
```

普通文本不能作为隐式回退结果。

## 8. legacy `ChatRoomManager` 改造

### 8.1 保持现有人类输入入口

继续使用：

```text
ChatViewModel.SendAsync
  → ChatRoomService.HumanInterjectAsync
  → ChatRoomManager.HumanInterjectAsync
```

`HumanInterjectAsync` 创建消息后调用扩展后的 `MentionParser`，把结构化 Mention 写回消息。无需增加 `SubmitHumanInputAsync`、三态解析结果或 UI 发送分流。

### 8.2 保留原始输入位置

当前 `ChatViewModel.SendAsync` 会先执行 `InputText.Trim()`。为了准确判断 `StartIndex == 0`，应在 Mention 解析前保留原始输入，至少不能删除前导空白。

可以继续按既有产品规则处理尾随空白，但结构化 Mention 的位置必须基于实际写入聊天室的消息内容计算。这里不增加对子代理专属的 ASCII 空格、Tab 或方括号限制。

### 8.3 Mention 入队规则

`MentionParser` 解析所有角色；`PushMentionedRoles` 或其上游匹配方法根据来源消息决定是否入队：

- `Participant`：保持现有逻辑。
- `SubAgent` + `IsAtMessageStart`：入优先队列，不区分 Human 或 Assistant 来源。
- 其他 `SubAgent` Mention：不入队。
- `IsPresetInfo == true`：整条消息不参与 Mention 调度。

默认角色队列和管理者兜底只跳过 `InvocationMode == SubAgent`。不需要在 `TryDequeueNextSpeaker`、解析器、公开 `StepAsync` 等每层重复防御。

### 8.4 扩展而非禁止 `StepAsync`

保留现有公开重载，并增加可供自动循环和 AITool 使用的通用选项重载。子代理正是通过该入口运行，不得在 `StepAsync` 中拒绝 `InvocationMode == SubAgent`。

选项负责：

- 追加 AI 工具调用传入的任务文本。
- 合并本轮附加工具。
- 把本次创建的角色消息标记为 preset。
- 关联 `ReturnOutputToCaller` collector。

有效 preset 值为“选项显式要求”或“目标角色是 SubAgent”；不能依赖每个调用点都记得传标记。

原有流式消息创建、错误处理、`SaveRoleAgentSessionStateAsync` 和 `CurrentSpeaker` 生命周期不分叉。

由于 Standard AITool 可以在父角色 `StepAsync` 尚未结束时嵌套调用目标角色，`CurrentSpeaker` 不能再按单值直接覆盖后清空。实现可使用发言者栈，或在每次进入前保存 previous speaker、退出时恢复，确保子调用完成后 UI 回到父角色而不是错误显示为空。

### 8.5 上下文过滤与角色自身会话

`BuildIncrementalUserMessages` 必须跳过 `message.IsPresetInfo == true`，避免子代理输出进入其他角色的新输入。

但子代理本人的回复已经由其 `CopilotChatManager` 写入自身 AgentSession，后续调用自然可以延续。`ChatRoomSession` 仍可把该消息视为目标角色已发言并更新时间水位；其他角色跨过该时间点时只需在最终输入构造中跳过 preset 消息即可。

### 8.6 同一角色可重入执行

现有 `CopilotChatManager` 使用单值聊天状态和 CTS，不能直接并发重入。为支持不受限制的 A → A、A → B → A 等自然调用，增加通用的角色执行栈，而不是把自调用排到父工具之后造成死锁：

1. 外部同时到达的独立调用仍按 RoleId 串行，避免并发修改同一 AgentSession。
2. 工具调用形成的嵌套调用携带当前执行链上下文，可以在同一异步调用链内压入新的角色 turn。
3. 目标 RoleId 已经在执行链中时，复用该活动 `ChatRoomRole`、`CopilotChatManager` 和 AgentSession；父模型正等待工具结果，因此嵌套 turn 顺序执行而不是并发执行。
4. `CopilotChatManager` 的当前 CTS、聊天状态和消息发送上下文改为栈/计数语义：嵌套 turn 退出后恢复父 turn，不能覆盖或清空父调用状态。
5. 每个嵌套 turn 完成后先保存最新 AgentSession 状态，再把结果交还直接调用者；父 turn 恢复后继续在同一线性会话上运行。

这不是自调用限制，也不拒绝环路；它是普通角色和会话通用的可重入能力，不创建临时 AgentSession 或分支会话。

### 8.7 聊天室提示词

Standard 角色提示词区分：

```text
普通聊天室角色：可通过 @角色名 请求其接下来发言
子代理角色：在公开消息开头 @角色名可触发其后续执行；若当前轮需要同步获得结果并继续推理，应使用 InvokeChatRoomSubAgent 工具
```

Human 与 Assistant 消息都可以在开头使用现有 Mention 语法调用子代理。提示词列出可调用子代理的 RoleId、RoleName、简介和执行引擎，并说明 Mention 调用不会把结果作为当前模型轮次的 function result 返回。

### 8.8 `ChatRoomSubAgentToolProvider`

在 `AgentLib.ChatRoom.Tools` 新增：

```text
ChatRoomSubAgentToolProvider
ChatRoomReturnOutputCollector
```

Provider 直接持有或接收当前 `ChatRoomManager`，创建 `InvokeChatRoomSubAgent` AITool；调用时定位现有 `ChatRoomRole` 并调用 `StepAsync`。不创建 invoker、临时角色、临时 AgentSession 或公共 invocation runner。

### 8.9 Mention 直接调用的显示消息

触发 Mention 的来源消息仍保持原有 Human 或 Assistant 类型。子代理每次模型运行产生的 Assistant 消息都设置 `IsPresetInfo = true`，UI 与普通角色输出完全一致。

`ReturnOutputToCaller` 的值只用于确认完成；不因工具返回值追加专用结果消息，也不增加“子代理调用”卡片或视觉标识。协议提醒造成的再次模型运行仍按正常角色输出显示。失败继续走现有角色发言失败/系统消息机制。

## 9. 新 `ChatRoomCoordinator` 改造

### 9.1 保持现有消息命令

继续使用现有 `AppendHumanMessageCommand`、角色输出提交及自动循环命令。Human 或 Assistant 消息写入时都由统一 Mention 解析组件生成结构化 Mentions，Coordinator 不再维护语义不同的私有 RoleId-only 解析器。

不新增 `SubmitHumanInputCommand` 或 `InvokeUserSubAgentCommand`。

### 9.2 调度现有 execution

Coordinator 的自动队列按与 legacy 相同的表格判断结构化 Mention。任意非 preset 消息开头的子代理 Mention 可以进入队列；默认队列与管理者兜底不主动加入子代理。

`StartExecutionCore` 不因 `InvocationMode == SubAgent` 拒绝执行。顶层执行继续使用 `CurrentExecution`；为支持工具内调用，领域状态把单个 execution 扩展为通用 execution stack，`CurrentExecution` 作为栈顶便捷视图，不承担旧数据兼容职责。

### 9.3 通用可重入角色执行栈

Standard runtime 中的 `InvokeChatRoomSubAgent` AITool 通过 Coordinator 的通用“执行角色并等待完成”能力压入一个子 execution frame：

1. 父 frame 保留 role identity、runtime version、workspace version、输入水位和待恢复的工具调用 continuation。
2. 目标角色当前不在栈中时，子 frame 使用目标角色现有 runtime lease、`IChatRoomRoleRuntime.ExecuteAsync`、committed checkpoint 和 candidate 提交协议。
3. 目标 RoleId 已经在栈中时，子 frame 复用该活动 runtime context 和 AgentSession，不从旧 committed checkpoint 创建第二个分支。
4. 同一时刻只有栈顶 frame 推进模型；父模型任务在 AITool 中异步等待，不与子 turn 并发修改同一角色会话。
5. 每个子 turn 完成后提交普通 preset Assistant 消息，并提交该角色下一版连续 checkpoint；活动父 frame 随即更新自己的基准 SessionRevision，最终不能再提交基于旧 revision 的 candidate。
6. 将 Standard 返回工具值或 Coding 完成结果交给等待中的父工具，再弹栈恢复父 frame。
7. 子 frame 失败时把异常交还父 AITool，并恢复父 frame；父模型自行决定继续、重试或公开说明。

该能力是普通角色执行机制的可重入扩展，不命名为 SubAgent invoker/runtime。它允许任意自然嵌套，不增加最大深度、环路或自调用检查。

### 9.4 命令与状态模型

可新增通用内部请求，例如 `ExecuteRoleAndWaitAsync` 或同等命令/continuation，但名称和契约必须面向“角色执行”，不能成为子代理专用入口。`ChatRoomState` 在内存中暴露 execution stack；snapshot 仍只允许在没有活动 execution 时保存或恢复，因此不序列化半途调用 continuation。

停止、关闭、工作区切换、审批与流式事件都作用于栈顶 frame。整条可重入调用链复用房间现有执行取消作用域，栈展开时等待方自然收到取消；不另建子代理专用取消树或按深度遍历子调用。

### 9.5 可见输入与消费高水位

Domain `ChatRoomMessage` 增加 `IsPresetInfo` 后，角色输入规则为：

```text
InputMessages = 消费水位之后且 IsPresetInfo == false 的消息
InputThroughSequence = 执行开始时房间最新消息序号
```

因此 `ChatRoomRoleExecutionRequest` 应允许最后一条可见输入早于 `InputThroughSequence`：

1. 输入消息序号严格递增。
2. 每条输入消息序号不大于 `InputThroughSequence`。
3. preset 消息不进入 `InputMessages`，但消费水位可以跨过。
4. 本次 AITool 传入的任务使用 execution request 的附加输入表达，不伪装成公开非 preset 消息。

### 9.6 子代理正常提交 checkpoint

子代理与普通角色一样接收 committed checkpoint、产生 candidate checkpoint、更新消费水位并写入 `ChatRoomSnapshot.RoleCheckpoints`。`IsPresetInfo` 只过滤共享公开上下文，不清除目标角色自己的私有会话。

### 9.7 Runtime 接缝

继续使用 `IChatRoomRoleRuntime.ExecuteAsync`。如需承载工具调用任务和输出标记，只扩展通用 `ChatRoomRoleExecutionRequest` / candidate 字段，不增加 `IChatRoomSubAgentRuntime.InvokeAsync`。

`IsolatedChatRoomRoleRuntime` 仍负责所有角色的执行与 checkpoint 生命周期，子代理不获得独立 runtime 类型。

## 10. 工具设计

### 10.1 调用工具

ChatRoom 自有 Provider 建议暴露：

```text
InvokeChatRoomSubAgent
```

参数：

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `targetRoleId` | string | 是 | 子代理稳定 RoleId |
| `prompt` | string | 是 | 本次任务说明和期望输出 |

工具描述必须包含：

- 只能选择当前列出的子代理 RoleId。
- 子代理会延续自己的角色会话，并能获得现有增量聊天室上下文。
- AI 可以通过消息开头的普通 `@` 文本触发子代理；只有需要同步获得结果并在当前轮继续推理时才应调用本工具。
- 工具会同步等待并返回子代理提交结果。

不能使用 `InvokeSubAgent` 作为工具名，因为该名称已由 AgentLib 默认 `SubAgentToolProvider` 占用；ChatRoom 工具必须保持独立名称，避免函数注册冲突和模型误选。

### 10.2 子代理列表

普通角色系统提示词应列出可调用子代理：

```text
- RoleId
- RoleName
- 简短人设摘要
- ExecutionKind
```

不需要单独 `list_subagents`。每次 Standard 角色执行都基于当前房间角色快照生成工具描述和目标列表。

### 10.3 返回工具

建议工具名：

```text
ReturnOutputToCaller
```

参数：

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `output` | string | 是 | 返回给直接调用方的最终结果 |

该工具只在 Standard 子代理被执行的本轮工具集合中注入。消息开头 Mention 与 AI AITool 触发都使用它；普通参与者本轮没有完成回传需要时不获得该工具。

### 10.4 工具集合

Standard 子代理继续获得普通 Standard 角色已有的工具集合，并额外获得：

- `InvokeChatRoomSubAgent`：与其他 Standard 角色相同，可继续调用聊天室子代理。
- `ReturnOutputToCaller`：只在 Standard 角色本轮被作为子代理触发时加入。

不因为子代理身份移除角色管理工具、工作区工具或其他 ChatRoom 现有工具，也不建立额外权限白名单。AgentLib 默认 `InvokeSubAgent` 不属于 ChatRoom 工具集合。Coding 工具集合保持完全不变。

## 11. 持久化

### 11.1 legacy JSON

直接保存当前模型新增字段：

```text
Session.FormatVersion
Role.InvocationMode
Message.IsPresetInfo
Message.Mentions[]
```

必须更新：

- legacy 角色定义
- legacy 消息模型
- Mention 模型
- JSON 源生成上下文关联类型
- `ChatRoomPersistence.ValidateRoleDefinitions`
- 保存/加载测试

加载时先检查 `FormatVersion == CurrentFormatVersion`。版本字段缺失会得到无效值并立即拒绝；不为缺少这些字段的旧 `room.config.json` 提供默认恢复或兼容分支。

### 11.2 角色模板

必须更新：

- `RoleTemplate.FormatVersion`
- `RoleTemplateService.ToDefinition`
- `RoleTemplateService.FromDefinition`
- `RoleTemplateService.UpdateFromDefinition`
- 模板校验
- 模板测试

`RoleTemplateService.LoadAll` 只接受当前 `FormatVersion`。旧模板、缺少版本的模板或缺少 `InvocationMode` 的模板都不迁移、不补默认值；可按现有损坏模板策略跳过并记录诊断。

### 11.3 snapshot schema

更新当前 schema，使角色、消息和 Mention 直接按新结构序列化。

读取器只接受当前 schema。无需实现 schema 2 → 新 schema 的迁移，也无需为缺失字段猜测默认值；版本缺失或不等于当前值时明确拒绝。

### 11.4 子代理调用记录

用户输入按普通 Human 消息持久化。子代理原始输出按普通 Assistant 消息持久化，并保留 `IsPresetInfo = true`。AI 工具调用的 function call/result 继续存在于父角色自己的 AgentSession/checkpoint；目标角色的私有会话也按普通角色机制保存。

不保存 `SubAgentInvocationId`，也不需要恢复专用调用卡片或把 Mention 来源消息与角色输出关联成特殊记录。

execution stack 只描述运行中状态。沿用 Coordinator 当前约束：snapshot 仅在没有活动 execution 时作为可恢复状态；不序列化或恢复半途父子调用 continuation。

### 11.5 公开日志

当前 `public_logs/{sessionId}.txt` 是只读回看的纯文本日志，不是会话恢复权威，因此保持与其他 Assistant 消息相同的文本格式即可，不承诺在该文本日志中编码 preset 或 Mentions。`room.config.json` / snapshot 才负责结构化恢复 `IsPresetInfo` 与 Mentions；无需新增 `subagent_calls` 日志。

从结构化配置恢复 legacy Assistant 消息时，`RestoreCopilotChatMessage` 必须把外层 `ChatRoomMessage.IsPresetInfo` 传播到新建的 `CopilotChatMessage.IsPresetInfo`，保证恢复后的 UI 与上下文语义一致。

## 12. Avalonia UI 方案

### 12.1 角色编辑页

新增“角色调用方式”：

```text
聊天室参与者
子代理
```

选择子代理只保存 `InvocationMode`，不自动改写或禁用 `IsHuman`、`ParticipationMode`、`IsManagerRole`、`ExecutionKind` 等其他配置。界面可以显示说明：

```text
子代理不会被加入普通自动发言队列。
任意非 preset 消息可在开头 @该角色；Standard AI 需要同步获得结果时应使用 InvokeChatRoomSubAgent 工具。
角色仍保留自己的会话与记忆，输出正常显示，但不会进入其他角色的后续上下文。
```

### 12.2 角色列表

角色卡片增加类型标签：

- 人类
- 普通 AI
- 子代理
- 管理者

子代理沿用普通角色上下文菜单，包括“@ 提及角色”“编辑角色”“删除角色”“压缩对话”“清空记忆”等已有能力；其角色卡只需显示“子代理”类型标签。

点击“@ 提及角色”时，为满足触发规则，应把 Mention 插入到消息开头，而不是隐藏该操作或追加到消息尾部。

### 12.3 输入框插入

普通角色继续使用现有追加行为：

```text
已有文本 + ` @普通角色 `
```

子代理仍使用同一个 Mention 插入命令，只按角色调用模式选择插入位置：

```text
InputText = `@子代理角色名 ` + 当前任务正文
```

如果输入框已有正文：

```text
@子代理角色名 {原正文}
```

不得把子代理 Mention 追加到消息尾部，否则结构化结果的 `IsAtMessageStart` 为 false，调度器不会触发。

### 12.4 发送流程

`ChatViewModel.SendAsync` 保持单一发送流程：

- 保留前导空白后提交普通 Human 消息。
- 按现有逻辑启动自动循环。
- 自动循环依据结构化 Mention 选择普通角色或消息开头的子代理。

普通 Assistant 消息写入后使用相同的结构化 Mention 调度规则，不增加发送者类型判断。

### 12.5 消息展示

子代理角色输出与普通角色完全相同：同一个 `MessageItemViewModel`、同一套 Assistant 消息模板、同样的角色名称、头像、模型、Token 和流式内容。

不增加“子代理调用”视觉标识，不把同一份输出拆成“原始输出 + 工具结果”两段气泡，也不在父助手消息内创建 `CopilotChatSubAgentItem`。AI 工具调用时，目标角色每次真实模型输出都独立出现在聊天室正常时间线上，父角色完成后再显示自己的正常回复。

## 13. 错误、取消与生命周期

### 13.1 目标角色不存在或类型错误

`InvokeChatRoomSubAgent` 只验证 RoleId 存在且目标 `InvocationMode == SubAgent`。失败时由 AITool 抛出明确的参数/状态异常。

不额外建立“子代理必须非人类/非管理者/MentionOnly”之类跨字段不变量。若目标本身是人类角色，现有 `StepAsync` / runtime 会按普通规则不启动模型执行；工具把该正常执行结果反馈给调用者，而不是在角色模型层新增一套子代理校验。

这里的“无跨字段不变量”不等于把空执行当成功：`InvokeChatRoomSubAgent` 若最终没有获得 Standard 提交值或 Coding 完成结果，就按“目标没有可返回的执行结果”失败。人类 SubAgent 因不会启动模型，工具调用自然失败；Mention 调用仍按现有人类角色规则不产生 AI 回复。

### 13.2 模型不可用

Mention 与 AI 工具调用都沿用 `StepAsync` / runtime 的现有角色失败处理。AI 工具调用还会把失败作为工具异常返回父模型，由父模型决定是否解释或重试。

### 13.3 Standard 子代理未调用返回工具

第二次提醒后仍无结果：当前 Mention 调用或 AITool 调用明确失败。

不得使用最后一段普通文本作为结果。

### 13.4 取消

- 每次 `StepAsync` / runtime execution 继续使用其现有 cancellation token。
- 停止房间、关闭会话或取消当前执行沿用现有行为。
- 不建立额外父子取消树，也不为嵌套子代理增加专用传播规则。

### 13.5 角色更新和删除

- legacy 继续使用当前 `ChatRoomRole` 实例和现有角色增删改生命周期。
- Coordinator 继续使用既有 runtime lease、identity、runtime version 和 workspace version 规则。
- 只修改 `InvocationMode` 时保留当前 runtime、AgentSession/checkpoint 和消费水位；不得把纯调度变更当成运行时替换。
- 不增加 InvocationId 或子代理专用的删除、更新、迟到结果校验。

## 14. 需要修改的核心文件

### 14.1 明确不修改

- `AgentLib/Tools/SubAgentToolProvider.cs`
- `AgentLib/Model/CopilotChatSubAgentItem.cs`
- `AgentLib.Coding/CodingAgent.cs`
- `AgentLib.ChatRoom/CodingChatRoomRoleExecutor.cs`
- AgentLib 与 AgentLib.Coding 的现有子代理机制及测试

### 14.2 AgentLib.ChatRoom legacy / Standard

- `Model/ChatRoomRoleDefinition.cs`
- `Model/ChatRoomMessage.cs`
- `Model/ChatRoomSessionData.cs`
- `Model/RoleTemplate.cs`
- 新增 `Model/ChatRoomMention.cs`
- `MentionParser.cs`
- 新增 `Tools/ChatRoomSubAgentToolProvider.cs`
- 新增轻量 `ChatRoomReturnOutputCollector` 或同等内部类型
- `ChatRoomManager.cs`
- `ChatRoomManager.ChatRoomAutoLoopRunner.cs`
- `ChatRoomSession.cs`
- `ChatRoomRole.cs`
- `ChatRoomRoleExecutionContext.cs`
- `StandardChatRoomRoleExecutor.cs`
- `AgentLib/Model/SendMessages_/SendMessageRequest.cs` 与 `AgentLib/CopilotChatManager.cs`：增加通用默认工具排除项；不修改 `AgentLib/Tools/SubAgentToolProvider.cs`
- `ChatRoomPersistence.cs`
- `Services/ChatRoomService.cs`
- `Services/RoleTemplateService.cs`
- `Tools/ChatRoomRoleManagementTools.cs`
- `Services/CodingAssistantRoleFactory.cs`

### 14.3 Domain / Coordinator / Runtime

- `Domain/ChatRoomEnums.cs`
- `Domain/ChatRoomRoleDefinition.cs`
- `Domain/ChatRoomMessage.cs`
- 新增 `Domain/ChatRoomMention.cs`
- `Domain/ChatRoomSnapshot.cs`
- `Domain/ChatRoomState.cs`
- `Coordination/ChatRoomCommand.cs`
- `Coordination/ChatRoomChange.cs`
- `Coordination/ChatRoomCoordinator.cs`
- `Runtime/IChatRoomRoleRuntime.cs`
- `Runtime/IsolatedChatRoomRoleRuntime.cs`
- `Runtime/ChatRoomRoleRuntimeRegistry.cs`
- `Persistence/StoredChatRoomSnapshot.cs`
- `Persistence/ChatRoomSnapshotMapper.cs`

### 14.4 Avalonia

- `ViewModels/RoleEditViewModel.cs`
- `Views/RoleEditView.axaml`
- `ViewModels/RoleListViewModel.cs`
- `Views/RoleListView.axaml`
- `ViewModels/ChatViewModel.cs`
- `Views/ChatView.axaml`
- `ViewModels/RoleLobbyViewModel.cs`
- 相关 Shell 测试

## 15. 测试计划

### 15.1 结构化 Mention

1. 现有 `@角色名` 与 `@[角色名]` 语法继续匹配。
2. 结果包含 TargetRoleId、SourceMessageId、StartIndex、Length 和 IsAtMessageStart。
3. 消息开头匹配的 StartIndex 为 0。
4. 消息中段与前导空白场景保留准确位置。
5. Tab、换行、大小写和方括号行为与现有 Mention 测试保持一致。
6. 多 Mention 按出现顺序返回，同一角色保持现有去重语义。

### 15.2 Mention 与角色匹配

1. Human 消息开头的 SubAgent Mention 进入优先队列。
2. Assistant 消息开头的 SubAgent Mention 同样进入优先队列。
3. Human 与 Assistant 消息中段的 SubAgent Mention 被解析但不入队。
4. Participant Mention 保持现有调度行为。
5. preset 消息中的所有 Mention 都不调度，即使 Mention 位于消息开头。
6. 调度判断不读取消息发送者类型。
7. 默认队列和管理者兜底不会主动选择 SubAgent。

### 15.3 现有执行链复用

1. legacy `StepAsync` 可以执行 SubAgent。
2. 子代理流式消息正常加入 `Session.Messages`。
3. 子代理完成后保存现有 AgentSession 状态。
4. Coordinator 使用通用可重入 execution stack，并让每个 frame 走普通 runtime/candidate 流程。
5. 子代理正常接收并提交 checkpoint。
6. 附加任务、附加工具和 preset 输出标记能通过通用执行选项传递。
7. AI 工具调用期间父 execution frame 挂起，子 frame 完成后恢复父 frame。
8. ChatRoom Standard 最终工具列表保留其他 AgentLib 默认工具，但排除默认 `InvokeSubAgent`，且包含 `InvokeChatRoomSubAgent`。
9. A → A 与 A → B → A 不被拒绝，且不会并发重入同一 AgentSession、产生重复 SessionRevision 或 stale checkpoint。
10. 嵌套期间取消或关闭后，execution stack、活动 CTS、CurrentSpeaker、等待 collector 和 continuation 都被清理，不残留挂起任务。
11. execution stack 非空时 snapshot 保存/恢复入口明确拒绝；栈清空后可正常保存并恢复。

### 15.4 `ReturnOutputToCaller`

1. 消息开头 Mention 与 AI AITool 两条 Standard 路径都会注入返回工具。
2. 返回值等于 `ReturnOutputToCaller` 的 output 参数。
3. 普通文本不被当作正式结果。
4. 第一次未提交时在同一角色会话中提醒并重试一次。
5. 第二次未提交时当前调用明确失败。
6. 空白 output 被拒绝。
7. AI 调用成功后父角色在同一轮获得 function result 并继续回复。
8. 第二次执行调用无重试 core，不会触发第三次模型运行。
9. 仅调用 ReturnOutputToCaller 且无文本时，output 填入同一个普通 Assistant 消息；已有文本时不覆盖，也不新增消息。

### 15.5 会话与上下文隔离

1. 子代理第二次调用可以延续第一次调用的私有 AgentSession/checkpoint。
2. 子代理可以收到按现有规则构建的非 preset 增量聊天室输入。
3. 子代理输出的公开消息设置 `IsPresetInfo = true`。
4. 其他角色构建输入时跳过子代理 preset 消息。
5. preset 消息不参与 Mention 调度。
6. 目标子代理自己的私有会话仍保留其上一轮输出。

### 15.6 Standard AI 调用与嵌套

1. Standard 普通角色可以调用子代理。
2. Standard 子代理也可以获得 InvokeChatRoomSubAgent 工具。
3. 子代理 A 可以调用子代理 B。
4. 实现中不存在自调用、环路或最大深度的专用拒绝分支。
5. AI 普通文本在消息开头 `@子代理` 时触发目标角色，在消息中段时不触发。
6. 不存在或非 SubAgent 的 RoleId 被工具明确拒绝。
7. Coding 执行器和 AgentLib.Coding 相关测试保持不变。
8. Human + SubAgent 配置不增加保存校验，但 AI 工具调用因无模型完成结果而明确失败。

### 15.7 持久化

1. legacy 角色 InvocationMode 往返。
2. Message.IsPresetInfo 与结构化 Mentions 往返。
3. 模板三种转换都保留 InvocationMode。
4. 当前 snapshot schema 往返。
5. 子代理 checkpoint 正常保存和恢复。
6. 恢复后的子代理消息仍按普通 Assistant 消息展示并继续被上下文过滤。
7. legacy `FormatVersion` 缺失/旧值、模板 `FormatVersion` 缺失/旧值和旧 snapshot schema 都被明确拒绝，不执行兼容迁移。
8. 新格式缺少必填的 InvocationMode、IsPresetInfo 或 Mentions 时按无效数据拒绝，而不是使用 CLR 默认值。
9. legacy 恢复时外层 IsPresetInfo 会传播到重建的 CopilotChatMessage；纯文本 public log 不作为结构化恢复来源。
10. 仅修改 InvocationMode 后，runtime identity/version、AgentSession/checkpoint 和消费水位保持不变。

### 15.8 UI

1. 角色编辑页正确加载和保存子代理类别，不自动改写其他角色配置。
2. 子代理角色卡显示类型标签。
3. 子代理继续提供普通角色已有菜单。
4. 从角色列表插入子代理 Mention 时位于输入开头。
5. 发送前导空白消息时不会因 Trim 改变 Mention 起始位置。
6. 子代理消息使用与普通角色完全相同的消息模板。
7. Mention 直接调用和 AI 工具调用都只显示目标角色真实产生的普通输出，不根据工具结果增加重复气泡或调用卡片。
8. preset 子代理消息仍可见模型名、Token 和普通角色操作。
9. UI 修改 InvocationMode 后不会触发角色会话清空或 checkpoint 重建。

所有新增 MSTest 测试应设置硬超时；新测试按项目现有规范使用中文 `DisplayName`。

## 16. 分步实施计划

1. 在 legacy 与 Domain 角色定义中增加 `ChatRoomRoleInvocationMode`，不添加跨字段不变量。
2. 新增结构化 `ChatRoomMention`，让 legacy 与 Coordinator 共用现有 Mention 语法并保存匹配位置。
3. 在 legacy 与 Domain 消息中加入 `IsPresetInfo` 和结构化 Mentions，替换 RoleId-only 数据。
4. 改造 legacy 与 Coordinator 的 Mention 入队规则，只根据 preset 状态、消息开头和 InvocationMode 判断子代理触发，不区分发送者。
5. 扩展 `StepAsync` 与通用 runtime request，使其可接收附加输入、附加工具和 preset 输出标记。
6. 为 `SendMessageRequest` 增加通用默认工具排除项，让 ChatRoom Standard 排除 AgentLib `InvokeSubAgent`。
7. 在 ChatRoom 内新增 `ChatRoomSubAgentToolProvider`、`InvokeChatRoomSubAgent`、`ReturnOutputToCaller` 和轻量 collector。
8. 将 ChatRoom 子代理工具注入 Standard 执行链，并实现一次提醒后的失败协议。
9. 让任意非 preset 消息开头的 Standard 子代理 Mention 同样注入返回工具并检查完成结果。
10. 保持子代理 AgentSession/checkpoint 的正常保存恢复，并在其他角色输入中跳过 preset 消息。
11. 将 Coordinator 的单 execution 扩展为通用可重入 execution stack，每个 frame 继续复用现有 runtime/candidate/checkpoint 流程。
12. 为 legacy JSON 与角色模板增加 FormatVersion，更新当前 snapshot schema，并明确拒绝旧版本和缺失必填字段，不实现迁移。
13. 改造角色编辑、角色列表和 Mention 插入位置；消息模板保持不变。
14. 添加结构化 Mention、调度、StepAsync、工具回传、会话、持久化和 UI 测试。
15. 运行 AgentLib.ChatRoom、ChatRoom Shell 相关测试和完整构建；确认 Coding 与 AgentLib 现有测试无回归。
16. 更新 ChatRoom README，说明消息开头 Mention、Standard AITool、preset 可见性和长期会话语义。

## 17. 验收标准

1. 子代理角色不能进入默认参与者队列或管理者兜底；只有消息开头 Mention、AITool 或显式执行才能让其发言。
2. 现有 Mention 语法保持不变，解析结果包含来源消息和匹配位置。
3. 任意非 preset 消息开头的子代理 Mention 可以触发目标角色，消息中段或前导空白不会触发。
4. 子代理 Mention 调度不区分 Human 或 Assistant 发送者；preset 消息即使在开头 Mention 也不触发。
5. Mention 与 AI 工具调用最终都复用现有 `StepAsync` / 普通 runtime。
6. Standard 角色可以使用 ChatRoom 自有 `InvokeChatRoomSubAgent` 工具；AgentLib 现有 `InvokeSubAgent` Provider 不改且不出现在 ChatRoom Standard 工具列表中。
7. Standard 子代理必须通过 `ReturnOutputToCaller` 提交结果；第一次未提交提醒一次，第二次失败。
8. 子代理保留并恢复自己的 AgentSession/checkpoint，可延续旧调用上下文。
9. 子代理原始输出作为普通 Assistant 角色消息显示和持久化，并设置 `IsPresetInfo = true`。
10. UI 不增加第二段结果气泡、子代理卡片或特殊消息模板。
11. 其他角色后续输入和 Mention 调度跳过 preset 子代理消息。
12. AI 调用时，Standard 返回工具值或 Coding 现有完成结果作为 function result 交给父 AI，父 AI 继续生成公开回复。
13. 子代理不受自调用、环路、最大深度或角色字段组合等专用限制。
14. Coding 执行引擎、AgentLib.Coding 与 AgentLib 现有子代理机制保持不变。
15. 当前持久化格式直接升级，不读取旧 schema、旧模板或缺失字段数据。
16. legacy 与 Coordinator 对 InvocationMode、结构化 Mention、普通执行链和 preset 过滤保持一致；Coordinator 的嵌套调用通过通用 execution stack 完成。
17. 现有 MentionOnly、管理者、角色管理、持久化和 Coding 回归测试保持通过。

## 18. 最终建议

该功能不应实现为“第三种 ParticipationMode”，也不应另造一套子代理运行时。正确边界是：

```text
角色定义明确声明 InvocationMode
  + 现有 Mention 输出来源与位置元数据
  + 调度器判断任意非 preset 消息开头的子代理 Mention
  + Standard AI 可用开头 Mention 触发后续执行，或使用 ChatRoom 自有 AITool 同步获得结果
  + 所有目标角色复用现有 StepAsync/runtime 与长期会话
  + Standard 子代理通过 ReturnOutputToCaller 提交结果
  + 原始输出按普通角色消息展示并标记 preset
  + 上下文构造和 Mention 调度过滤 preset
```

这样可以把变化限制在触发、工具回传和上下文可见性上，最大程度复用普通多角色聊天室的角色、会话、消息、UI、持久化和 Coordinator 机制，同时避免无状态临时上下文、专用入口和过度防御导致的复杂度。
