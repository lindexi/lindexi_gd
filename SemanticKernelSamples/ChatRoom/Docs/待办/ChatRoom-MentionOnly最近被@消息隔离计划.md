# ChatRoom MentionOnly 最近被 @ 消息隔离计划

## 文档定位

本文为 `MentionOnly` 参与模式增加一个独立的公开消息隔离开关：

> 开启后，角色在一次由 @mention 触发的发言中，只接收本次实际触发它的最新一条公开消息，不接收该角色此前尚未发言期间积累的其他聊天室消息。

该能力主要解决：

- 新角色加入已有大量历史的聊天室
- 新角色第一次被 @ 时不应读取加入前的全部历史
- 专业角色只需要处理明确指派给自己的任务
- MentionOnly 角色长时间未参与后，不应在下一次发言时获得大量无关上下文

本文按当前 `ChatRoomManager` 扁平架构编写，只给出计划，不实现代码，也不恢复已经清理的 `Domain`、`Coordination`、`Runtime`、Snapshot 或消费水位架构。

## 术语与配置名

建议在 `ChatRoomRoleDefinition` 增加：

```text
OnlyReadLatestMentionMessage
```

中文界面名称：

```text
被 @ 时仅读取触发消息
```

说明文本：

```text
开启后，该角色由 @ 提及触发发言时，只读取本次直接 @ 它的最新消息，不补发其他聊天室历史。角色已有的私有对话记忆不会被清空。
```

## 当前代码事实

### 角色参与模式已存在，消息隔离开关尚不存在

`AgentLib.ChatRoom/Model/ChatRoomRoleDefinition.cs` 当前包含：

- `ChatRoomRoleExecutionKind ExecutionKind`
- `bool IsHuman`
- `ChatRoomParticipationMode ParticipationMode`
- `bool IsManagerRole`

其中 `MentionOnly` 只决定角色何时进入自动调度，不改变角色本次可以看到的公开消息范围。

当前没有 `OnlyReadLatestMentionMessage` 或等价字段。

### 当前增量范围由“上次公开发言时间”决定

`ChatRoomSession.GetMessagesSinceLastSpeak(roleId)` 当前规则是：

- 角色已经发言过：返回其上次公开发言时间之后的所有消息
- 角色从未发言过：返回聊天室中的全部公开消息

`ChatRoomManager.ChatRoomAutoLoopRunner.BuildIncrementalUserMessages` 随后过滤系统消息、预设信息和当前角色自身消息，再格式化其余消息。

因此：

- 新加入角色第一次被 @ 时会读取加入前的全部公开历史
- 长期沉默的 MentionOnly 角色会读取上次发言后的全部公开消息
- `ParticipationMode == MentionOnly` 并不会限制输入到触发消息

### 当前优先栈只保存角色

`RunAutoLoopTurnAsync` 当前使用：

```text
Stack<ChatRoomRole> priorityRoles
Queue<ChatRoomRole> defaultRoles
```

`PushMentionedRoles` 只接收 `mentionedRoleIds`，找到对应 `ChatRoomRole` 后压入优先栈。实际产生 mention 的 `ChatRoomMessage` 身份在入栈后丢失。

现有重复 mention 规则是：

- 同一条消息中的重复 RoleId 去重
- 若角色已在优先栈，先移除旧候选
- 新 mention 将角色重新压入栈顶，获得更高优先级

该行为已经表达“后出现的 mention 刷新角色优先级”，但没有保留“由哪一条消息刷新”。

### 结构化 Mention 已存在，但兼容回退仍会动态重新解释历史

`ChatRoomMessage.MentionedRoleIds` 已作为公开消息字段参与持久化：

- 人类消息在 `ChatRoomManager.HumanInterjectAsync` 追加前解析
- AI 消息在 `HandleAutoLoopMessageAsync` 完成后解析
- `GetMentionedRoleIds` 优先使用结构化 RoleId
- 当 `MentionedRoleIds` 为空时，使用当前 `Roles` 再次调用 `MentionParser.ParseMentions`

这为已经解析出 mention 的消息提供稳定 RoleId，但空列表同时表示“没有 mention”和“旧数据未保存结构化结果”，当前模型无法区分二者。因此兼容回退仍存在以下边界：

