# ChatRoom 子代理角色设计方案

## 文档状态

- 状态：方案设计，待实施
- 交付类型：跨领域模型、调度、运行时、工具、持久化与 Avalonia UI 的改造方案
- 适用范围：`SemanticKernelSamples/ChatRoom/ChatRoom.slnx`
- 当前生产入口：legacy `ChatRoomManager`
- 并行演进入口：`Domain` + `ChatRoomCoordinator` + `Runtime`
- 兼容策略：旧角色和旧消息默认保持普通聊天室语义；新 snapshot 必须兼容读取旧 schema

## 1. 背景

当前聊天室只区分以下角色维度：

- 人类角色与非人类角色
- `AlwaysParticipate` 与 `MentionOnly`
- 管理者与非管理者
- Standard 与 Coding 执行引擎

这些维度都不能准确表达“子代理角色”：

- `MentionOnly` 角色仍可被任意人类或 AI 消息中的普通 `@角色名` 触发。
- `ExecutionKind` 只决定运行引擎，不决定角色如何被调用。
- `IsManagerRole` 只决定自动循环的兜底和仲裁行为。
- 普通 `StepAsync` / `StartRoleExecutionCommand` 仍可以绕过 mention 调度直接执行任意非人类角色。

本方案引入显式的子代理角色语义。子代理角色不是聊天室中的普通发言参与者，而是一个只能通过专用调用协议执行的受控任务代理。

## 2. 需求定义

### 2.1 核心需求

1. 角色可以被定义为普通聊天室参与者或子代理。
2. 子代理不能进入普通自动发言队列。
3. 子代理不能被人类或非人类角色通过普通消息中的 `@角色名` 触发。
4. 非人类角色只能通过专用子代理工具调用子代理。
5. 用户可以直接调用子代理，但消息必须严格以 `@子代理角色名 ` 开头。
6. `@子代理角色名 ` 出现在消息中段时无效。
7. 子代理每次调用是独立任务，不读取整个聊天室历史，不复用上一次子代理调用的 AgentSession。
8. 子代理普通文本输出不能直接成为调用结果；必须通过返回工具显式提交给调用方。
9. 子代理输出不进入整个聊天室的后续上下文；用户直调产生的结果消息必须设置 `IsPresetInfo = true`。
10. AI 调用子代理时，结果只作为工具结果返回给调用者自己的模型运行，不写入聊天室公开消息。

### 2.2 严格触发示例

假设存在名为“代码审查子代理”的子代理角色。

| 输入 | 是否调用 | 原因 |
|------|----------|------|
| `@代码审查子代理 检查当前改动` | 是 | 严格位于消息开头，角色名后为 ASCII 空格，且任务非空 |
| `Xxxx。@代码审查子代理 检查当前改动` | 否 | 不在消息开头 |
| `Xxxx。 @代码审查子代理 检查当前改动` | 否 | 不在消息开头 |
| ` @代码审查子代理 检查当前改动` | 否 | 存在前导空白 |
| `@代码审查子代理	检查当前改动` | 否 | 分隔符不是 ASCII 空格 |
| `@代码审查子代理` | 否 | 缺少固定空格和任务内容 |
| `@[代码审查子代理] 检查当前改动` | 否 | 子代理首版不支持方括号语法 |
| `@普通助手 检查当前改动` | 否 | 目标不是子代理，继续按普通 mention 规则处理 |

### 2.3 非目标

本轮不包含以下能力：

- 并行运行多个用户直调子代理。
- 让子代理自动参与聊天室讨论。
- 让子代理通过公开发言把结果广播给全部角色。
- 每次调用保存和恢复子代理自己的 AgentSession。
- 通过角色名称、提示词、模板名称或工具集合推断子代理身份。
- 把子代理类别合并到 `ParticipationMode` 或 `ExecutionKind`。

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

因此不能通过在现有 mention 解析器中增加一个分支实现子代理。普通 mention 和用户直调子代理必须是两个独立协议。

### 3.3 当前子代理工具已经具备的目标语义

`AgentLib.Tools.SubAgentToolProvider` 已体现以下行为：

- 同步等待子代理完成。
- 每次调用不传 AgentSession，任务相互独立。
- 子代理可以使用受控工具。
- 子代理必须通过 `ReturnOutputToParent` 显式返回结果。
- 子代理内部文本、推理、工具和嵌套调用只作为调用过程展示。
- 父代理只取得最终工具返回值并自行整合。

聊天室子代理角色应复用这些语义，而不是另造一套“子角色公开发言”机制。

### 3.4 `IsPresetInfo` 不是自动上下文过滤器

`IsPresetInfo` 当前只存在于 `CopilotChatMessage`，主要用于：

- UI 和日志保留提示、错误、取消等消息。
- 排除自动标题和部分消息计数。
- 标记不应被当作普通用户对话的显示内容。

聊天室公开消息模型没有该字段。普通角色输入来自 `ChatRoomMessage` 和角色私有 AgentSession，所以只给底层 `CopilotChatMessage` 设置 `IsPresetInfo = true` 不能完成聊天室上下文隔离。

本方案必须把 `IsPresetInfo` 提升到聊天室消息语义，并在上下文构造、mention 解析、自动调度和持久化恢复中真正执行过滤。

## 4. 术语

### 4.1 普通参与者

可以通过普通自动循环、普通 mention 或显式公开发言入口参与聊天室的角色。

