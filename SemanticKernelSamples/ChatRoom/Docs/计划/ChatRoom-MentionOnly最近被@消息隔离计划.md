# ChatRoom MentionOnly 最近被 @ 消息隔离计划

## 文档定位

本文为 `MentionOnly` 参与模式增加一个独立的上下文隔离开关：

> 开启后，角色在一次由 @mention 触发的发言中，只接收本次触发它的最近一条公开消息，不接收该角色此前尚未发言期间积累的其他聊天室消息。

该能力主要解决以下场景：

- 新角色加入已有大量历史的聊天室
- 新角色第一次被 @ 时不应读取整个聊天室历史
- 专业角色只需要处理明确指派给自己的任务
- MentionOnly 角色不应因为长时间未参与而在下一次发言中获得大量无关上下文

本文只给出修改计划，不实现代码。

## 术语和建议配置名

建议在角色定义中新增独立布尔配置：

```text
OnlyReadLatestMentionMessage
```

中文界面名称建议为：

```text
被 @ 时仅读取触发消息
```

说明文本建议为：

```text
开启后，该角色由 @ 提及触发发言时，只读取本次直接 @ 它的最新消息，不补发其他聊天室历史。角色已有的私有对话记忆不会被清空。
```

## 与 ParticipationMode 的关系

该字段是独立配置，不新增第三个参与模式。

生效条件必须同时满足：

1. `ParticipationMode == MentionOnly`
2. `OnlyReadLatestMentionMessage == true`
3. 本次发言确实由一条包含当前角色 RoleId 的 @mention 消息触发

以下情况不应用隔离规则：

- `AlwaysParticipate` 角色正常轮流发言
- 用户或测试代码通过 `StepAsync(role)` 手动要求角色发言
- MentionOnly 管理者在无人可继续发言时作为兜底管理者发言
- 其他没有明确 mention 触发消息的宿主调用

这样可以保持“参与时机”和“输入可见范围”两个概念独立：

- `ParticipationMode` 决定角色何时进入调度
- `OnlyReadLatestMentionMessage` 决定 mention 触发时能看到哪些公开消息

## 默认值策略

### 已有角色和历史数据

字段默认值为 `false`。

旧会话和旧模板 JSON 不包含该字段，反序列化后自然保持现有行为，避免升级后静默改变已有角色的上下文范围。

### 新建 MentionOnly 角色

为解决“新角色第一次被 @ 就读取全部历史”的核心问题，建议以下新角色默认开启：

- `ChatRoomRoleManagementTools.create_character` 动态创建的角色
- `CodingAssistantRoleFactory` 创建的编程助手
- Avalonia 角色编辑页中新建并选择 `MentionOnly` 的 AI 角色

用户可以在角色编辑页关闭该开关。

`AlwaysParticipate` 新角色默认关闭。若用户把已开启该选项的角色临时改为 `AlwaysParticipate`，建议保留字段值但使其不生效；切换回 `MentionOnly` 后恢复原偏好。

## 当前问题根因

### Legacy ChatRoomManager 使用“上次发言时间”推导增量范围

`ChatRoomSession.GetMessagesSinceLastSpeak(roleId)` 当前逻辑为：

- 角色发过言：返回其上次公开发言时间之后的全部消息
- 角色从未发言：返回聊天室全部公开消息

`BuildIncrementalUserMessages` 再从中跳过系统消息和自身消息，并为其余消息添加发送者前缀。

因此，新加入角色第一次被 @ 时会获得加入前的全部历史；长期沉默的 MentionOnly 角色会获得上次发言之后积累的全部内容。

### 调度队列只保存角色，不保存触发消息

当前自动循环的优先栈保存 `ChatRoomRole`。一条消息解析出 `MentionedRoleIds` 后，只把角色放入队列，触发它的具体 `ChatRoomMessage` 身份随即丢失。

如果执行时再从整个历史中倒序搜索“最近一条提及该角色的消息”，会产生错误：

- 手动 `StepAsync` 可能误用很久以前的 mention
- 管理者兜底可能被旧 mention 错误裁剪
- 同一角色排队期间再次被 mention 时无法明确哪条消息应覆盖旧触发
- 角色改名后重新解析文本可能与当时的结构化 mention 结果不同
- 失败重试无法证明重试的是哪一个触发事件