- 新角色加入后，历史文本可能按新的当前角色列表被重新解释
- 角色改名后，缺少结构化 RoleId 的旧消息可能无法按原名称解析
- 不能在角色执行时通过扫描历史并重新解析文本来证明本次调度来源

### Standard 与 Coding 已共用公开消息选择结果

公开消息由 `BuildIncrementalUserMessages` 统一选择和格式化，`ChatRoomRole.SpeakAsync` 再转换为 `TextContent`。

因此隔离应在 `ChatRoomManager` 的消息选择层实现。`StandardChatRoomRoleExecutor` 与 `CodingChatRoomRoleExecutor` 不应各自增加历史裁剪逻辑。

### 角色私有 AgentSession 已独立持久化

每个非人类角色的内部 AgentSession 由 `ChatRoomRole` 和 `ChatRoomPersistence` 独立保存、恢复。

本需求只改变“本轮新增公开消息”的选择，不应调用：

- `ChatRoomRole.ClearSessionMemory`
- `CopilotChatManager.CreateNewSession`
- 角色执行器替换
- AgentSession 状态删除

## 目标与非目标

### 目标

1. 给 MentionOnly 角色增加可持久化的独立隔离开关
2. 只在本次发言由明确 mention 消息触发时启用隔离
3. 精确保留触发消息身份，不在执行阶段猜测历史 mention
4. 同一角色排队期间收到新 mention 时，以最新触发消息覆盖旧触发
5. Standard 与 Coding 使用同一公开消息选择规则
6. 保留角色已有私有 AgentSession
7. 在会话、模板、动态创建、编程助手和 Avalonia 角色编辑页中完整传播配置
8. 旧数据缺失字段时保持现有行为

### 非目标

本文不处理：

- 每次 mention 使用全新 AgentSession
- 全局聊天室消息删除或隐藏
- 角色之间真正隔离公共日志存储
- 将时间戳游标升级为全局消息序号消费水位
- 恢复新 Coordinator 或 Snapshot schema
- 改写 Mention 文本语法
- 解决空 `MentionedRoleIds` 无法区分“已解析为空”和“旧数据未解析”的兼容模型
- 修改自动发言优先级、最大发言次数或管理者仲裁规则

## 与 ParticipationMode 的关系

该字段是独立配置，不新增第三个参与模式。

生效条件必须同时满足：

1. `role.Definition.ParticipationMode == ChatRoomParticipationMode.MentionOnly`
2. `role.Definition.OnlyReadLatestMentionMessage == true`
3. 本次发言由自动调度中的明确 mention 队列项触发
4. 队列项携带的 `MentionTriggerMessage.MentionedRoleIds` 包含当前 `RoleId`

以下情况不应用隔离：

- `AlwaysParticipate` 角色正常进入默认队列
- 外部代码调用公开 `ChatRoomManager.StepAsync(role)`
- MentionOnly 管理者因无普通角色可执行而兜底发言
- 达到最大发言次数后由管理者仲裁
- 其他没有明确 mention 触发消息的宿主调用

概念边界为：

- `ParticipationMode` 决定何时参与
- `OnlyReadLatestMentionMessage` 决定 mention 触发时本轮可见哪些公开消息

## 默认值策略

### 已有角色和旧数据

属性默认值为 `false`。

旧会话和旧模板 JSON 不包含该字段时，`System.Text.Json` 会使用 `false`，保持现有完整增量行为，不需要单独的数据迁移文件或 schema 版本。

### 新建 MentionOnly 角色

建议以下新角色默认开启：

- `ChatRoomRoleManagementTools.create_character` 动态创建的角色
- `CodingAssistantRoleFactory.CreateDefinition` 创建的编程助手
- Avalonia 角色编辑页中新建且选择 `MentionOnly` 的 AI 角色

`AlwaysParticipate` 新角色默认关闭。

用户将已开启选项的角色切换为 `AlwaysParticipate` 时，建议保留字段值但使其暂时不生效；切回 `MentionOnly` 后恢复原偏好。

## 目标行为

### 开关关闭

行为完全保持现状：

```text
本轮公开消息 = GetMessagesSinceLastSpeak(roleId) 返回的完整增量范围
```

### 开关开启且由 mention 触发

```text
本轮公开消息 = 触发当前队列项的那一条 ChatRoomMessage
```

应保留整条消息，包括：

- @ 当前角色之前和之后的文本
- 同一条消息中对其他角色的 @mention
- 原始发送者身份
- 原始完整公开内容