### 4.2 子代理角色

具有稳定角色定义、模型、人设、技能和执行引擎，但只能通过子代理调用协议执行的非人类角色。

### 4.3 调用者

发起子代理调用的一方：

- 人类用户
- 当前正在执行的普通 AI 角色
- 未来允许的上级子代理

### 4.4 子代理调用

一次自包含、无状态、有唯一 InvocationId 的任务执行。其最终结果必须由返回工具提交。

### 4.5 公开消息

聊天室全部参与者可能看到的普通消息。

### 4.6 预置信息消息

可以显示和持久化，但不进入普通角色后续模型输入、不参与 mention 调度的消息。使用 `IsPresetInfo = true` 标识。

## 5. 核心设计决策

### 决策一：新增独立的角色调用模式

建议新增：

```text
ChatRoomRoleInvocationMode
  - Participant
  - SubAgent
```

角色定义新增：

```text
InvocationMode
```

该字段只决定角色允许通过哪类入口执行，不改变运行引擎、模型、人设或工具配置。

选择独立字段的理由：

- `ParticipationMode` 决定普通聊天室参与时机。
- `ExecutionKind` 决定 Standard/Coding 运行引擎。
- `IsManagerRole` 决定调度仲裁身份。
- `InvocationMode` 决定普通公开发言还是子代理调用。

四者职责不能混合。

### 决策二：子代理使用防御性角色不变量

必须满足：

| 条件 | 要求 |
|------|------|
| 人类角色 | `InvocationMode == Participant` |
| 子代理 | `IsHuman == false` |
| 子代理 | `IsManagerRole == false` |
| 子代理 | `ParticipationMode == MentionOnly` |
| 子代理 | 不进入默认自动队列 |
| 子代理 | 不进入普通 mention 目标集合 |
| 子代理 | 不能通过普通公开 Step 执行 |
| 未知 InvocationMode | 创建或恢复时立即拒绝 |

对子代理强制保存 `MentionOnly` 不是把两种语义合并，而是防御性保证：即使某个旧调度入口遗漏新的 `InvocationMode` 检查，也不会把子代理加入默认 AlwaysParticipate 队列。

### 决策三：用户直调使用独立严格解析器

新增独立解析器，例如：

```text
UserSubAgentInvocationParser
```

它只接收：

- 原始、未 Trim 的用户输入
- 当前子代理角色定义快照

返回三态结果：

```text
NoMatch
Matched(targetRoleId, prompt)
Invalid(targetRoleId?, error)
```

解析规则：

1. 只检查字符串索引 0。
2. 只匹配 `@{RoleName} `。
3. 分隔符只接受 U+0020 ASCII 空格。
4. 不接受前导空白。
5. 不接受方括号格式。
6. 角色名按 `OrdinalIgnoreCase` 匹配，与现有 mention 用户体验保持一致。
7. 角色名后的任务正文 Trim 后必须非空。
8. 只在 `InvocationMode == SubAgent` 的角色集合中匹配。
9. 角色名仍保持唯一，禁止依赖列表顺序解决歧义。

该解析器不能替换 `MentionParser`。

### 决策四：普通 mention 解析显式排除子代理

legacy `MentionParser` 和新 Coordinator 的普通 mention 解析都只能把以下角色写入 `MentionedRoleIds`：

```text
InvocationMode == Participant
```

即使非人类角色输出以下内容，也不能触发子代理：

```text
@代码审查子代理 请检查
```

该文本仍可以作为普通公开文本显示，但不会成为结构化 mention，也不会进入优先发言队列。

### 决策五：子代理调用不复用普通发言入口

禁止以下方式执行子代理：

- `ChatRoomManager.StepAsync(subAgentRole)`
- 普通自动循环从默认队列选择子代理
- 普通 mention 优先栈选择子代理
- 管理者兜底选择子代理
- `StartRoleExecutionCommand` 对子代理启动普通角色执行

新增独立入口，例如：

```text
IChatRoomSubAgentInvoker.InvokeAsync(request)
```

所有调用路径统一进入该入口：

```text
AI 角色工具调用 ─┐
                 ├─→ IChatRoomSubAgentInvoker → 子代理运行时 → 返回工具 → 调用结果
用户严格前缀 ────┘
```

### 决策六：子代理每次调用使用临时会话

每次 invocation：

1. 根据持久化角色定义选择模型和运行引擎。
2. 创建临时角色运行上下文。
3. 不恢复 committed checkpoint。
4. 不读取聊天室公开消息历史。
5. 不读取该子代理上次调用产生的 AgentSession。
6. 只注入角色静态人设、静态 MemoryContent、调用任务和允许的工具。
7. 调用结束后释放临时 AgentSession 和工具租约。
8. 不保存子代理 checkpoint 或 legacy `agent-session-state.json`。

`MemoryContent` 被视为角色静态配置，而不是跨调用学习到的会话记忆，因此每次调用仍可注入。

### 决策七：最终结果必须通过返回工具提交

抽取可复用的 invocation 作用域：

```text
SubAgentInvocationScope
  - InvocationId
  - OutputCollector
  - CancellationToken
  - ProgressSink
  - InvocationStack
```

返回工具建议命名为：

```text
ReturnOutputToCaller
```

协议规则：