因此，“最近一条”必须指调度器实际选择的最新 mention 触发消息，而不是执行阶段进行无上下文的历史扫描。

### 新 Coordinator 已有消费水位，但仍传递全部未消费消息

新 `ChatRoomCoordinator` 使用 `ConsumedThroughSequenceByRole` 和单调递增的 `MessageSequence` 记录消费进度，比 legacy 时间戳更可靠。

但 `StartExecutionCore` 当前仍把消费水位之后的全部消息放入 `ChatRoomRoleExecutionRequest.InputMessages`。新角色消费水位为 0，因此第一次执行同样会获得全部历史。

## 目标行为

### 开关关闭

行为完全保持现状：

```text
本轮输入 = 角色上次发言/消费水位之后的全部可见公开消息
```

### 开关开启且由 mention 触发

```text
本轮公开消息输入 = 触发本次调度的那一条 ChatRoomMessage
```

该消息整体保留，包括：

- @ 当前角色之前和之后的文本
- 同一条消息中对其他角色的 @mention
- 原始发送者身份
- 原始完整公开内容

不从消息文本中裁剪只有 `@角色名` 后面的片段。

### 开关开启但不是 mention 触发

使用现有增量消息行为，不从历史中猜测旧 mention。

这适用于：

- 手动单步发言
- 管理者兜底
- 宿主显式执行

### 多次 mention

如果同一角色尚未发言时又收到新的 mention：

- 队列中只保留该角色一次
- 更新其触发消息为最新一条 mention
- 角色最终只看到最新触发消息

如果同一条消息多次 mention 同一角色，仍只产生一次调度，角色看到整条消息一次。

### 一条消息 mention 多个角色

每个被 mention 的角色都以同一条消息作为自己的触发输入。是否省略“用户说：”前缀由单 AI 消息格式规则单独决定。

## 不清空角色私有 AgentSession

本开关只改变 `BuildIncrementalUserMessages` 或新 Runtime 的“本轮新增公开消息选择”，不清空角色已经拥有的私有 AgentSession。

因此：

- 新加入角色没有历史，只会看到首次触发消息
- 已经参与过的角色仍能记住自己此前收到的 mention 和自己的回答
- 角色不会看到从未发送给它的其他公开消息
- 开关切换不自动调用 `ClearSessionMemory`

如果未来需要“每次 mention 都完全无状态”，应另设“每次触发使用新 AgentSession”功能，不能混入本配置。

## Legacy 调度设计

### 设计一：队列项同时保存角色和 mention 触发消息

建议新增内部不可变队列项，例如：

```text
QueuedRoleTurn
  - ChatRoomRole Role
  - ChatRoomMessage? MentionTriggerMessage
```

将以下集合从只保存角色改为保存队列项：

- `priorityRoles`
- `defaultRoles`
- 暂缓角色列表

规则：

- mention 触发入优先队列：保存具体触发消息
- 默认参与入普通队列：触发消息为 null
- 管理者兜底：触发消息为 null
- 手动 `StepAsync(role)`：触发消息为 null
- 同一角色被新的消息再次 mention：移除旧队列项，并用新触发消息重新入优先队列

`lastSpeakerRoleId`、最大发言次数和队列去重仍按 RoleId 工作。

### 设计二：所有 mention 入队入口传递消息对象

调整以下调用关系：

```text
EnqueueInitialRoles(triggerMessage)
  → PushMentionedRoles(..., triggerMessage)

HandleAutoLoopMessageAsync(message)
  → 解析并写入 message.MentionedRoleIds
  → PushMentionedRoles(..., message)
```

不得只传 `mentionedRoleIds` 后丢弃消息身份。

结构化 `MentionedRoleIds` 是权威判断。只有历史消息尚未填充该字段时，才沿用现有 `MentionParser` 作为兼容回退。

为避免新加入角色被旧消息中的同名文本意外触发，兼容回退不应在每次调度时针对“当前角色列表”重复执行。建议在加载旧会话时，使用当时恢复出的角色快照对缺失 `MentionedRoleIds` 的消息做一次性回填；运行期间新增角色后，历史消息不再重新解释。新产生的消息继续在提交时立即写入结构化 RoleId。

