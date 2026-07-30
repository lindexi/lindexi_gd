# ChatRoom 角色模型、Mention 与调度实施计划

## 1. 目标

本阶段建立两套架构共享的基础语义，并让当前生产 legacy 自动循环先正确调度子代理：

- 角色增加独立 `InvocationMode`。
- Mention 从 RoleId 列表升级为带来源和位置的结构化结果。
- 聊天室消息增加 `IsPresetInfo` 与结构化 `Mentions`。
- legacy 和 Coordinator 顶层消息使用同一套现有 Mention 语法。
- Participant 保持现有 Mention 行为；SubAgent 只接受非 preset 消息索引 0 的 Mention。
- SubAgent 不进入默认参与队列和管理者兜底。
- preset 消息不进入后续上下文，也不继续触发 Mention。

本阶段只打通“选中谁、给谁什么任务”的语义。Standard 返回工具、新会话和 AITool 调用在下一阶段实现。Coordinator 的内部 AITool 嵌套调用不属于首期范围。

## 2. 当前代码事实

### 2.1 角色模型

- legacy `Model/ChatRoomRoleDefinition.cs` 和 Domain `Domain/ChatRoomRoleDefinition.cs` 都只有 `ParticipationMode` 与 `ExecutionKind`。
- legacy 更新角色定义时原位修改现有 `ChatRoomRole`。
- Coordinator 当前把任意 definition 变化都解释为 runtime replacement；新增 `InvocationMode` 后，至少要让仅该字段变化不替换 runtime、不清 checkpoint、不重置消费水位。

### 2.2 Mention

- legacy `MentionParser` 已支持 `@角色名`、`@[角色名]`、大小写不敏感和现有空白边界。
- legacy 解析按文本顺序返回，同一角色只保留第一次，但只返回 RoleId。
- Coordinator 有独立 `IndexOf` 解析，语法与 legacy 不一致。
- 两套消息模型都只有 `MentionedRoleIds`，无法知道 Mention 是否位于开头。

### 2.3 调度和上下文

- legacy 自动循环的优先栈只保存角色，触发消息和 Mention 位置会丢失。
- legacy 默认队列和管理者兜底尚未排除 SubAgent。
- legacy `BuildIncrementalUserMessages` 和 Coordinator runtime 输入构造尚未过滤 preset。
- Coordinator 自动循环同样只保存 RoleId，并重新解析消息文本。

## 3. 目标契约

### 3.1 角色调用模式

legacy 与 Domain 分别增加：

```text
ChatRoomRoleInvocationMode
  Participant = 0
  SubAgent = 1
```

规则：

- 与 `ParticipationMode`、`ExecutionKind`、`IsManagerRole`、`IsHuman` 正交。
- 不增加 SubAgent 必须非 Human、必须 MentionOnly、不能是 Manager 等组合校验。
- 内存新对象可以默认 `Participant`；持久化加载不能用默认值兼容旧数据。
- 只修改 `InvocationMode` 时保留现有角色/runtime/checkpoint/消费水位。

### 3.2 结构化 Mention

legacy 和 Domain 各有一个适配自身 MessageId 类型的不可变模型。不要额外保存派生的 `IsAtMessageStart` 状态；需要时直接使用 `StartIndex == 0`，避免同一事实双写：

```text
TargetRoleId
SourceMessageId
StartIndex
Length
```

最小不变量：

- RoleId 和来源 ID 有效。
- `StartIndex >= 0`，`Length > 0`。
- Mention span 不超过所属消息内容。

不在 Mention 中复制发送者类型、preset 状态、角色名或整条消息。

### 3.3 统一解析器

保留现有 `MentionParser` 作为唯一语法核心。它接收角色 ID/名称快照，返回中立匹配结果，再由 legacy/Domain 调用点补上 MessageId。中立匹配结果只需 `TargetRoleId`、`StartIndex`、`Length`，不新增解析器接口层次。

必须保持：

- `@角色名` 和 `@[角色名]`。
- 现有空白、Tab、换行和角色名边界。
- 大小写不敏感。
- 文本顺序。
- 同一 RoleId 首次匹配去重。
- 不在解析前 Trim 文本。

