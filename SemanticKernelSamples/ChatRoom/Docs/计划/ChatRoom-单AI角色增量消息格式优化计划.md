# ChatRoom 单 AI 角色增量消息格式优化计划

## 文档定位

本文只解决一个消息格式问题：

> 当聊天室当前只有一个非人类角色时，发送给该角色的人类公开消息直接使用原文，不再添加“用户说：”前缀；存在多个非人类角色时继续保留现有发送者说明。

本文只给出修改计划，不实现代码。

## 当前实现

### Legacy ChatRoomManager 路径

`ChatRoomManager.ChatRoomAutoLoopRunner.BuildIncrementalUserMessages` 当前执行以下流程：

1. 调用 `ChatRoomSession.GetMessagesSinceLastSpeak(roleId)` 获取该角色上次公开发言后的消息
2. 跳过系统消息
3. 跳过当前角色自己的消息
4. 人类消息格式化为 `用户说：{内容}`
5. 其他 AI 角色消息格式化为 `{角色名}说：{内容}`
6. 将内部协调产生的 `additionalUserMessages` 原样追加

随后 `ChatRoomRole.SpeakAsync` 把每个字符串转换为独立 `TextContent`，并以 `ChatRole.User` 输入当前角色的 AgentSession。

### 新 Domain/Coordinator 路径

仓库同时编译新的 `Domain`、`Coordination` 和 `Runtime` 架构。

`IsolatedChatRoomRoleRuntime.BuildIncrementalMessages` 复制了相同格式规则：

- 人类消息加 `用户说：`
- 其他角色消息加 `{角色名}说：`
- 系统和自身消息跳过

虽然 Avalonia 当前仍使用 legacy `ChatRoomService`，两条路径必须保持一致，否则未来切换宿主后会重新出现旧行为。

### 当前系统提示词也假设始终存在前缀

`BuildChatRoomContext` 当前告诉角色：

- 其他人类和非人类角色都作为 User 消息输入
- 身份通过“用户说：...”或“角色名说：...”表达

单 AI 角色改为直接发送人类原文后，这段说明也应按房间角色数量调整，否则系统提示词会描述模型实际上看不到的格式。

## 问题

在只有一个 AI 角色的聊天室中：

- 人类消息天然就是该角色当前要处理的用户输入
- AgentFramework 已经把消息标记为 `ChatRole.User`
- 不存在另一个 AI 角色需要通过文本标签消歧
- 每条消息重复添加“用户说：”会增加噪声和少量 Token
- 某些模型可能把“用户说：”理解为转述内容，而不是用户直接提出的请求

因此，单 AI 场景应尽量接近普通一对一聊天的输入形式。

## 目标行为

### 单 AI 角色聊天室

当前活动角色集合中只有一个 `IsHuman == false` 的角色时：

| 公开消息类型 | 发送给模型的文本 |
|---|---|
| 人类消息 | 原始 `message.Content` |
| 当前角色自己的历史消息 | 跳过 |
| 系统消息 | 跳过 |
| 其他 AI 的历史消息 | 仍使用 `{角色名}说：{内容}` |
| `additionalUserMessages` | 保持原样 |

“其他 AI 的历史消息”可能来自已被移除的旧角色。即使当前房间只剩一个 AI，也不能把这种历史消息伪装成用户原话，因此必须保留发送者标签。

### 多 AI 角色聊天室

当前活动角色集合中有两个或更多非人类角色时，行为完全保持现状：

| 公开消息类型 | 发送给模型的文本 |
|---|---|
| 人类消息 | `用户说：{内容}` |
| 其他 AI 消息 | `{角色名}说：{内容}` |
| 当前角色自己的消息 | 跳过 |
| 系统消息 | 跳过 |
| `additionalUserMessages` | 保持原样 |

## “只有一个角色”的判定

统一定义为：

```text
当前聊天室角色集合中 IsHuman == false 的角色数量等于 1
```

判定不考虑：

- `ParticipationMode`
- `IsManagerRole`
- `ExecutionKind`
- 当前角色是否已发言
- 当前是否通过 @ 触发
- 人类角色数量

因此，以下都属于单 AI 场景：

- 一个 Standard 助手 + 一个人类
- 一个 Coding 助手 + 一个人类
- 一个 MentionOnly 助手 + 一个人类
- 一个管理者 AI + 一个人类
- 一个 AI + 多个人类身份

动态添加第二个 AI 后，后续发言立即恢复带前缀格式；移除到只剩一个 AI 后，后续人类消息立即改为原文。该行为不需要持久化额外状态。

## 核心设计

### 设计一：只省略人类消息前缀

不要在单 AI 分支中简单执行：

```text
所有增量消息都直接使用 message.Content
```