1. 子代理可以输出普通文本和推理作为进度，但这些内容不被当作最终结果。
2. 最终结果只能由 `ReturnOutputToCaller(output)` 提交。
3. `output` 必须非空白。
4. 第一次成功提交后即确定最终结果；重复提交应被拒绝。
5. 提交后应尽快终止本次子代理运行。
6. 第一次模型运行未提交结果时，追加一次强制提醒并重试。
7. 第二次仍未提交时，调用以协议失败结束，不能静默返回空字符串。
8. 取消、模型异常、工具异常和协议失败使用不同终态。

### 决策八：先修正并抽取当前子代理回传基础设施

当前 `SubAgentToolProvider` 每次 `CreateTools` 都会创建新的 executor。`InvokeSubAgentAsync` 所检查的 collector 与子代理实际 `ReturnOutputToParent` 工具所写入的 collector 可能不是同一个实例。

实施聊天室子代理前，应先：

1. 把 collector 从 executor 私有字段提升为 invocation scope。
2. 为同一次 invocation 创建的全部返回工具共享该 scope。
3. 增加“最终函数结果等于返回工具参数”的测试。
4. 把“第二次仍未返回”改为明确协议异常。
5. 将通用调用循环抽取为可被 ChatRoom 复用的 runner。

### 决策九：AI 调用结果只进入调用者私有运行上下文

普通 AI 角色调用子代理时：

- 子代理结果作为专用工具的 function result 返回给调用者。
- 调用者可以在同一轮继续推理并输出公开总结。
- 子代理原始结果不生成顶层 `ChatRoomMessage`。
- 其他角色只会看到调用者最终公开发布的普通文本。
- 子代理工具调用详情可以显示在调用者助手消息的 `CopilotChatSubAgentItem` 中。
- 子代理结果可以留在调用者自己的 AgentSession，符合当前子代理工具行为，但不进入整个聊天室共享上下文。

### 决策十：用户直调请求和结果都作为预置信息隔离

用户严格前缀调用时：

1. 输入作为可显示的调用记录追加，设置 `IsPresetInfo = true`。
2. 不写入普通 `MentionedRoleIds`。
3. 不启动普通自动循环。
4. 子代理完成后追加结果消息，设置 `IsPresetInfo = true`。
5. 结果消息发送者显示为目标子代理角色。
6. 普通角色构建上下文时跳过请求和结果。
7. 恢复会话后仍显示这些消息，但继续不进入普通上下文。

虽然原始需求只明确要求输出为 preset，同时隔离用户调用请求可以避免以下副作用：

- 普通角色在下次发言时读取并重复处理该任务。
- 没有普通 mention 后意外启动全部 AlwaysParticipate 角色。
- 会话恢复后重新解释旧调用文本。

### 决策十一：Standard 与 Coding 都通过受控 host tool 调用子代理

Standard 执行器当前可以接收本轮额外工具；CodingAgent 当前只使用编程工作区工具并忽略 ChatRoom 工具。

为了让普通非人类角色都具备子代理调用能力，建议：

1. `ChatRoomRoleExecutionContext` 增加受控 host tools。
2. Standard 执行器把 host tools 合并到 `SendMessageRequest.Tools`。
3. `CodingAgent.RunAsync` 增加通用 `IReadOnlyList<AITool>` host tools 参数。
4. Coding 运行工具集合为：

```text
工作区编程工具 + ChatRoom 子代理调用工具
```

5. `AgentLib.Coding` 只依赖 `AITool`，不引用 ChatRoom 类型，保持依赖方向。
6. host tool 名称冲突时立即失败，不允许覆盖文件、Roslyn 或 CLI 工具。
7. 不把角色管理工具、工作区切换工具等全部重新注入 Coding；本轮只开放子代理调用工具。

### 决策十二：子代理嵌套调用必须有环路保护

调用请求携带：

```text
InvocationStack = [callerRoleId, ..., targetRoleId]
Depth
```

规则：

- 目标角色已经出现在 stack 中时拒绝调用。
- 设置可配置的最大嵌套深度，建议默认 4。
- 子调用继承父调用取消令牌。
- 父调用失败或取消时，所有未完成子调用一起取消。
- 子代理不能通过普通 `@` 绕过 stack 检查。

## 6. 目标数据模型

### 6.1 角色定义

legacy 和 Domain 角色定义都增加：

```text
ChatRoomRoleInvocationMode InvocationMode = Participant
```

默认 `Participant` 保证旧 JSON 和旧模板恢复后行为不变。

### 6.2 聊天室消息

legacy 和 Domain 消息增加：

```text
bool IsPresetInfo = false
Guid? SubAgentInvocationId
```

语义：

- `IsPresetInfo == true`：可以显示和持久化，但不进入普通角色模型输入，不参与普通 mention 和自动调度。
- `SubAgentInvocationId`：关联用户直调请求和结果；普通消息为空。

不建议新增“子代理消息角色”替代 Human/Assistant：

- 用户调用请求仍可按 Human 气泡显示。
- 子代理结果仍可按 Assistant 气泡显示。
- 调用语义由 preset 和 invocation id 表达。

### 6.3 调用请求

建议新增不可变请求：

```text
ChatRoomSubAgentInvocationRequest
  - Guid InvocationId
  - string TargetRoleId
  - string Prompt
  - ChatRoomSubAgentCaller Caller
  - Guid? ParentExecutionId
  - IReadOnlyList<string> InvocationStack
  - int Depth
  - string? WorkspacePath
```

调用方：