不要只截取 `@角色名` 后面的文本片段。

### 开关开启但本次不是 mention 触发

继续使用当前完整增量行为，不扫描历史寻找旧 mention。

这适用于：

- 手动 `StepAsync`
- 管理者兜底
- 宿主直接要求角色发言

### 多次 mention

同一角色尚未发言时又被新消息 mention：

- 队列中只保留该角色一次
- 使用新队列项覆盖旧队列项
- 新队列项保存最新 `MentionTriggerMessage`
- 角色最终只读取最新触发消息

同一条消息多次 mention 同一角色时，只产生一次调度，角色读取整条消息一次。

### 一条消息 mention 多个角色

每个角色的队列项都引用同一条触发消息，各自在发言时按自身配置决定是否隔离。

消息表达仍由 `ChatRoomIncrementalMessageFormatter` 决定：

- 单 AI + 人类 @：使用原文
- 多 AI + 人类 @：使用 `用户说：...`
- AI @：使用 `{角色名}说：...`

## 核心设计

### 1. 增加角色级配置

在 `ChatRoomRoleDefinition` 增加：

```text
public bool OnlyReadLatestMentionMessage { get; set; }
```

默认 `false`。

该字段只影响编排时的公开消息选择，不改变：

- `ExecutionKind`
- 模型绑定
- 角色系统提示词内容本身
- 工作区
- 工具
- 私有 AgentSession

### 2. 优先队列项保存角色与触发消息

建议增加 `ChatRoomAutoLoopRunner` 内部不可变队列项：

```text
QueuedRoleTurn
  - ChatRoomRole Role
  - ChatRoomMessage? MentionTriggerMessage
```

只需要将以下优先调度结构改为保存队列项：

- `priorityRoles`
- 优先候选的暂缓列表

`defaultRoles` 继续保存 `ChatRoomRole`。默认队列从不携带 mention 触发消息，没有必要为了类型统一扩大修改范围。`TryDequeueNextSpeaker` 可以统一返回一个临时 `QueuedRoleTurn`：优先候选返回保存的队列项，默认候选返回 `QueuedRoleTurn(role, null)`。

队列项规则：

- mention 触发：保存具体 `ChatRoomMessage`
- 默认参与：`MentionTriggerMessage = null`
- 管理者兜底：直接构造无触发消息的本轮执行，不复用旧队列项
- 手动 `StepAsync(role)`：传 `null`

`lastSpeakerRoleId`、`speakCounts`、去重和移除仍按 `RoleId` 工作。

### 3. 所有 mention 入队入口传递消息对象

初始触发：

```text
EnqueueInitialRoles(triggerMessage)
  → PushMentionedRoles(priorityRoles, mentionedRoleIds, triggerMessage)
```

后续 AI 消息：

```text
HandleAutoLoopMessageAsync(message)
  → 返回结构化 mentionedRoleIds
  → PushMentionedRoles(priorityRoles, mentionedRoleIds, message)
```

不要只传 RoleId 后丢失消息身份。

### 4. 私有 StepAsync 接收触发上下文

建议将私有入口调整为：

```text
StepAsync(
  ChatRoomRole role,
  ChatRoomMessage? mentionTriggerMessage,
  IReadOnlyList<string>? additionalUserMessages,
  CancellationToken cancellationToken)
```

公开 `ChatRoomManager.StepAsync(role)` 继续传 `null`。

`RunAutoLoopTurnAsync` 从 `QueuedRoleTurn` 取出角色和触发消息，并将二者一起传入。

管理者兜底与最大发言次数仲裁均传 `null`，避免管理者很久以前被 mention 的消息影响本次输入。

### 5. 在消息选择阶段启用隔离

`BuildIncrementalUserMessages` 接收 `mentionTriggerMessage` 后先选择公开消息集合：

```text
if role.ParticipationMode == MentionOnly
   && role.OnlyReadLatestMentionMessage
   && mentionTriggerMessage != null
   && mentionTriggerMessage.MentionedRoleIds 包含 role.RoleId
then
   publicMessages = [mentionTriggerMessage]
else
   publicMessages = Session.GetMessagesSinceLastSpeak(role.RoleId)
```

然后沿用现有统一流程：