这样会把已移除角色留下的 AI 历史消息、或其他异常来源的非人类消息错误地解释为当前用户输入。

正确规则是：

```text
singleAiParticipant && message.IsHumanMessage
  → message.Content
否则
  → 现有发送者前缀格式
```

Domain 消息使用 `message.Kind == ChatRoomMessageKind.Human` 做等价判断。

### 设计二：提取最小的共享文本格式化函数

Legacy 和新 Runtime 当前各自实现了一份相同的文本拼装逻辑。建议增加一个程序集内部的纯格式化辅助方法，只接收必要事实：

```text
FormatIncrementalMessage(
  string content,
  bool isHuman,
  string? senderRoleName,
  bool omitHumanPrefix)
```

返回规则：

1. `isHuman && omitHumanPrefix`：返回原始内容
2. `isHuman`：返回 `用户说：{内容}`
3. 非人类且角色名为空：返回 `另一位参与者说：{内容}`
4. 非人类且有角色名：返回 `{角色名}说：{内容}`

辅助方法不负责：

- 查询增量范围
- 跳过系统消息
- 跳过自身消息
- 判断房间角色数量
- 处理 additional messages

这样既消除文本规则重复，又不把 legacy 与 Domain 消息类型强行耦合。

### 设计三：Legacy 路径在构建消息前计算一次

`BuildIncrementalUserMessages` 在进入消息循环前计算：

```text
bool omitHumanPrefix =
  _manager.Roles.Count(role => !role.Definition.IsHuman) == 1;
```

随后仅在格式化每条人类消息时使用该值。

不要在循环中反复统计角色，也不要根据 `incrementalMessages` 中出现了多少发送者推断房间模式。历史消息集合并不等同于当前参与者集合。

### 设计四：新 Coordinator 显式传递房间视角

`IsolatedChatRoomRoleRuntime` 当前只收到目标角色定义和增量消息，无法从请求中知道房间有几个 AI 角色。

建议在 `ChatRoomRoleExecutionRequest` 中增加一个不可持久化的执行事实，例如：

```text
bool OmitHumanSenderPrefix
```

`ChatRoomCoordinator.StartExecutionCore` 从当前不可变状态计算该值：

```text
_state.Roles.Count(role => !role.IsHuman) == 1
```

`IsolatedChatRoomRoleRuntime.BuildIncrementalMessages` 使用该值调用共享格式化函数。

不建议把完整角色列表传给 Runtime，只为格式化消息引入额外数据面；布尔执行事实足以表达本需求，也不会混入 checkpoint 或持久化状态。

### 设计五：同步调整聊天室协作提示词

`BuildChatRoomContext` 根据当前非人类角色数量生成不同说明。

单 AI 角色时建议说明：

```text
- 人类消息会直接作为 User 消息输入，不额外添加“用户说：”前缀
- 如果历史中出现其他角色消息，仍会使用“角色名说：...”标明来源
```

多 AI 角色时保留现有说明：

```text
- 人类和其他角色消息会通过“用户说：...”或“角色名说：...”标明来源
```

其他 @ 机制和协作说明不变。

Coding 执行器当前不使用 `ChatRoomRole.BuildSystemPrompt`，但其输入文本同样经过 `BuildIncrementalUserMessages`，因此会自然获得单 AI 原文格式，无需在 `CodingAgent` 中新增分支。

## 与第三项 Mention 上下文隔离机制的组合顺序

消息处理顺序应固定为：

1. 选择本轮允许角色看到的公开消息集合
2. 跳过系统消息和自身消息
3. 根据单 AI / 多 AI 规则格式化每条消息
4. 追加内部协调消息
5. 转换为 `TextContent`

因此：

- 第二项负责“选中消息如何表达”
- 第三项负责“哪些消息被选中”

如果第三项只选出最近一条 @ 消息，第二项仍对该消息应用相同格式规则：

- 单 AI + 人类 @：直接发送原文
- 多 AI + 人类 @：发送 `用户说：...`
- 任意角色数量 + AI @：发送 `{角色名}说：...`

两项不得各自复制一份完整消息构建逻辑。

## 不需要修改的内容

本需求不需要：

- 新增角色配置字段
- 修改参与模式
- 修改会话 JSON 架构
- 修改角色模板
- 修改消息游标或消费水位
- 修改 Mention 解析规则
- 修改 AgentSession 持久化
- 修改工作区工具
- 修改 Avalonia 设置界面

## 需要修改的核心文件

### Legacy 运行路径

- `SemanticKernelSamples/AgentLib/AgentLib.ChatRoom/ChatRoomManager.ChatRoomAutoLoopRunner.cs`
  - 统计当前非人类角色数量
  - 对单 AI 场景的人类消息直接使用原文
  - 条件化更新 `BuildChatRoomContext` 的格式说明