```text
ChatRoomSubAgentCaller
  - Human(humanRoleId, humanRoleName)
  - Role(roleId)
  - SubAgent(roleId)
```

### 6.4 调用结果

```text
ChatRoomSubAgentInvocationResult
  - Guid InvocationId
  - string TargetRoleId
  - ChatRoomSubAgentInvocationOutcome Outcome
  - string? Output
  - string? FailureMessage
  - string? ModelDisplayName
```

终态：

```text
Completed
Canceled
ModelFailed
ToolFailed
ProtocolFailed
Rejected
```

### 6.5 瞬态执行事件

新 Coordinator 不应把子代理过程写入 `ChatRoomState.Messages`。建议增加独立事件：

```text
SubAgentInvocationStarted
SubAgentInvocationDelta
SubAgentInvocationToolChanged
SubAgentInvocationCompleted
SubAgentInvocationFailed
SubAgentInvocationCanceled
```

每个事件至少携带：

- RoomId
- RoomInstanceId
- ParentExecutionId（AI 调用时）
- InvocationId
- CallerRoleId
- TargetRoleId
- 单调事件序号

AI 调用时 UI 把事件投影到父消息的子代理卡片；用户直调时投影为独立调用卡片或 preset 消息。

## 7. 调用流程

### 7.1 用户直接调用

```text
ChatViewModel.SendAsync(rawInput)
  → ChatRoomService.SubmitHumanInputAsync(rawInput)
      → UserSubAgentInvocationParser.Parse(rawInput, roles)
          → NoMatch
              → 普通 HumanInterject
              → 按现有规则启动自动循环
          → Invalid
              → 返回输入错误，不追加普通消息
          → Matched
              → 追加 preset 人类调用记录
              → 排队/启动用户子代理 invocation
              → 子代理通过 ReturnOutputToCaller 提交结果
              → 追加 preset 子代理结果消息
              → 不启动普通自动循环
```

`SubmitHumanInputAsync` 返回结构化分流结果：

```text
HumanInputDispatchKind
  - ChatMessageAppended
  - SubAgentInvocationCompleted
  - SubAgentInvocationQueued
  - Rejected
```

UI 不再根据“发送成功”无条件启动自动循环。

### 7.2 普通 AI 角色调用

```text
角色模型调用 invoke_chatroom_subagent(targetRoleId, prompt)
  → 工具验证调用者是 Participant 非人类角色
  → 验证目标是 SubAgent
  → 验证 stack、深度、房间实例和取消状态
  → IChatRoomSubAgentInvoker.InvokeAsync
      → 获取目标 runtime lease
      → 创建临时 AgentSession
      → 注入目标人设 + prompt + 返回工具 + 授权工具
      → 等待 ReturnOutputToCaller
  → 结果作为 function result 返回父角色
  → 父角色继续生成普通公开回复
```

### 7.3 子代理调用另一个子代理

子代理内部可以获得同一个 `invoke_chatroom_subagent` 工具，但工具调用请求会继承 invocation stack 和 parent cancellation token。

子代理不能通过公开 `@目标角色` 调用另一个子代理。

### 7.4 子代理未调用返回工具

```text
第一次运行结束且 collector 无结果
  → 追加“必须调用 ReturnOutputToCaller”提醒
  → 第二次运行
      → 有结果：Completed
      → 仍无结果：ProtocolFailed
```

普通文本不能作为隐式回退结果。

## 8. legacy `ChatRoomManager` 改造

### 8.1 新增统一人类输入入口

在 `ChatRoomService` 和 `ChatRoomManager` 增加：

```text
SubmitHumanInputAsync(rawContent, humanRoleId, humanRoleName, ct)
```

现有 `HumanInterjectAsync` 保持“明确追加普通人类聊天消息”的低层语义，不再作为 UI 直接入口。

### 8.2 严格解析必须早于 Trim

当前 `ChatViewModel.SendAsync` 执行：

```text
InputText.Trim()
```

这会把带前导空白的无效输入变为有效调用。应改为：

1. 只用 `IsNullOrWhiteSpace` 判断是否可发送。
2. 保留原始输入交给 `SubmitHumanInputAsync` 路由。
3. 普通聊天分支可在确认不是子代理调用后按现有产品规则规范化尾随空白。

### 8.3 自动调度过滤

以下位置都必须增加 `InvocationMode == Participant`：

- `EnqueueInitialRoles`
- `PushMentionedRoles`
- `TryDequeueNextSpeaker`
- `GetManagerRole`
- 普通 mention 解析的角色索引
- `BuildChatRoomContext` 的普通可 @ 角色列表

### 8.4 公开 Step 保护

`ChatRoomManager.StepAsync(ChatRoomRole)` 收到子代理时必须抛出明确异常：

```text
子代理角色不能通过普通公开发言入口执行，请使用子代理调用接口。
```

不能静默返回 null，否则调用方难以发现错误入口。

### 8.5 上下文过滤

`BuildIncrementalUserMessages` 必须跳过：

```text
message.IsPresetInfo == true
```

`ChatRoomSession.GetMessagesSinceLastSpeak` 可以保留原始时间范围选择，但：

- preset 消息不得更新角色的“上次公开发言时间”。
- 最终输入构造必须再次过滤 preset。

长期应使用消息序号替代时间戳，本轮不要求借该功能完成 legacy 水位重构。

### 8.6 聊天室提示词

普通角色提示词应把角色分成：