### 设计三：StepAsync 显式接收本轮触发上下文

私有发言入口调整为类似：

```text
StepAsync(
  ChatRoomRole role,
  ChatRoomMessage? mentionTriggerMessage,
  IReadOnlyList<string>? additionalUserMessages,
  CancellationToken cancellationToken)
```

公开 `StepAsync(role)` 继续传 null，保持手动调用语义。

`BuildIncrementalUserMessages` 同样接收触发消息，并按以下顺序选择公开消息：

```text
if role.ParticipationMode == MentionOnly
   && role.OnlyReadLatestMentionMessage
   && mentionTriggerMessage 明确包含 roleId
then
   publicMessages = [mentionTriggerMessage]
else
   publicMessages = Session.GetMessagesSinceLastSpeak(roleId)
```

若内部调用声称是 mention 触发，但消息的 `MentionedRoleIds` 不包含角色，应立即暴露调度不变量错误，而不是静默扫描其他历史消息。

### 设计四：继续复用统一消息格式化

消息选择完成后，再应用第二项计划中的格式规则：

- 跳过系统消息
- 跳过自身消息
- 单 AI 场景的人类消息可直接使用原文
- 多 AI 场景保留 `用户说：`
- AI 消息保留 `{角色名}说：`

不要为隔离分支复制另一份字符串拼装代码。

### 设计五：宿主附加消息保持独立

`additionalUserMessages` 是自动循环生成的宿主控制信息，不是聊天室公开历史。

首版建议继续在公开消息选择完成后追加这些控制信息，以免破坏最大发言次数仲裁等安全机制。当前正常的 mention 角色发言不会携带这类附加消息。

文档和测试应明确：开关限制的是公开聊天室上下文，不屏蔽宿主必须传达的运行控制消息。

## 新 Domain/Coordinator 设计

### 设计一：自动循环队列保存触发消息序号

将 `_autoLoopQueue` 从 `LinkedList<string>` 调整为保存：

```text
QueuedAutoLoopRole
  - string RoleId
  - long? MentionTriggerMessageSequence
```

规则与 legacy 一致：

- mention 入队保存消息序号
- 默认参与和管理者兜底使用 null
- 同一角色被新 mention 时，用更晚的消息序号替换旧队列项

优先使用序号而不是对象引用，便于从不可变 `ChatRoomState.Messages` 精确解析，并与快照、测试和执行请求保持一致。

`HandleAppendHumanMessage` 和 `HandleCompleteExecution` 已经拥有刚提交的 `ChatRoomMessage`，应直接用其 `MentionedRoleIds` 和 `MessageSequence` 入队，不再次只传原始文本。

### 设计二：执行请求区分可见消息和消费高水位

当前 `ChatRoomRoleExecutionRequest` 要求：

```text
InputMessages 最后一条序号 == InputThroughSequence
```

上下文隔离后，可见输入可能只有序号 20 的 mention 消息，但角色开始执行时聊天室已经提交到序号 23。序号 21 至 23 应被策略性忽略，而不是留到以后突然补发。

因此需要重新定义：

- `InputMessages`：本轮允许模型看到的公开消息子集
- `InputThroughSequence`：本轮已评估并在成功提交后视为已消费的公开消息高水位

验证规则调整为：

1. `InputMessages` 按序号严格递增
2. 每条输入消息序号不大于 `InputThroughSequence`
3. 非空输入的最后序号可以小于 `InputThroughSequence`
4. 输入消息必须来自当前房间快照

隔离模式下：

```text
InputMessages = [MentionTriggerMessage]
InputThroughSequence = 当前执行开始时的最新消息序号
```

成功提交后，角色消费水位直接推进到高水位，保证被隐藏的旧消息以后不会重新出现。

失败或取消时仍不提交候选 checkpoint，消费水位保持不变。

### 设计三：Coordinator 精确选择输入

`StartExecutionCore` 增加内部 mention 触发序号参数。

满足生效条件时：

