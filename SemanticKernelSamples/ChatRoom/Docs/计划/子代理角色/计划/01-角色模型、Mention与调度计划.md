# ChatRoom 角色模型、Mention 与调度实施计划

## 1. 目标

本阶段在当前唯一的聊天室实现中建立子代理基础语义：

- `Model/ChatRoomRoleDefinition.cs` 增加独立 `InvocationMode`。
- Mention 从 RoleId 列表升级为带来源和位置的结构化结果。
- `Model/ChatRoomMessage.cs` 增加 `IsPresetInfo` 与结构化 `Mentions`。
- `MentionParser` 继续作为唯一 Mention 语法核心。
- Participant 保持现有 Mention 行为；SubAgent 只接受非 preset 消息索引 0 的 Mention。
- SubAgent 不进入默认参与队列和管理者兜底。
- preset 消息不进入后续上下文，也不继续触发 Mention。

本阶段只打通“选中谁、给谁什么任务”的语义。Standard 返回工具、新会话和 AITool 调用在下一阶段实现。

## 2. 当前代码事实

### 2.1 角色模型

- 当前角色模型位于 `Model/ChatRoomRoleDefinition.cs`。
- `ParticipationMode` 只表达普通参与者何时加入自动队列，`ExecutionKind` 只表达 Standard/Coding 执行方式。
- `ChatRoomManager` 更新角色定义时原位修改现有 `ChatRoomRole`，新增调用模式不需要运行时替换、角色世代或 checkpoint 协议。

### 2.2 Mention

- `MentionParser` 已支持 `@角色名`、`@[角色名]`、大小写不敏感和现有空白边界。
- 解析结果按文本顺序返回，同一角色只保留第一次，但目前只返回 RoleId。
- `ChatRoomMessage` 无法记录 Mention 是否位于开头，也无法把调度项关联回触发消息。

### 2.3 调度和上下文

- 自动循环优先栈只保存角色，触发消息和 Mention 位置会丢失。
- 默认队列和管理者兜底尚未排除 SubAgent。
- `BuildIncrementalUserMessages` 尚未过滤外层 preset 消息。

## 3. 目标契约

### 3.1 角色调用模式

新增：

```text
ChatRoomRoleInvocationMode
  Participant = 0
  SubAgent = 1
```

规则：

- 与 `ParticipationMode`、`ExecutionKind`、`IsManagerRole`、`IsHuman` 正交。
- 不增加 SubAgent 必须非 Human、必须 MentionOnly、不能是 Manager 等组合校验。
- 内存新对象可以默认 `Participant`；持久化加载不能用默认值兼容旧数据。
- 修改 `InvocationMode` 沿用现有角色更新机制。

### 3.2 结构化 Mention

新增一个适配当前消息标识类型的不可变模型：

```text
TargetRoleId
SourceMessageId
StartIndex
Length
```

不额外保存 `IsAtMessageStart`；需要时直接判断 `StartIndex == 0`。

最小不变量：

- RoleId 和来源 ID 有效。
- `StartIndex >= 0`，`Length > 0`。
- Mention span 不超过所属消息内容。

不在 Mention 中复制发送者类型、preset 状态、角色名或整条消息。

### 3.3 MentionParser

保留现有 `MentionParser` 作为唯一语法核心。解析结果增加 `TargetRoleId`、`StartIndex` 和 `Length`，消息创建处再补充 `SourceMessageId`。

必须保持：

- `@角色名` 和 `@[角色名]`。
- 现有空白、Tab、换行和角色名边界。
- 大小写不敏感。
- 文本顺序。
- 同一 RoleId 首次匹配去重。
- 不在解析前 Trim 文本。

解析器不判断 Participant/SubAgent，也不丢弃中段 SubAgent Mention。

### 3.4 消息模型

`Model/ChatRoomMessage.cs` 增加：

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

默认队列和管理者兜底都只选择 `InvocationMode == Participant` 的角色。