```text
普通聊天室角色：可以通过 @角色名 请求公开发言
子代理角色：不能通过 @ 调用，只能使用 invoke_chatroom_subagent 工具
```

禁止继续向模型宣称“所有角色都可通过 @ 指定回复”。

### 8.7 legacy 子代理 invoker

建议新增：

```text
ChatRoomSubAgentInvoker
ChatRoomSubAgentToolProvider
ChatRoomSubAgentInvocationScope
```

invoker 使用角色定义和统一角色工厂创建临时执行对象，不直接调用聊天室中长期存在的 `ChatRoomRole.SpeakAsync`，避免复用其私有 AgentSession。

### 8.8 用户直调的显示消息

建议生成两条消息：

1. 人类调用请求：Human + `IsPresetInfo = true`。
2. 子代理结果：Assistant + `IsPresetInfo = true`。

结果的底层 `CopilotChatMessage` 也设置 `IsPresetInfo = true`。

若执行失败，追加 preset System 消息或带失败状态的子代理调用卡片；失败信息不触发普通自动循环。

## 9. 新 `ChatRoomCoordinator` 改造

### 9.1 统一人类输入命令

建议用：

```text
SubmitHumanInputCommand
```

替代应用层先调用 `AppendHumanMessageCommand` 再无条件启动自动循环的组合。

Coordinator 在单写者循环中根据当前角色快照执行严格解析，保证角色更新和输入路由使用同一个状态版本。

### 9.2 独立子代理调用命令

用户直调：

```text
InvokeUserSubAgentCommand
```

AI 工具调用不应尝试占用房间的第二个 `CurrentExecution`。它属于父 execution 内部的嵌套工具执行，由 `IChatRoomSubAgentInvoker` 在协调器外运行，并通过带 ParentExecutionId 的事件回投状态。

### 9.3 房间级并发规则

- 房间仍最多只有一个普通公开 execution。
- 父角色调用子代理时，子代理作为父 execution 内的同步工具任务运行。
- 用户直调子代理在没有普通 execution 时立即执行。
- 用户在普通角色发言期间直调子代理时，记录 preset 调用请求并排队；当前普通角色完成后优先执行该用户调用。
- 用户直调子代理不会启动或恢复普通自动循环。
- 停止、关闭、房间替换会取消未完成用户调用和所有嵌套调用。

### 9.4 普通执行入口保护

`StartExecutionCore` 必须拒绝：

```text
definition.InvocationMode == SubAgent
```

自动队列和管理者兜底同样只接受 Participant。

### 9.5 可见输入与消费高水位

Domain `ChatRoomMessage` 增加 `IsPresetInfo` 后，普通角色输入规则为：

```text
InputMessages = 消费水位之后且 IsPresetInfo == false 的消息
InputThroughSequence = 执行开始时房间最新消息序号
```

因此必须放宽 `ChatRoomRoleExecutionRequest` 当前约束：

```text
InputMessages 最后一条序号 == InputThroughSequence
```

改为：

1. 输入消息序号严格递增。
2. 每条输入消息序号不大于 `InputThroughSequence`。
3. 最后一条可见输入可以早于高水位。
4. preset 消息不进入 `InputMessages`，但成功后会被消费水位跨过。

若水位之后只有 preset 消息，不需要启动模型；Coordinator 可以执行一次“不可见消息水位推进”，并在存在 checkpoint 时复制 checkpoint 载荷，只更新其 consumed watermark 和 checkpoint revision，不提升私有 SessionRevision。

### 9.6 子代理不产生 checkpoint

子代理 invocation 不走普通 `ChatRoomRoleExecutionCandidate` 提交协议：

- 不接收 committed checkpoint。
- 不产生 candidate checkpoint。
- 不更新 `ConsumedThroughSequenceByRole`。
- 不加入 `ChatRoomSnapshot.RoleCheckpoints`。

角色定义仍由 runtime registry 管理，invoker 通过 lease 固定其配置和资源生命周期。

### 9.7 Runtime 接缝

建议增加独立接口，而不是把普通执行请求塞入大量可空字段：

```text
IChatRoomSubAgentRuntime
  InvokeAsync(ChatRoomSubAgentRuntimeRequest, eventSink, ct)
```

`IsolatedChatRoomRoleRuntime` 可以同时实现普通角色 runtime 和子代理 runtime，但每个方法拥有不同的输入与提交契约。

普通角色调用仍使用 `ExecuteAsync`；子代理调用只使用 `InvokeAsync`。

## 10. 工具设计

### 10.1 调用工具

建议工具名：

```text
invoke_chatroom_subagent
```

参数：

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `targetRoleId` | string | 是 | 子代理稳定 RoleId |
| `prompt` | string | 是 | 自包含任务说明和期望输出 |

工具描述必须包含：

- 只能选择当前列出的子代理 RoleId。
- 任务必须自包含，子代理看不到聊天室历史。
- 不要通过普通 `@` 调用子代理。
- 工具会同步等待并返回子代理提交结果。

### 10.2 子代理列表

普通角色系统提示词应列出可调用子代理：

```text
- RoleId
- RoleName
- 简短人设摘要
- ExecutionKind
```

首版不需要单独 `list_subagents`，避免增加一次不必要工具调用。动态角色变化后，每次普通角色执行都应基于当前房间快照生成工具描述和目标列表。

### 10.3 返回工具

建议工具名：

```text
ReturnOutputToCaller
```