1. 从 `_state.Messages` 按序号取得触发消息
2. 验证其 `MentionedRoleIds` 包含目标 RoleId
3. 验证触发序号大于角色当前消费水位
4. 只把该消息放入 `InputMessages`
5. 将当前房间消息高水位放入 `InputThroughSequence`

若触发消息已被消费，返回 NoOp，避免重启自动循环时重复处理同一 mention。

非隔离模式继续传递消费水位之后的全部消息。

### 设计四：Runtime 不自行扫描或裁剪历史

`IsolatedChatRoomRoleRuntime` 应信任 `ChatRoomRoleExecutionRequest.InputMessages` 已经是协调器批准的可见子集，只负责：

- 跳过系统消息和自身消息
- 按统一格式转换文本
- 执行角色
- 让候选 checkpoint 使用请求的 `InputThroughSequence`

不要在 Runtime 中再次根据 `ParticipationMode` 扫描完整历史。调度触发和消费水位属于 Coordinator 的职责。

## 数据模型与持久化

### Legacy 角色定义

在 `AgentLib.ChatRoom.Model.ChatRoomRoleDefinition` 增加：

```text
public bool OnlyReadLatestMentionMessage { get; set; }
```

默认 `false`。

### Domain 角色定义

在不可变 `AgentLib.ChatRoom.Domain.ChatRoomRoleDefinition` 中增加：

- 构造参数
- 只读属性
- 深拷贝传播
- legacy Runtime 映射传播

建议将可选构造参数放在 `participationMode` 附近，并使用命名参数更新关键调用，避免位置参数误传。

该字段只影响 Coordinator 的调度和输入选择，不改变模型、提示词、工具或 AgentSession 序列化格式，因此属于“编排策略事实”，不是“角色运行时事实”。单独修改该字段时不得递增 `RuntimeVersion`，也不得替换角色 Runtime。

### Legacy 会话和模板 JSON

`System.Text.Json` 会自动序列化新增布尔属性。旧 JSON 缺少字段时使用 false，不需要单独迁移文件。

必须更新：

- `RoleTemplateService.ToDefinition`
- `RoleTemplateService.FromDefinition`
- `RoleTemplateService.UpdateFromDefinition`
- 编程助手运行时模板
- 角色管理工具创建和编辑逻辑

### 新 Snapshot 存储

在 `StoredChatRoomRoleDefinition` 和 `ChatRoomSnapshotMapper` 中增加字段。

建议将 snapshot schema 从 2 升到 3，并提供 2 → 3 兼容读取：

- schema 2 缺失字段按 false
- schema 3 正常读取字段
- 未知更高版本继续拒绝

不要仅修改 `CurrentSchemaVersion` 后让全部旧 snapshot 无法加载。

## 更新 API

### ChatRoomManager

扩展 `UpdateRoleAsync` 参数，并在原位更新失败回滚时保存和恢复旧值：

```text
OnlyReadLatestMentionMessage
```

更新开关不得替换：

- `ChatRoomRole` 实例
- 执行器
- CodingAgent
- 当前 AgentSession
- 工作区资源

### ChatRoomService

同步扩展转发参数。

### 新 Coordinator 的策略更新

当前 `UpdateRoleCommand` 要求递增 `RuntimeVersion`，随后替换 Runtime、删除 committed checkpoint，并把消费水位重置为 0。若直接用该路径修改本开关，会违背“切换开关不清空角色私有 AgentSession”的目标。

建议增加专用的编排策略更新命令，或在 `HandleUpdateRoleAsync` 中显式区分字段差异：

```text
只修改 ParticipationMode / IsManagerRole / OnlyReadLatestMentionMessage
  → 保持 Identity、ExecutionKind、RuntimeVersion
  → 不调用 RuntimeRegistry.ReplaceAsync
  → 保留 committed checkpoint
  → 保留 ConsumedThroughSequence

修改模型、系统提示词、执行种类等运行时事实
  → 继续要求 RuntimeVersion 递增
  → 按现有规则替换 Runtime 和处理 checkpoint
```