1. 跳过系统消息
2. 跳过预设信息
3. 跳过当前角色自己的消息
4. 使用 `ChatRoomIncrementalMessageFormatter.Format`
5. 追加 `additionalUserMessages`

隔离分支不能复制另一套过滤和格式化代码。

### 6. 对触发消息执行不变量校验

调度器只有在消息明确 mention 当前角色时才能创建带触发消息的队列项。

如果内部调用传入非空 `mentionTriggerMessage`，但结构化 `MentionedRoleIds` 不包含目标 RoleId，应抛出或通过调试断言暴露编排错误，不应退化为“扫描全部历史找到最近一次 mention”。

创建初始队列项时可以沿用当前 `GetMentionedRoleIds(triggerMessage)` 兼容回退；若回退解析出非空结果，应写回 `message.MentionedRoleIds`。执行阶段只消费已经确定的队列项和触发消息，不再扫描其他历史。

### 7. 保持兼容回退范围最小

首版不扩展 `ChatRoomMessage` 的持久化模型，也不增加“是否已经解析 mention”的额外标记。现有兼容回退只应发生在某条消息实际作为 trigger 进入调度时：

1. 优先读取 `MentionedRoleIds`
2. 字段为空时执行一次 `MentionParser.ParseMentions`
3. 解析出非空结果时写回 `message.MentionedRoleIds`
4. 本轮后续队列和执行只使用已经确定的结果

由于空列表没有“已解析”标记，首版不能承诺旧消息在未来每次加载或再次成为 trigger 时永远按同一角色快照解释。该问题属于 Mention 历史兼容模型，不应为本隔离需求顺带引入新 schema。隔离功能只要求：角色一旦因某条消息进入队列，本轮执行必须使用该精确消息，而不是再搜索历史。

### 8. 宿主附加消息保持独立

`additionalUserMessages` 是自动循环产生的控制信息，不是聊天室公开历史。

隔离只限制公开消息集合，宿主附加消息仍在其后原样追加，以保持最大发言次数仲裁等安全机制。

正常 mention 角色发言通常不带附加消息，但实现和测试应固定该边界。

## 私有 AgentSession 语义

本开关不清空角色私有 AgentSession。

因此：

- 新加入角色第一次被 @ 时，只收到触发消息
- 角色回答后，其触发消息和回答进入该角色自己的私有会话
- 第二次被 @ 时，本轮新增公开输入只包含第二条触发消息
- 模型仍可通过自己的私有历史记住第一次任务和回答
- 角色不会自动获得两次 mention 之间从未发送给它的公共消息
- 切换开关不调用 `ClearSessionMemory`

如果未来需要“每次 mention 完全无状态”，应设计独立功能，不能复用本开关。

## 配置传播与持久化

### 会话持久化

`ChatRoomRoleDefinition` 已由 `ChatRoomSessionData.Roles` 和源生成 JSON 上下文整体序列化。新增普通布尔属性后：

- 新配置自动写入 `room.config.json`
- 旧 JSON 缺失字段时为 `false`
- 不需要增加 schema 版本

需要补充显式往返与旧 JSON 缺失字段测试。

### 角色模板

`RoleTemplateService` 手工复制角色定义，必须在以下方法中传播字段：

- `ToDefinition`
- `FromDefinition`
- `UpdateFromDefinition`

否则角色提升到大厅、从大厅加入会话或编辑模板后会丢失配置。

角色大厅当前只编辑模板名称、描述、分类、标签和系统提示词。本轮至少保证字段在未编辑的情况下不丢失；是否在大厅编辑面板直接开放此开关可作为后续增强，不作为首版阻塞项。

### 编程助手默认值

`CodingAssistantRoleFactory.CreateDefinition` 当前创建 `MentionOnly` Coding 角色。建议同时设置：

```text
OnlyReadLatestMentionMessage = true
```

对应运行时模板会自然携带该值。

### 动态角色工具

`ChatRoomRoleManagementTools` 建议：

- `list_characters` 增加“Mention 上下文”列
- `create_character` 接受可选 `onlyReadLatestMentionMessage`
- 参数未指定时，动态创建的 MentionOnly 角色默认 `true`
- `edit_character` 接受可选开关，并通过原位更新 API 保存
- 工具说明明确切换开关不会清空角色私有记忆

### 原位更新 API

扩展：

- `ChatRoomManager.UpdateRoleAsync`
- `ChatRoomService.UpdateRoleAsync`