参数：

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `output` | string | 是 | 返回给直接调用方的最终结果 |

该工具只存在于子代理运行中，普通参与者不能直接获得。

### 10.4 权限边界

子代理可使用的工具由执行引擎和宿主授权共同决定：

- Standard：角色默认工具、技能工具、工作区工具、嵌套子代理工具、返回工具。
- Coding：代码工作区工具、嵌套子代理工具、返回工具。
- ChatRoom 角色管理工具默认不提供给子代理，避免子任务修改整个房间角色结构。
- 用户直调和 AI 调用使用同一权限集合，不因调用者类型扩大权限。

## 11. 持久化与兼容

### 11.1 legacy JSON

新增字段默认值：

```text
Role.InvocationMode = Participant
Message.IsPresetInfo = false
Message.SubAgentInvocationId = null
```

旧 `room.config.json` 缺少字段时自然保持现有行为。

必须更新：

- legacy 角色定义
- legacy 消息模型
- JSON 源生成上下文关联类型
- `ChatRoomPersistence.ValidateRoleDefinitions`
- 保存/加载测试

### 11.2 角色模板

必须更新：

- `RoleTemplateService.ToDefinition`
- `RoleTemplateService.FromDefinition`
- `RoleTemplateService.UpdateFromDefinition`
- 模板校验
- 模板测试

旧模板默认 Participant。

### 11.3 新 snapshot schema

建议从 schema 2 升到 schema 3。

schema 2 → 3：

- 所有角色 `InvocationMode = Participant`
- 所有消息 `IsPresetInfo = false`
- 所有消息 `SubAgentInvocationId = null`

读取规则：

- schema 2：兼容迁移。
- schema 3：正常读取。
- 高于当前版本：拒绝。
- 低于支持范围：按明确错误拒绝。

不能继续使用“只接受等于 CurrentSchemaVersion”的实现。

### 11.4 子代理调用记录

AI 调用：

- 不写入公开 snapshot。
- 可以存在于调用者私有 AgentSession/checkpoint 的工具调用历史中。
- UI 瞬态详情默认不跨重启恢复。

用户直调：

- preset 请求和结果作为稳定消息持久化。
- 不持久化内部推理、工具流和嵌套过程。
- 恢复后按 InvocationId 关联显示。

### 11.5 公开日志

`SavePublicMessageAsync` 转换 `ChatRoomMessage` 时必须传播 `IsPresetInfo`。

也可以把用户直调记录写入独立 `subagent_calls` 日志；首版为减少存储结构变化，建议继续写公开日志，但保留 preset 标记的结构化配置才是恢复权威。

## 12. Avalonia UI 方案

### 12.1 角色编辑页

新增“角色调用方式”：

```text
聊天室参与者
子代理
```

选为子代理时：

- 自动取消人类角色。
- 人类开关禁用。
- 参与模式设为 MentionOnly 并禁用。
- 管理者配置不可用。
- 显示说明：

```text
子代理不会参与自动讨论，也不能被 AI 通过普通 @ 调用。
其他角色只能使用子代理工具调用；用户只能在消息开头使用“@角色名 ”调用。
每次调用互相独立，结果不会进入聊天室后续上下文。
```

### 12.2 角色列表

角色卡片增加类型标签：

- 人类
- 普通 AI
- 子代理
- 管理者

子代理上下文菜单：

- “调用子代理”
- “提升到角色大厅”
- “编辑角色”
- “删除角色”

隐藏或禁用：

- 普通“@ 提及角色”
- 压缩对话
- 清空记忆

无状态子代理没有可压缩或清空的持久 AgentSession。

### 12.3 输入框插入

普通角色继续使用现有追加行为：

```text
已有文本 + ` @普通角色 `
```

子代理使用独立命令：

```text
InputText = `@子代理角色名 ` + 当前任务正文
```

如果输入框已有正文：

```text
@子代理角色名 {原正文}
```

不得把子代理前缀追加到消息尾部。

### 12.4 发送分流

`ChatViewModel.SendAsync` 根据 `SubmitHumanInputAsync` 返回值决定：

- 普通聊天：按现有逻辑启动自动循环。
- 子代理立即完成：不启动自动循环。
- 子代理排队：显示等待状态，不启动普通自动循环。
- 输入无效：显示错误并保留或恢复输入内容。

### 12.5 消息展示

用户直调建议显示：

```text
用户气泡：@代码审查子代理 检查当前改动
子代理气泡：最终返回结果
```

两条消息都可以有“子代理调用”视觉标识。结果可显示模型名和 Token 用量，但不提供普通“@ 提及角色”菜单。

AI 调用继续显示在父助手消息内部的 `CopilotChatSubAgentItem`：

- 任务输入
- 进度
- 工具
- 返回给调用者的输出

## 13. 错误、取消与生命周期

### 13.1 目标角色不存在或类型错误

返回 Rejected：

- RoleId 不存在。
- 目标是普通参与者。
- 目标是人类。
- 目标配置违反子代理不变量。

### 13.2 模型不可用

用户直调：追加 preset 失败消息。

AI 调用：工具抛出可诊断异常，由父模型决定是否重试或向用户解释。

### 13.3 未调用返回工具

第二次提醒后仍无结果：ProtocolFailed。

不得使用最后一段普通文本作为结果。

### 13.4 取消