首版至少必须为 `OnlyReadLatestMentionMessage` 提供不重置 Runtime/checkpoint 的更新路径。若同时把已有 `ParticipationMode` 和 `IsManagerRole` 收敛为编排策略，应补齐相应回归测试，不能顺带改变其他字段的更新语义而不验证。

### ChatRoomRoleManagementTools

建议同步更新：

- `list_characters`：增加“Mention 上下文”列
- `create_character`：接受可选 `onlyReadLatestMentionMessage`；未指定时对新 MentionOnly 角色默认 true
- `edit_character`：接受可选开关并原位更新

工具描述应明确该开关不清空角色已有私有记忆。

## Avalonia 界面

### RoleEditViewModel

新增属性：

```text
bool OnlyReadLatestMentionMessage
bool CanConfigureMentionMessageIsolation
```

`CanConfigureMentionMessageIsolation` 的建议条件：

```text
!IsHuman && ParticipationMode == “仅被 @ 时发言”
```

当 `IsHuman` 或 `ParticipationMode` 变化时，通知该属性变化。

加载已有角色时读取配置；保存已有角色和创建新角色时都传递配置。

新建角色的建议默认规则：

- 选择“仅被 @ 时发言”时默认勾选
- 选择“始终参与”时控件禁用但保留当前值
- 用户可以切回 MentionOnly 后继续使用原偏好

### RoleEditView.axaml

在“参与模式”卡片内增加复选框：

```text
被 @ 时仅读取触发消息
```

并添加简短说明：

```text
不会补发其他聊天室历史；不会清空该角色自己的对话记忆。
```

控件只对非人类 MentionOnly 角色启用。不要把它放到全局聊天室设置，因为这是角色级策略。

### 角色列表和角色大厅

建议但非强制：

- `RoleItemViewModel.ParticipationModeDisplay` 显示“仅被 @ / 仅触发消息”
- 模板卡片可显示该上下文策略

角色大厅现有模板编辑页并未开放完整参与模式配置，因此本轮至少保证模板提升、复制和再次加入会话时字段不丢失；是否在大厅编辑面板新增开关可作为同一实施中的展示增强。

## BuildChatRoomContext 提示词

角色首次发言时的聊天室协作说明应反映配置：

- 对开启隔离的角色说明：本轮只会收到直接触发自己的 mention 消息
- 不声称该角色已经获得完整聊天室历史
- 提醒角色如信息不足，应在回复中询问用户或 @ 其他角色，而不是假设缺失上下文

该说明只影响 Standard 执行器。CodingAgent 使用固定编程提示词，因此其隔离语义主要由实际输入集合保证，不依赖提示词。

## 边界场景

### 新角色加入旧聊天室

1. 角色加入时不读取历史
2. 加载旧数据时，只按恢复出的当时角色快照一次性回填缺失的结构化 mention
3. 运行期间新增角色后，不使用新角色名称重新解释历史消息
4. 旧消息中即使文本出现同名 @，只要没有该新角色的结构化 RoleId，就不应自动触发新角色
5. 新消息明确 mention 后，只发送该新消息

### 角色改名

使用 `MentionedRoleIds` 判断触发，不根据当前名称重新解释已经结构化的历史消息。这样改名不会改变旧消息的 mention 归属。

### 自己 mention 自己

沿用现有“不让同一角色连续发言”和“跳过自身消息”规则。隔离开关不应让角色把自己的公开消息重新当作 User 输入。

### MentionOnly 管理者兜底

管理者没有被 mention 而因空闲规则介入时，使用现有增量上下文；不能错误复用该管理者很久以前的 mention。

### 空回复

- legacy：保持当前循环终止/管理者介入行为
- 新 Coordinator：若角色执行成功并产生有效 checkpoint，即使公开文本为空，也提交消费高水位，避免同一 mention 被反复处理

### 失败和取消

- 不提交消费高水位
- 不修改角色私有 checkpoint
- 当前 legacy 自动循环不自动重试失败角色
- 若宿主以后重试同一执行，必须携带同一触发消息身份
- 用户发送新的 mention 时，以新消息替换旧触发

### 开关运行中修改

本轮执行使用启动时的角色定义和触发消息快照。配置修改只影响后续执行，不热改正在运行的模型输入。

### 从 true 改为 false