更新与回滚逻辑必须保存旧值和新值。

修改该开关不得替换：

- `ChatRoomRole` 实例
- `IChatRoomRoleExecutor`
- `CodingAgent`
- 工作区状态
- AgentSession

现有原位编辑测试已经验证执行器与运行时身份保留，可在其基础上增加该字段断言。

## Avalonia 角色编辑页

### `RoleEditViewModel`

新增：

```text
bool OnlyReadLatestMentionMessage
bool CanConfigureMentionMessageIsolation
```

建议启用条件：

```text
!IsHuman && ParticipationMode == “仅被 @ 时发言”
```

要求：

- 加载已有角色时读取字段
- 编辑保存时传递字段
- 新建角色时写入字段
- 新建并选择 MentionOnly 时默认勾选
- 切换为 AlwaysParticipate 时禁用控件但保留值
- `IsHuman` 或 `ParticipationMode` 变化时通知启用状态

### `RoleEditView.axaml`

在“参与模式”卡片中增加复选框：

```text
被 @ 时仅读取触发消息
```

说明文本：

```text
不会补发其他聊天室历史；不会清空该角色自己的对话记忆。
```

控件仅对非人类 MentionOnly 角色启用。

### 角色列表

`RoleItemViewModel.ParticipationModeDisplay` 当前只显示“人类”或“AI 角色”，没有展示实际 `ParticipationMode`。

可选增强为：

- `始终参与`
- `仅被 @`
- `仅被 @ / 仅触发消息`

该展示不影响首版正确性，可与现有角色列表显示问题一起处理。

## `BuildChatRoomContext` 提示词

对开启隔离的角色，首次系统提示词应说明：

- mention 触发时，本轮可能只收到直接触发自己的公开消息
- 不应假设自己获得了完整聊天室历史
- 信息不足时，应询问用户或 @ 其他角色补充
- 已有私有 AgentSession 仍然保留

`BuildChatRoomContext` 当前由 `ChatRoomManager` 按房间构建，再赋给发言角色。若要生成角色特定说明，建议改为接收当前角色：

```text
BuildChatRoomContext(ChatRoomRole currentRole)
```

该说明只在角色首次构建系统提示词时注入；角色已经发言后再修改开关，不会自动重建既有 AgentSession 的系统提示词。因此提示词只能作为首次使用说明，正确性必须由实际输入集合保证。Coding 执行器同样不能依赖该提示词才能正确隔离。

## 边界场景

### 新角色加入旧聊天室

- 加入时不主动读取历史
- 第一次明确 mention 后，只收到触发消息
- 角色执行阶段不扫描历史寻找触发源
- 若旧消息本身作为自动循环 trigger，现有空 `MentionedRoleIds` 兼容回退仍可能按当前角色列表解释文本；冻结旧消息空 mention 结果不属于本计划

### 角色改名

已经保存非空结构化 `MentionedRoleIds` 的消息不受角色改名影响，仍按 RoleId 定位触发角色。

缺失结构化字段的旧消息只在本轮实际作为 trigger 进入调度时兼容解析，不在角色执行阶段反复解析。

### 自己 mention 自己

沿用现有禁止连续发言和跳过自身消息规则。隔离不能让角色把自己的公开消息作为 User 输入。

### MentionOnly 管理者兜底

管理者未被 mention、仅因空闲规则介入时，`MentionTriggerMessage` 必须为 `null`，使用现有完整增量上下文。

### 空回复、失败与取消

- 保持现有空回复、管理者介入和循环终止规则
- 队列项在角色被选择时即出队，当前扁平架构不增加自动重试状态
- 新 mention 到来时按现有规则创建新的最新队列项
- 不因失败或取消清空私有 AgentSession

### 运行中修改开关

本次已经开始的 `StepAsync` 使用启动时传入的角色配置和触发消息。配置修改只影响后续执行，不热改已经构造的模型输入。

### 从 `true` 改为 `false`

关闭开关后，下一次非 mention 手动执行或后续发言仍按当前时间戳增量逻辑获取消息。

当前时间戳水位由角色公开消息更新。隔离发言成功并提交角色公开回复后，回复之前被隔离跳过的旧消息通常已落在该角色新的上次发言时间之前，不会在后续增量中补发。若角色空回复、失败或取消，流式占位消息会被移除且上次发言时间不会推进，被跳过消息仍可能在后续手动 `StepAsync` 中重新出现。