- 父 execution 取消会取消全部嵌套子代理。
- 用户停止房间会取消排队和执行中的用户直调。
- 关闭或替换房间使 RoomInstanceId 失效，迟到结果全部丢弃。
- 取消消息使用 preset，不触发普通自动循环。

### 13.5 角色更新和删除

- 调用开始时通过 runtime lease 固定角色 identity、runtime version 和 workspace version。
- 调用期间删除角色：拒绝立即删除或先取消并等待 invocation 终止。
- 调用期间更新角色：当前调用使用启动快照，更新只影响后续调用。
- 迟到结果必须校验 RoomInstanceId、InvocationId、RoleIdentity 和 RuntimeVersion。

## 14. 需要修改的核心文件

### 14.1 AgentLib

- `Tools/SubAgentToolProvider.cs`
- 可新增共享 invocation scope、runner、collector 和返回工具模型
- `Model/CopilotChatSubAgentItem.cs`（如需补充 InvocationId 或状态）
- 相关 `AgentLib.Tests`

### 14.2 AgentLib.ChatRoom legacy

- `Model/ChatRoomRoleDefinition.cs`
- `Model/ChatRoomMessage.cs`
- `MentionParser.cs`
- 新增 `UserSubAgentInvocationParser.cs`
- 新增 `ChatRoomSubAgentInvoker.cs`
- 新增 `Tools/ChatRoomSubAgentTools.cs`
- `ChatRoomManager.cs`
- `ChatRoomManager.ChatRoomAutoLoopRunner.cs`
- `ChatRoomSession.cs`
- `ChatRoomRole.cs`
- `ChatRoomRoleExecutionContext.cs`
- `StandardChatRoomRoleExecutor.cs`
- `CodingChatRoomRoleExecutor.cs`
- `ChatRoomPersistence.cs`
- `Services/ChatRoomService.cs`
- `Services/RoleTemplateService.cs`
- `Tools/ChatRoomRoleManagementTools.cs`
- `Services/CodingAssistantRoleFactory.cs`

### 14.3 AgentLib.Coding

- `CodingAgent.cs`
- `CodingWorkspaceToolSession.cs` 或运行时工具合并位置
- 对应测试

### 14.4 Domain / Coordinator / Runtime

- `Domain/ChatRoomEnums.cs`
- `Domain/ChatRoomRoleDefinition.cs`
- `Domain/ChatRoomMessage.cs`
- `Domain/ChatRoomSnapshot.cs`
- `Coordination/ChatRoomCommand.cs`
- `Coordination/ChatRoomChange.cs`
- `Coordination/ChatRoomCoordinator.cs`
- `Runtime/IChatRoomRoleRuntime.cs`
- `Runtime/IsolatedChatRoomRoleRuntime.cs`
- `Runtime/ChatRoomRoleRuntimeRegistry.cs`
- `Persistence/StoredChatRoomSnapshot.cs`
- `Persistence/ChatRoomSnapshotMapper.cs`

### 14.5 Avalonia

- `ViewModels/RoleEditViewModel.cs`
- `Views/RoleEditView.axaml`
- `ViewModels/RoleListViewModel.cs`
- `Views/RoleListView.axaml`
- `ViewModels/ChatViewModel.cs`
- `Views/ChatView.axaml`
- `ViewModels/RoleLobbyViewModel.cs`
- 相关 Shell 测试

## 15. 测试计划

### 15.1 严格用户前缀解析

1. 精确 `@子代理名 任务` 匹配。
2. 消息中段不匹配。
3. 前导空白不匹配。
4. Tab、换行和全角空格不匹配。
5. 方括号格式不匹配。
6. 缺少任务返回 Invalid。
7. 普通角色前缀返回 NoMatch。
8. 未知角色返回 NoMatch。
9. 大小写按 OrdinalIgnoreCase 匹配。
10. 输入在解析前未被 Trim。

### 15.2 普通 mention 隔离

1. 人类消息中段 `@子代理 ` 不写入 MentionedRoleIds。
2. AI 消息开头 `@子代理 ` 不写入 MentionedRoleIds。
3. 普通 MentionOnly 角色仍可被 @。
4. 多 mention 中只保留普通参与者。
5. 角色改名后旧结构化 mention 不重新解释。

### 15.3 调度保护

1. 子代理不进入默认队列。
2. 子代理不进入优先 mention 队列。
3. 子代理不作为管理者兜底。
4. legacy `StepAsync` 拒绝子代理。
5. Coordinator 普通 `StartRoleExecutionCommand` 拒绝子代理。
6. 用户子代理调用不启动普通自动循环。
7. 子代理调用完成后默认角色不自动发言。

### 15.4 工具回传协议

1. 返回工具与调用 runner 共享同一个 collector。
2. 返回值等于 `ReturnOutputToCaller` 的 output 参数。
3. 普通文本不被当作结果。
4. 第一次未返回时提醒并重试一次。
5. 第二次未返回时 ProtocolFailed。
6. 空白 output 被拒绝。
7. 重复提交被拒绝。
8. 模型异常、工具异常和取消分别映射正确终态。

### 15.5 无状态与上下文隔离

1. 子代理只收到本次 prompt。
2. 子代理不收到聊天室历史。
3. 第二次调用看不到第一次调用。
4. 子代理不恢复 checkpoint。
5. 子代理不保存 checkpoint 或 legacy AgentSession 状态。
6. 角色静态 MemoryContent 每次都可注入。
7. 普通角色看不到用户直调请求和结果。
8. preset 消息被普通消费水位跨过且以后不补发。