解析器不判断 Participant/SubAgent，也不丢弃中段 SubAgent Mention。

### 3.4 消息模型

legacy 与 Domain `ChatRoomMessage` 增加：

```text
bool IsPresetInfo
IReadOnlyList<ChatRoomMention> Mentions
```

并移除长期的 `MentionedRoleIds` 双写。

规则：

- 普通消息默认 `IsPresetInfo = false`。
- Mentions 按位置有序，同一目标只保留一次，来源 ID 必须等于当前消息。
- Human 与 Assistant 使用相同 Mention 模型。
- 历史 Mention 的目标角色后来被删除时，消息本身仍可保留。
- 需要 RoleId 的调用点从 `Mentions` 投影。

### 3.5 调度表

| 来源消息 | 目标模式 | Mention 位置 | 结果 |
|---|---|---|---|
| 非 preset | Participant | 任意有效位置 | 保持现有调度 |
| 非 preset | SubAgent | `StartIndex == 0` | 调度目标 |
| 非 preset | SubAgent | 非开头 | 不调度 |
| preset | 任意 | 任意位置 | 不调度 |

判断不读取发送者类型。

默认队列条件增加 `InvocationMode == Participant`；管理者兜底同样增加该条件。

### 3.6 调度项与任务文本

legacy 优先队列不能继续只保存角色；否则到 `StepAsync` 时已经丢失触发消息。最小队列项携带：

```text
TargetRoleId / Role
TriggerMessage
TriggerMention
```

默认参与和管理者兜底的 trigger 可以为空。Coordinator 若采用“从当前结构化消息按 RoleId 反查 trigger”的小改动即可满足顶层调度，不强制建立与 legacy 相同的队列类型；只要不重新解析文本且结果一致即可。

SubAgent 的任务文本从开头 Mention token 结束位置截取，只移除 token 后的分隔空白，不对整条正文 Trim。任务为空白时由执行阶段明确失败。

Participant 继续使用完整的普通增量上下文，不使用该裁剪规则。

### 3.7 preset 可见性

- 消息仍保存和显示。
- legacy `BuildIncrementalUserMessages` 跳过 preset。
- Coordinator 的 `InputMessages` 跳过 preset，但 `InputThroughSequence`/观察上界仍可跨过这些消息。
- 最新 trigger 选择和 Mention 入队跳过 preset。
- 不改变消息类型，不新增子代理消息 kind。

## 4. 按序实施任务

### 01-01 增加角色调用模式

修改 legacy/Domain 枚举和角色定义；更新所有角色创建、复制和映射点，使现有角色显式或默认成为 Participant。

至少检查：

- `Model/ChatRoomRoleDefinition.cs`
- `Domain/ChatRoomEnums.cs`
- `Domain/ChatRoomRoleDefinition.cs`
- `Domain/ChatRoomSnapshot.cs`
- `Runtime/IsolatedChatRoomRoleRuntime.cs`
- 测试场景构造器

### 01-02 新增结构化 Mention 模型

新增 legacy/Domain `ChatRoomMention`，实现 RoleId、来源 ID、span 的最小构造校验和只读属性。不保存 `IsAtMessageStart` 派生字段。

### 01-03 统一 Mention 解析

重构 `MentionParser.cs` 返回位置匹配；删除 Coordinator 私有的不同语法实现，让两条链路投影同一个匹配核心。

先补充解析回归测试，再迁移生产调用点。

### 01-04 升级消息模型

在 legacy/Domain 消息中加入 `IsPresetInfo` 和 `Mentions`，移除 `MentionedRoleIds`。同步更新工厂、深复制、snapshot mapper DTO 占位和测试构造器，保证编译迁移一次完成。

持久化版本和严格加载在阶段三处理，本任务只保证新模型可序列化和现有内存路径可运行。

### 01-05 保存消息 Mention

- `HumanInterjectAsync` 创建 Human 消息后解析并保存 Mentions。
- legacy 角色输出完成后保存 Assistant Mentions。
- Coordinator 的 Human/Assistant 消息提交也使用统一解析器。
- 已有结构化结果时不再二次解析文本。