### 3.6 调度项与任务文本

优先队列不能继续只保存角色，最小队列项携带：

```text
TargetRoleId / Role
TriggerMessage
TriggerMention
```

默认参与和管理者兜底的 trigger 可以为空。

SubAgent 的任务文本从开头 Mention token 结束位置截取，只移除 token 后的分隔空白，不对整条正文 Trim。任务为空白时由执行阶段明确失败。

Participant 继续使用完整的普通增量上下文，不使用该裁剪规则。

### 3.7 preset 可见性

- 消息仍保存和显示。
- `BuildIncrementalUserMessages` 跳过 preset。
- 最新 trigger 选择和 Mention 入队跳过 preset。
- 不改变消息类型，不新增子代理消息 kind。

## 4. 按序实施任务

### 01-01 增加角色调用模式

修改 `Model/ChatRoomRoleDefinition.cs` 和所有角色创建、复制、模板与编辑映射点，使现有角色显式或默认成为 Participant。

### 01-02 新增结构化 Mention 模型

新增 `ChatRoomMention`，实现 RoleId、来源 ID、span 的最小构造校验和只读属性。

### 01-03 升级 MentionParser

重构 `MentionParser.cs` 返回位置匹配。先补解析回归测试，再迁移生产调用点。

### 01-04 升级消息模型

在 `Model/ChatRoomMessage.cs` 加入 `IsPresetInfo` 和 `Mentions`，移除 `MentionedRoleIds`。同步更新消息工厂、复制和序列化上下文引用。

持久化版本和严格加载在阶段三处理，本任务只保证新模型可序列化和现有内存路径可运行。

### 01-05 保存消息 Mention

- `HumanInterjectAsync` 创建 Human 消息后解析并保存 Mentions。
- 角色输出完成后保存 Assistant Mentions。
- 已有结构化结果时不再二次解析文本。

### 01-06 改造自动循环队列

将优先队列项扩展为携带 trigger message/mention；在一个 helper 中集中执行调度表。

同时：

- 默认队列排除 SubAgent。
- 管理者兜底排除 SubAgent。
- preset 不产生优先项。
- Human 角色继续不进入模型执行队列。
- 保持现有顺序、去重、发言次数和连续发言延后行为。

### 01-07 构造子代理任务输入

新增小型纯函数或 AutoLoopRunner 内部 helper，从 trigger message/mention 提取本次任务，并通过现有 Step 调用的最小内部参数传到执行层。只移除 Mention token 后的分隔空白，不得对整条任务执行 `Trim()`。

本阶段不创建正式 collector，不用普通文本模拟返回协议。

### 01-08 过滤 preset 上下文

修改最新 trigger 选择和 `BuildIncrementalUserMessages`：跳过 preset，保留现有 System、自身消息、时间水位和宿主附加消息语义。

### 01-09 更新聊天室提示词

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

### 调度

- Human 与 Assistant 开头 Mention SubAgent 都调度。
- Human 与 Assistant 中段 Mention SubAgent 都不调度。
- Participant 任意有效位置保持现有行为。
- preset 中任何 Mention 都不调度。
- 默认队列和管理者兜底不选择 SubAgent。
- 调度实现不按发送者类型分支。

### 上下文

- 普通角色看不到 preset 消息。
- SubAgent 只得到 Mention 后的任务文本。
- 前导空白导致 Mention 非开头，且发送前不被 Trim 改写。

所有新增异步 MSTest 使用硬超时，并遵循项目现有中文 `DisplayName` 规范。

## 6. 完成门禁

- 当前模型可无歧义表达 InvocationMode、Mention 位置和 preset，且不双写派生位置状态。
- `MentionParser` 是唯一 Mention 语法实现。
- 当前自动调度表已生效，普通角色行为无回归。
- 没有长期 RoleId-only Mention 双写。
- 未修改 Coding 执行链和 AgentLib 现有子代理实现。