### 15.6 AI 调用

1. Standard 普通角色可以调用子代理。
2. Coding 普通角色可以调用子代理。
3. 调用结果只作为父角色 function result。
4. 公开消息中没有子代理原始输出。
5. UI 能显示父消息内的子代理调用项。
6. 普通 AI 通过 `@子代理` 无效。
7. 调用不存在或普通角色 RoleId 时被拒绝。

### 15.7 嵌套与环路

1. 子代理 A 可以工具调用子代理 B。
2. A → A 被拒绝。
3. A → B → A 被拒绝。
4. 超过最大深度被拒绝。
5. 父调用取消会取消子调用。

### 15.8 持久化

1. legacy 角色 InvocationMode 往返。
2. 旧 JSON 缕失字段默认为 Participant。
3. preset 与 InvocationId 往返。
4. 模板三种转换都保留 InvocationMode。
5. snapshot schema 3 往返。
6. schema 2 恢复为 Participant/非 preset。
7. 未知高版本仍拒绝。
8. 子代理没有 checkpoint。
9. 用户直调恢复后仍显示但不进入上下文。

### 15.9 UI

1. 角色编辑页正确加载和保存子代理类别。
2. 子代理强制非人类、MentionOnly、非管理者。
3. 子代理角色卡显示类型标签。
4. 子代理菜单使用“调用子代理”。
5. 插入前缀始终位于输入开头。
6. 输入有前导空白时不会被 Trim 成有效调用。
7. 普通发送和子代理发送正确分流。
8. preset 结果可见。
9. preset 结果不提供普通 mention 操作。

所有新增 MSTest 测试应设置硬超时；新测试按项目现有规范使用中文 `DisplayName`。

## 16. 分步实施计划

1. 修正 `SubAgentToolProvider` 的共享 collector 作用域，并补齐真正的返回值测试。
2. 抽取可复用的子代理 invocation runner、返回工具和协议终态。
3. 在 legacy 与 Domain 角色定义中增加 `ChatRoomRoleInvocationMode` 和不变量校验。
4. 在 legacy 与 Domain 消息中增加 `IsPresetInfo` 和 `SubAgentInvocationId`。
5. 实现严格 `UserSubAgentInvocationParser`，并让普通 mention 排除子代理。
6. 实现共享 `IChatRoomSubAgentInvoker` 和无状态临时运行路径。
7. 为 Standard 角色注入聊天室子代理调用工具。
8. 为 CodingAgent 增加受控 host tool 接缝并注入子代理调用工具。
9. 改造 legacy 自动队列、公开 Step、上下文构造和聊天室提示词。
10. 增加 `SubmitHumanInputAsync`，改造 ChatRoomService 与 Avalonia 发送分流。
11. 改造 Coordinator 命令、嵌套 invocation 事件、普通执行保护和 preset 输入过滤。
12. 改造 legacy JSON、模板、snapshot schema 3 及 schema 2 兼容迁移。
13. 改造角色编辑、角色列表、输入菜单和用户直调结果展示。
14. 添加解析、调度、工具协议、无状态、嵌套、持久化和 UI 测试。
15. 运行 AgentLib、AgentLib.Coding、AgentLib.ChatRoom、Shell 相关测试和完整构建。
16. 更新 ChatRoom 需求文档与 README，明确普通角色和子代理的调用差异。

## 17. 验收标准

1. 子代理角色不能通过普通自动循环发言。
2. 人类或 AI 消息中的普通 `@子代理名` 都不会触发子代理。
3. 用户只有在消息严格以 `@子代理名 ` 开头时才能调用。
4. 消息中段、前导空白、Tab 和方括号格式都不会误触发。
5. 普通 AI 角色可以使用专用工具调用子代理。
6. Standard 与 Coding 普通角色都支持该工具。
7. 子代理每次调用只看到自包含任务，不读取聊天室历史或旧调用历史。
8. 子代理必须通过返回工具提交最终结果；未提交时明确失败。
9. AI 调用结果只返回调用者，不形成公开聊天室消息。
10. 用户直调请求和结果可显示，结果 `IsPresetInfo = true`。
11. 用户直调请求和结果不进入任何普通角色后续输入。
12. 用户直调不会启动普通自动循环。
13. 子代理不会生成或恢复角色 checkpoint/AgentSession 状态。
14. 旧会话和旧模板默认恢复为普通角色。
15. snapshot schema 2 可以升级读取，未知更高版本仍被拒绝。
16. 调用取消、失败、角色更新、角色删除和房间关闭不会提交迟到结果。
17. 嵌套调用有循环和深度保护。
18. 现有 MentionOnly、管理者、Coding、角色管理和持久化回归测试保持通过。

## 18. 最终建议

该功能不应实现为“第三种 ParticipationMode”，也不应只在 mention 正则中增加特例。正确边界是：

```text
角色定义明确声明 InvocationMode
  + 用户输入使用独立严格协议
  + AI 使用专用子代理工具
  + 子代理使用无状态独立运行
  + 最终结果必须工具提交
  + preset 消息由上下文构造真正过滤
  + 普通调度和普通执行入口双重拒绝
```

这样可以保持普通多角色聊天室、现有子代理工具和新 Coordinator 架构三者语义一致，并为未来增加搜索子代理、审查子代理、编程子代理等不同角色模板提供稳定扩展点。