### 01-06 改造 legacy 队列

将优先队列项扩展为携带 trigger message/mention；在一个 helper 中集中执行调度表。

同时：

- 默认队列排除 SubAgent。
- 管理者兜底排除 SubAgent。
- preset 不产生优先项。
- Human 角色继续不进入模型执行队列。
- 保持现有顺序、去重、发言次数和连续发言延后行为。

### 01-07 构造子代理任务输入

新增小型纯函数或 AutoLoopRunner 内部 helper，从 trigger message/mention 提取本次任务，并把结果通过现有 Step 调用的最小内部参数传到执行层。只移除 Mention token 后的分隔空白；不得对整条任务执行 `Trim()`。

本阶段不创建正式 collector，不用普通文本模拟返回协议。

### 01-08 过滤 legacy 上下文

修改最新 trigger 选择和 `BuildIncrementalUserMessages`：跳过 preset，保留现有 System、自身消息、时间水位和宿主附加消息语义。

### 01-09 对齐 Coordinator 顶层调度

Coordinator 使用同一调度表和结构化消息数据：

- `HandleAppendHumanMessage` 和 Assistant candidate 提交时保存结构化 Mentions。
- Participant 顶层执行读取水位后的非 preset 消息。
- SubAgent 开头 Mention 顶层执行只获得任务文本，不恢复旧 checkpoint。
- `InputThroughSequence` 允许晚于最后一条模型可见输入，以便水位跨过 preset。
- 默认队列和管理者兜底排除 SubAgent。

此任务不实现 Standard AITool 内部角色调用，不改造 `CurrentExecution`，不增加 execution host、内部调用命令或 checkpoint revision 重基线。

### 01-10 修正仅 InvocationMode 更新

在 Coordinator 角色更新逻辑中加入最小判定：仅 `InvocationMode` 变化时，允许 `RuntimeVersion` 保持不变，并保留 runtime、checkpoint 和消费水位。

不借此重构全部角色字段分类。

### 01-11 更新聊天室提示词

普通 Standard 角色提示词说明：

- Participant 可按现有 Mention 规则请求后续发言。
- SubAgent 只有 Mention 位于索引 0 才会触发。
- 开头 Mention 只安排后续公开执行；需要当前轮同步结果时使用 `InvokeChatRoomSubAgent`。
- 列出可调用 SubAgent 的 RoleId、RoleName、简介和 ExecutionKind。

在阶段二工具尚未接入的中间提交中，不把该阶段发布为完整产品功能。

## 5. 关键测试

### Mention 解析

- 普通和方括号语法保持。
- 开头、中段、前导空白位置准确。
- Tab、换行、大小写与现有规则一致。
- 多 Mention 按文本顺序，同一角色首次去重。
- legacy/Coordinator 对相同输入产生同等匹配。

### 调度

- Human 与 Assistant 开头 Mention SubAgent 都调度。
- Human 与 Assistant 中段 Mention SubAgent 都不调度。
- Participant 任意有效位置保持现有行为。
- preset 中任何 Mention 都不调度。
- 默认队列和管理者兜底不选择 SubAgent。
- 调度实现不按发送者类型分支。

### 上下文

- 普通角色看不到 preset 消息。
- 消费/观察上界可以跨过 preset。
- SubAgent 只得到 Mention 后的任务文本。
- 前导空白导致 Mention 非开头，且发送前不被 Trim 改写。

### 角色更新

- 仅修改 InvocationMode 不替换 runtime、不删除 checkpoint、不重置水位。
- 运行时字段变化仍沿用现有 replacement 规则。

所有新增异步 MSTest 使用硬超时，并遵循项目现有中文 `DisplayName` 规范。

## 6. 完成门禁

- 两套模型可无歧义表达 InvocationMode、Mention 位置和 preset，且不双写派生位置状态。
- legacy 与 Coordinator 共用同一 Mention 语法。
- legacy 生产调度表已生效，普通角色回归通过。
- Coordinator 顶层调度语义一致，但不要求 AITool 内部调用完成。
- 没有长期 RoleId-only Mention 双写。
- 未修改 Coding 执行链和 AgentLib 现有子代理实现。