### 共享格式化逻辑

- 建议新增 `SemanticKernelSamples/AgentLib/AgentLib.ChatRoom/ChatRoomIncrementalMessageFormatter.cs`
  - 保存不依赖具体消息模型的纯文本格式化规则

如果实施时认为单个四分支方法不足以支撑独立文件，也可以在现有程序集内部类中放置，但 legacy 与新 Runtime 不应继续各自复制字符串规则。

### 新 Domain/Coordinator 路径

- `SemanticKernelSamples/AgentLib/AgentLib.ChatRoom/Runtime/IChatRoomRoleRuntime.cs`
  - 在 `ChatRoomRoleExecutionRequest` 中增加本轮是否省略人类前缀的事实
- `SemanticKernelSamples/AgentLib/AgentLib.ChatRoom/Coordination/ChatRoomCoordinator.cs`
  - 从当前角色快照计算该事实
- `SemanticKernelSamples/AgentLib/AgentLib.ChatRoom/Runtime/IsolatedChatRoomRoleRuntime.cs`
  - 使用共享格式化函数并删除重复字符串拼装

## 测试计划

### Legacy ChatRoomManager 测试

在 `ChatRoomManagerTests` 或 `ChatRoomManagerIntegrationTests` 中使用可记录模型输入的 Fake Client，精确验证：

1. 单 AI + 一条人类消息：模型收到原文，不包含 `用户说：`
2. 单 AI + 多条人类消息：顺序不变，每条均为原文
3. 单 Coding AI：同样不添加 `用户说：`
4. 单 MentionOnly AI 被 @：同样不添加 `用户说：`
5. 两个 AI：人类消息仍为 `用户说：...`
6. 多 AI 场景下其他角色消息仍为 `{角色名}说：...`
7. 单 AI 场景存在已移除角色的历史消息：该历史 AI 消息仍保留角色名前缀
8. 系统消息仍被跳过
9. 自身消息仍被跳过
10. `additionalUserMessages` 仍原样追加
11. 动态添加第二个 AI 后，后续消息恢复前缀
12. 移除第二个 AI 后，后续人类消息改为原文

### 系统提示词测试

验证首次发言时：

1. 单 AI 场景的系统提示词不再声称一定会看到“用户说：...”
2. 多 AI 场景继续包含现有发送者说明
3. 角色身份、@ 机制和协作规则没有丢失

### 新架构测试

在 `Architecture/ChatRoomCoordinatorTests.cs` 和运行时测试中验证：

1. 一个非人类角色时，执行请求中的省略前缀事实为 true
2. 多个非人类角色时为 false
3. 人类角色数量不影响判定
4. `IsolatedChatRoomRoleRuntime` 在 true 时对人类消息输出原文
5. 非人类消息无论 true/false 都保留发送者标签
6. 执行请求深拷贝、失败重试和 checkpoint 不持久化该瞬态事实

### 回归测试

继续运行：

- `AgentLib.ChatRoom.Tests`
- `ChatRoom.Shell.Tests`
- 完整解决方案构建

重点确认自动循环顺序、Mention 调度、历史恢复和 Coding AgentSession 测试不受影响。

## 分步实施计划

1. 定义单 AI 场景的人类消息格式规则和行为矩阵
2. 增加内部共享的增量消息文本格式化函数
3. 修改 legacy `BuildIncrementalUserMessages`，在单 AI 场景只省略人类消息前缀
4. 修改 legacy `BuildChatRoomContext`，使格式说明与实际输入一致
5. 扩展新架构执行请求，携带是否省略人类前缀的瞬态事实
6. 修改 Coordinator，从当前角色快照计算该事实
7. 修改 Isolated Runtime，复用共享格式化函数
8. 添加单 AI、多 AI、动态增删角色和历史 AI 消息测试
9. 添加系统提示词和新架构执行请求测试
10. 运行相关测试、完整构建并审查变更

## 验收标准

1. 当前聊天室只有一个非人类角色时，该角色收到的人类消息与用户输入文本完全一致
2. 单 AI 场景不再出现无意义的 `用户说：` 包装
3. 多 AI 场景的发送者标记保持现有行为
4. 非人类历史消息不会因当前只剩一个 AI 而失去来源信息
5. 系统消息、自身消息和附加协调消息的行为不变
6. Standard、Coding、AlwaysParticipate、MentionOnly 和管理者角色使用同一判断规则
7. legacy 和新 Domain/Coordinator 路径行为一致
8. 无新增持久化字段或 UI 配置
9. 现有自动循环、Mention、会话恢复和 AgentSession 测试全部通过