首版验收范围应明确：

- mention 触发的隔离轮次只看到触发消息
- 保持当前成功回复推进时间戳、空回复或失败不推进的语义
- 不额外引入消息序号消费水位改造
- 若未来要求所有触发方式都永久跳过被隐藏消息，应另立消息序号/消费水位计划

## 影响文件

### 核心模型与执行

- `SemanticKernelSamples/AgentLib/AgentLib.ChatRoom/Model/ChatRoomRoleDefinition.cs`
- `SemanticKernelSamples/AgentLib/AgentLib.ChatRoom/ChatRoomManager.ChatRoomAutoLoopRunner.cs`
- `SemanticKernelSamples/AgentLib/AgentLib.ChatRoom/ChatRoomManager.cs`
- `SemanticKernelSamples/AgentLib/AgentLib.ChatRoom/Services/ChatRoomService.cs`

### 配置入口与复制

- `SemanticKernelSamples/AgentLib/AgentLib.ChatRoom/Tools/ChatRoomRoleManagementTools.cs`
- `SemanticKernelSamples/AgentLib/AgentLib.ChatRoom/Services/CodingAssistantRoleFactory.cs`
- `SemanticKernelSamples/AgentLib/AgentLib.ChatRoom/Services/RoleTemplateService.cs`

### Avalonia

- `SemanticKernelSamples/ChatRoom/Code/ChatRoom.AvaloniaShell/ViewModels/RoleEditViewModel.cs`
- `SemanticKernelSamples/ChatRoom/Code/ChatRoom.AvaloniaShell/Views/RoleEditView.axaml`
- 可选：`SemanticKernelSamples/ChatRoom/Code/ChatRoom.AvaloniaShell/ViewModels/RoleListViewModel.cs`

### 测试

- `SemanticKernelSamples/AgentLib/AgentLib.ChatRoom.Tests/ChatRoomManagerTests.cs`
- `SemanticKernelSamples/AgentLib/AgentLib.ChatRoom.Tests/ChatRoomManagerIntegrationTests.cs`
- `SemanticKernelSamples/AgentLib/AgentLib.ChatRoom.Tests/ChatRoomPersistenceTests.cs`
- `SemanticKernelSamples/AgentLib/AgentLib.ChatRoom.Tests/RoleTemplateServiceTests.cs`
- `SemanticKernelSamples/AgentLib/AgentLib.ChatRoom.Tests/CodingAssistantRoleFactoryTests.cs`
- `SemanticKernelSamples/AgentLib/AgentLib.ChatRoom.Tests/Tools/ChatRoomRoleManagementToolsTests.cs`
- `SemanticKernelSamples/ChatRoom/Code/ChatRoom.Shell.Tests/ChatRoomServiceTests.cs`

## 测试计划

### 公开消息选择

1. 开关关闭时，新 MentionOnly 角色仍得到现有完整增量上下文
2. 开关开启时，新角色第一次被 mention 只得到触发消息
3. 上次发言后存在多条无关消息和一条 mention 时，只得到 mention 消息
4. mention 后又出现无关消息、角色稍后执行时，仍只得到队列保存的触发消息
5. 同一角色排队期间再次被 mention，最终只得到最新触发消息
6. 同一条消息 mention 多个角色，每个角色都得到该完整消息
7. 开关开启但角色为 AlwaysParticipate 时保持完整增量行为
8. 手动 `StepAsync` 不使用历史旧 mention 裁剪
9. MentionOnly 管理者兜底不使用历史旧 mention 裁剪
10. 自我 mention 不把自身消息作为 User 输入
11. `additionalUserMessages` 仍原样追加
12. 隔离后继续复用单 AI、多 AI发送者格式

### 调度回归

1. 人类 `@A @B` 的发言顺序不变
2. 后续角色产生的 mention 仍优先于默认队列
3. 新 mention 覆盖同角色旧优先候选
4. A → B → A 链式调度保持可用
5. 禁止同角色连续发言保持可用
6. 最大发言次数与管理者仲裁保持可用
7. 空回复不会导致死循环

### 私有 AgentSession