已经被隔离策略跳过并成功消费的公开消息不补发。关闭开关只影响未来未消费消息，避免一次配置切换突然注入大量旧历史。

Legacy 路径没有独立消费序号，实施时至少要保证下一次 mention 仍按精确触发消息执行；若要求与新 Coordinator 完全一致地记录“隐藏但已消费”的消息，应另行将 legacy 时间戳游标升级为显式序号水位，而不是把额外状态塞进角色定义。

## 需要修改的核心文件

### Legacy 模型和执行

- `SemanticKernelSamples/AgentLib/AgentLib.ChatRoom/Model/ChatRoomRoleDefinition.cs`
- `SemanticKernelSamples/AgentLib/AgentLib.ChatRoom/ChatRoomManager.ChatRoomAutoLoopRunner.cs`
- `SemanticKernelSamples/AgentLib/AgentLib.ChatRoom/ChatRoomManager.cs`
- `SemanticKernelSamples/AgentLib/AgentLib.ChatRoom/Services/ChatRoomService.cs`
- `SemanticKernelSamples/AgentLib/AgentLib.ChatRoom/Tools/ChatRoomRoleManagementTools.cs`
- `SemanticKernelSamples/AgentLib/AgentLib.ChatRoom/Services/CodingAssistantRoleFactory.cs`
- `SemanticKernelSamples/AgentLib/AgentLib.ChatRoom/Services/RoleTemplateService.cs`

### 新 Domain/Coordinator 架构

- `SemanticKernelSamples/AgentLib/AgentLib.ChatRoom/Domain/ChatRoomRoleDefinition.cs`
- `SemanticKernelSamples/AgentLib/AgentLib.ChatRoom/Domain/ChatRoomSnapshot.cs`
- `SemanticKernelSamples/AgentLib/AgentLib.ChatRoom/Coordination/ChatRoomCoordinator.cs`
- `SemanticKernelSamples/AgentLib/AgentLib.ChatRoom/Coordination/ChatRoomCommand.cs`
- `SemanticKernelSamples/AgentLib/AgentLib.ChatRoom/Runtime/IChatRoomRoleRuntime.cs`
- `SemanticKernelSamples/AgentLib/AgentLib.ChatRoom/Runtime/IsolatedChatRoomRoleRuntime.cs`
- `SemanticKernelSamples/AgentLib/AgentLib.ChatRoom/Persistence/StoredChatRoomSnapshot.cs`
- `SemanticKernelSamples/AgentLib/AgentLib.ChatRoom/Persistence/ChatRoomSnapshotMapper.cs`

### Avalonia

- `SemanticKernelSamples/ChatRoom/Code/ChatRoom.AvaloniaShell/ViewModels/RoleEditViewModel.cs`
- `SemanticKernelSamples/ChatRoom/Code/ChatRoom.AvaloniaShell/Views/RoleEditView.axaml`
- 可选：`ViewModels/RoleListViewModel.cs`
- 可选：角色大厅卡片和模板编辑展示

## 测试计划

### Legacy 消息选择测试

1. 开关关闭时，新 MentionOnly 角色仍得到现有完整增量上下文
2. 开关开启时，新角色第一次被 mention 只得到触发消息
3. 角色上次发言后存在多条无关消息和一条 mention，只得到 mention 消息
4. mention 后又出现无关消息、角色随后才执行，仍只得到调度器保存的触发消息
5. 同一角色排队期间再次被 mention，最终只得到更新后的最新触发消息
6. 同一条消息 mention 多个角色，每个角色都得到该完整消息
7. 开关开启但角色为 AlwaysParticipate 时保持完整增量行为
8. 手动 `StepAsync` 不使用旧 mention 裁剪
9. MentionOnly 管理者兜底不使用旧 mention 裁剪
10. 角色改名后结构化 RoleId 仍能定位触发消息
11. 自我 mention 不把自身消息作为 User 输入
12. additional host messages 仍按设计保留

### 私有历史测试

1. 第一次 mention 后形成角色私有 AgentSession
2. 第二次 mention 时本轮新增输入只有第二条触发消息
3. 角色私有历史仍包含第一次 mention 和第一次回答
4. 切换开关不自动清空 AgentSession
5. 保存和恢复会话后继续保持同一行为