1. 第一次 mention 后形成私有 AgentSession
2. 第二次 mention 的本轮新增输入只有第二条触发消息
3. 私有历史仍包含第一次 mention 与第一次回答
4. 切换开关不清空 AgentSession
5. 保存和恢复会话后，私有历史与隔离配置均保留
6. Coding 角色同样保留原执行器和 AgentSession

### 配置与持久化

1. 会话 JSON 往返保留 `true`
2. 旧 JSON 缺失字段读取为 `false`
3. `RoleTemplateService.ToDefinition` 复制字段
4. `RoleTemplateService.FromDefinition` 复制字段
5. `RoleTemplateService.UpdateFromDefinition` 复制字段
6. 编程助手新定义默认采用约定值
7. 动态创建角色默认采用约定值
8. 原位更新失败时回滚字段
9. 仅修改开关时保留角色实例、执行器和不可编辑元数据

### Avalonia

1. 编辑 MentionOnly 角色时正确加载开关
2. 保存后原位更新，且不替换 Coding 运行时身份
3. AlwaysParticipate 时控件禁用但值保留
4. 人类角色不能配置开关
5. 新建 MentionOnly 角色采用约定默认值
6. 保存失败时继续显示现有错误信息

## 分步实施计划

1. 在 `ChatRoomRoleDefinition` 增加配置并固定默认值
2. 扩展 `ChatRoomManager.UpdateRoleAsync` 与回滚逻辑
3. 扩展 `ChatRoomService.UpdateRoleAsync` 转发
4. 将自动循环优先队列改为携带 mention 触发消息的队列项
5. 让初始与后续 mention 入队均传递具体消息
6. 扩展私有 `StepAsync` 和 `BuildIncrementalUserMessages` 接收触发消息
7. 在统一消息选择层实现隔离，不复制过滤和格式化逻辑
8. 将缺失 `MentionedRoleIds` 的兼容解析限制在 trigger 入队阶段
9. 更新模板复制、动态角色工具和编程助手默认值
10. 在 Avalonia 角色编辑页增加开关与启用条件
11. 更新角色特定的聊天室上下文说明
12. 添加消息选择、调度、持久化、模板、工具、UI 和 AgentSession 测试
13. 运行 `AgentLib.ChatRoom.Tests`
14. 运行 `ChatRoom.Shell.Tests`
15. 运行完整解决方案构建并审查变更范围

## 验收标准

1. 开启隔离的新 MentionOnly 角色第一次被 @ 时不会读取加入前历史
2. 每次 mention 触发只获得调度器对应的最新触发消息
3. 未 mention 的公开消息不会进入该轮模型输入
4. 同一角色的新 mention 会覆盖旧触发消息
5. 手动 Step、AlwaysParticipate 和管理者兜底保持现有完整增量行为
6. 角色自己的既有 AgentSession 不会被清空或替换
7. Standard 与 Coding 使用同一公开消息选择策略
8. 单 AI、多 AI 和其他 AI 发送者格式继续由共享格式化器决定
9. 会话与模板持久化不会丢失配置，旧数据保持 `false`
10. Avalonia 可以查看和修改该角色级开关
11. 原位修改开关不替换角色实例、执行器、CodingAgent 或工作区状态
12. 现有 Mention 顺序、链式调用、防连续发言、管理者仲裁和会话恢复测试继续通过
13. 不引入新 Coordinator、Snapshot schema 或消费水位架构

## 与旧稿的差异

旧稿中以下内容仍然有效并被保留：

- 独立布尔开关
- 只在 MentionOnly 且明确 mention 触发时生效
- 队列项必须保存触发消息身份
- 新 mention 覆盖旧触发
- 私有 AgentSession 不清空
- 会话、模板、工具、编程助手和 Avalonia 配置传播

旧稿中以下内容已经不符合当前项目，应删除：

- `AgentLib.ChatRoom.Domain.ChatRoomRoleDefinition`
- `ChatRoomCoordinator` 自动循环队列
- `ChatRoomRoleExecutionRequest.InputMessages/InputThroughSequence`
- `IsolatedChatRoomRoleRuntime`
- `StoredChatRoomSnapshot` 与 schema 2 → 3 迁移
- RuntimeVersion、checkpoint 和 `ConsumedThroughSequenceByRole`

当前计划只围绕现有 `ChatRoomManager`、`ChatRoomSession`、角色定义、服务、工具、模板和 Avalonia 页面实施。

实施时应继续保持这一架构边界。