### 配置和持久化测试

1. legacy 会话 JSON 往返保留开关
2. 缺失字段的旧 JSON 读取为 false
3. 角色模板 To/From/Update 全部复制开关
4. 编程助手新定义采用约定默认值
5. 动态创建角色采用约定默认值
6. 原位编辑失败时回滚开关
7. Snapshot schema 2 迁移为 false
8. Snapshot schema 3 往返保留 true
9. 深拷贝和 legacy Runtime 映射不丢字段
10. 只修改该编排策略时，新 Coordinator 保留 RuntimeVersion、checkpoint 和消费水位

### Avalonia 测试

1. 编辑 MentionOnly 角色时正确加载开关
2. 保存后原位更新且不替换 Coding 执行器或 AgentSession
3. AlwaysParticipate 时开关禁用但值保留
4. 人类角色不显示或不能编辑该开关
5. 新建 MentionOnly 角色使用约定默认值
6. 错误保存时显示原有错误信息并保持页面状态

### 新 Coordinator 测试

1. mention 队列保存触发消息序号
2. 新 mention 替换同角色旧触发序号
3. 隔离执行请求只有一条可见消息
4. `InputThroughSequence` 可以大于最后一条可见消息序号
5. 成功提交后消费水位推进到执行高水位
6. 隐藏消息不会在下一轮补发
7. 失败和取消不推进消费水位
8. 已消费的旧 mention 重启自动循环时返回 NoOp
9. 默认队列和管理者兜底仍使用 null 触发
10. checkpoint 候选继续使用请求的消费高水位
11. 策略更新不替换 Runtime、不清除 checkpoint、不重置消费水位

## 分步实施计划

1. 在 legacy 和 Domain 角色定义中增加 `OnlyReadLatestMentionMessage`，默认 false
2. 更新角色定义复制、深拷贝、legacy 映射和模板转换
3. 更新 legacy 会话持久化测试，并为 snapshot schema 增加兼容迁移
4. 将 legacy 自动循环队列改为携带 mention 触发消息的队列项
5. 修改 legacy 消息构建，只在满足三个生效条件时选择精确触发消息
6. 将新 Coordinator 自动循环队列改为携带 mention 触发消息序号
7. 重定义新执行请求的可见消息与消费高水位不变量
8. 修改 Coordinator 的隔离输入选择、成功消费和失败保留逻辑
9. 修改 Isolated Runtime，使其只格式化 Coordinator 已批准的消息子集
10. 为新 Coordinator 增加不重置 Runtime、checkpoint 和消费水位的编排策略更新路径
11. 扩展 ChatRoomManager、ChatRoomService 和角色管理工具的配置读写
12. 在 Avalonia 角色编辑页增加开关、启用条件和说明文本
13. 更新新 MentionOnly 角色、动态角色和编程助手的默认配置
14. 添加 legacy、Coordinator、持久化、模板、UI 和 AgentSession 回归测试
15. 运行相关测试与完整构建，并审查新旧架构行为是否一致

## 验收标准

1. 新加入的隔离型 MentionOnly 角色第一次被 @ 时不会读取加入前历史
2. 角色每次 mention 触发只获得调度器对应的最新触发消息
3. 其他未 mention 的公开消息不会进入该轮模型输入
4. 角色自己的既有 AgentSession 不会被清空
5. 开关关闭、AlwaysParticipate、手动 Step 和管理者兜底行为保持现状
6. 多次 mention、多人同条 mention、角色改名和持久化恢复均使用稳定 RoleId/消息序号定位
7. 新 Coordinator 在隐藏消息后正确推进成功消费水位，失败或取消不推进
8. 配置可在 Avalonia 角色编辑页查看和修改
9. 会话、模板和 snapshot 持久化不会丢失配置，旧数据保持 false
10. Standard 与 Coding 角色均遵守相同的公开消息选择策略
11. 修改该开关不会清空或替换新 Coordinator 中既有的角色 Runtime 和私有 checkpoint
12. legacy 和新 Domain/Coordinator 路径测试均通过
