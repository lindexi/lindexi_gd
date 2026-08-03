# ChatRoom 单 AI 角色增量消息格式优化计划

## 文档定位

本文恢复“单 AI 角色增量消息格式优化”计划，并按当前代码状态重新收敛范围。

目标行为是：

> 当前聊天室只有一个非人类角色时，发送给该角色的人类公开消息直接使用原文；存在多个非人类角色时，继续使用“用户说：”说明来源。其他 AI 的历史消息始终保留发送者标签。

该行为的核心实现已经存在。本文保留行为约束、当前实现说明和剩余待办，避免重新引入清理时已经删除的 `Domain`、`Coordination`、`Runtime` 双架构。

本文只给出文档计划，不修改代码。

## 当前实现状态

### 已实现：统一增量消息格式化

`AgentLib.ChatRoom/ChatRoomIncrementalMessageFormatter.cs` 已提供程序集内部的纯格式化函数 `ChatRoomIncrementalMessageFormatter.Format`，规则为：

| 消息类型 | `omitHumanPrefix` | 输出 |
|---|---:|---|
| 人类消息 | `true` | 原始 `content` |
| 人类消息 | `false` | `用户说：{content}` |
| 非人类消息且有发送者名称 | 任意 | `{senderRoleName}说：{content}` |
| 非人类消息且发送者名称为空 | 任意 | `另一位参与者说：{content}` |

格式化器只负责表达消息，不负责选择增量范围、过滤消息或判断房间角色数量。

### 已实现：按当前非人类角色数量动态判断

`ChatRoomManager.ChatRoomAutoLoopRunner.BuildIncrementalUserMessages` 在每次角色发言前计算：

```text
当前 Roles 中 IsHuman == false 的角色数量是否等于 1
```

因此该行为会随当前角色集合即时变化：

- 添加第二个 AI 后，后续人类消息恢复为 `用户说：...`
- 移除到只剩一个 AI 后，后续人类消息恢复为原文
- 不需要保存额外模式或迁移持久化数据

### 已实现：只省略人类消息前缀

当前构建流程仍然：

1. 通过 `ChatRoomSession.GetMessagesSinceLastSpeak(roleId)` 获取增量公开消息
2. 跳过系统消息和预设信息
3. 跳过当前角色自己的公开消息
4. 使用 `ChatRoomIncrementalMessageFormatter.Format` 格式化其余消息
5. 原样追加自动循环生成的 `additionalUserMessages`

单 AI 分支不会把所有消息都改为原文。即使当前只剩一个 AI，已移除角色留下的历史消息仍会保持 `{角色名}说：{内容}`，避免被误解为人类直接输入。

### 已实现：Standard 与 Coding 共用消息构建

`ChatRoomRole.SpeakAsync` 接收已经构建好的 `incrementalUserTexts`，再将每项转换为独立的 `TextContent`。

`StandardChatRoomRoleExecutor` 与 `CodingChatRoomRoleExecutor` 的执行方式不同，但两者都使用同一批 `TextContent`。因此以下角色采用同一判定规则：

- Standard 角色
- Coding 角色
- `AlwaysParticipate` 角色
- `MentionOnly` 角色
- 管理者角色

无需在 `CodingAgent` 或具体执行器中重复增加单 AI 分支。

### 已实现：主要回归测试

`AgentLib.ChatRoom.Tests/ChatRoomManagerTests.cs` 已覆盖：

- 单 AI 多条人类消息按原文顺序输入
- Coding 执行种类的测试执行器省略人类前缀
- 单 MentionOnly AI 省略人类前缀
- 单管理者 AI 省略人类前缀
- 多 AI 保留人类与其他 AI 的发送者前缀
- 单 AI 保留已移除 AI 的历史发送者标签
- 系统消息、预设信息和自身消息继续被过滤
- 动态增加、移除第二个 AI 后立即切换格式
- 格式化器的人类、非人类和匿名发送者分支

这里验证的是进入执行器之前的统一消息构建结果；实际 `CodingChatRoomRoleExecutor` 不需要再单独实现同一格式分支。

## 仍需处理的问题

### `BuildChatRoomContext` 的说明与实际输入不完全一致

`ChatRoomManager.ChatRoomAutoLoopRunner.BuildChatRoomContext` 当前无条件告诉角色：

```text
当你看到“用户说：...”或“角色名说：...”时，应理解为对应的人类或非人类角色发表了该内容
```

但单 AI 场景中的人类消息实际不会带 `用户说：` 前缀。

现有系统提示词测试只验证协作说明仍存在，没有验证单 AI 提示词是否准确说明“人类消息可能直接使用原文”。因此当前剩余待办是修正提示词与测试，而不是重新实现消息格式化主体。

### 不应简单按当前角色数量生成互斥提示词

`ChatRoomContext` 会在每次发言前赋值，但 `ChatRoomRole.BuildSystemPrompt` 只在该角色首次发言时注入系统提示词。角色已经发言后，动态增删第二个 AI 会立即改变增量消息格式，却不会自动重建其既有 AgentSession 中的系统提示词。

因此不建议写成只对首次角色数量成立的互斥说明：

```text
单 AI：永远没有“用户说：”
多 AI：永远存在“用户说：”
```

更稳妥的说明应覆盖两种运行时形态，并在角色数量动态变化后仍然正确。

## 目标行为

### 单 AI 角色聊天室

当前角色集合中只有一个 `IsHuman == false` 的角色时：

| 公开消息类型 | 发送给模型的文本 |
|---|---|
| 人类消息 | 原始 `message.Content` |
| 当前角色自己的历史消息 | 跳过 |
| 系统消息 | 跳过 |
| 预设信息 | 跳过 |
| 其他 AI 的历史消息 | `{角色名}说：{内容}` |
| 匿名非人类历史消息 | `另一位参与者说：{内容}` |
| `additionalUserMessages` | 保持原样 |

### 多 AI 角色聊天室

当前角色集合中有两个或更多非人类角色时：

| 公开消息类型 | 发送给模型的文本 |
|---|---|
| 人类消息 | `用户说：{内容}` |
| 其他 AI 消息 | `{角色名}说：{内容}` |
| 当前角色自己的消息 | 跳过 |
| 系统消息 | 跳过 |
| 预设信息 | 跳过 |
| `additionalUserMessages` | 保持原样 |

## “只有一个 AI”的统一判定

统一定义为：

```text
当前 ChatRoomManager.Roles 中 IsHuman == false 的角色数量等于 1
```

判定不考虑：

- `ParticipationMode`
- `IsManagerRole`
- `ExecutionKind`
- 当前角色是否已发言
- 是否由 @mention 触发
- 人类角色数量

因此，一个 Coding 助手、一个 MentionOnly 专家或一个管理者 AI 都属于单 AI 场景。

## 剩余最小改动设计

### 1. 保持现有消息构建实现

以下实现已经符合目标，不应重写：

- `ChatRoomIncrementalMessageFormatter.Format`
- `BuildIncrementalUserMessages` 中的单 AI 计数
- 系统、预设、自身消息过滤
- 其他 AI 消息的发送者标签
- Standard 与 Coding 共用 `TextContent` 输入

### 2. 将提示词改为稳定的双形态说明

建议把现有无条件说明调整为同时覆盖单 AI 和多 AI：

```text
- 人类消息始终作为 User 角色输入；当前只有一个非人类角色时通常直接使用原文，存在多个非人类角色时会使用“用户说：...”标明来源
- 其他非人类角色的公开消息同样作为 User 角色输入，并使用“角色名说：...”标明来源
```

该表述具有以下性质：

- 与当前实际格式一致
- 动态增加或移除角色后仍然成立
- 不要求重建角色 AgentSession
- 不改变 @ 机制和协作原则
- 不要求 Coding 执行器消费该提示词才能获得正确输入

### 3. 补强提示词测试

在现有 `StepAsyncShouldDescribeCurrentSenderFormatInSystemPrompt` 附近补充精确断言：

1. 单 AI 首次发言的提示词明确允许人类消息直接使用原文
2. 提示词不再声称所有人类消息一定具有 `用户说：` 前缀
3. 多 AI 首次发言的提示词仍说明 `用户说：...` 的来源标记
4. 两种场景都保留以下内容：
   - 相对角色视角
   - 其他角色作为 User 输入
   - `@机制`
   - `协作原则`

## 与 MentionOnly 上下文隔离的组合顺序

单 AI 格式优化负责“选中的消息如何表达”，MentionOnly 隔离负责“本轮选择哪些公开消息”。处理顺序应固定为：

1. 选择本轮允许角色看到的公开消息
2. 跳过系统、预设和自身消息
3. 根据单 AI或多 AI 规则格式化消息
4. 追加宿主控制消息
5. 转换为 `TextContent`

如果 MentionOnly 隔离只选中一条触发消息：

- 单 AI + 人类 @：直接发送人类原文
- 多 AI + 人类 @：发送 `用户说：...`
- 任意角色数量 + AI @：发送 `{角色名}说：...`

隔离分支不应复制另一套字符串格式化逻辑。

## 不需要修改的内容

本待办不需要：

- 新增角色配置字段
- 修改 `ParticipationMode`
- 修改 Mention 解析或调度顺序
- 修改会话和模板 JSON
- 修改 AgentSession 持久化
- 修改 Standard 或 Coding 执行器
- 修改 Avalonia 页面
- 恢复 `Domain`、`Coordination`、`Runtime`、Snapshot 或消费水位架构

## 影响文件

### 预计修改

- `SemanticKernelSamples/AgentLib/AgentLib.ChatRoom/ChatRoomManager.ChatRoomAutoLoopRunner.cs`
  - 调整 `BuildChatRoomContext` 中的消息格式说明

- `SemanticKernelSamples/AgentLib/AgentLib.ChatRoom.Tests/ChatRoomManagerTests.cs`
  - 补强单 AI、多 AI 系统提示词断言

### 明确不修改

- `ChatRoomIncrementalMessageFormatter.cs`
- `ChatRoomRole.cs`
- `StandardChatRoomRoleExecutor.cs`
- `CodingChatRoomRoleExecutor.cs`
- `ChatRoomSession.cs`
- 持久化、模板和 UI 文件

除非实施时发现当前已实现行为回归，否则不应扩大修改范围。

## 实施顺序

1. 固化单 AI、多 AI 和历史 AI 消息的行为矩阵
2. 调整 `BuildChatRoomContext` 为动态角色变化后仍成立的双形态说明
3. 补强系统提示词测试
4. 运行 `AgentLib.ChatRoom.Tests`
5. 运行 `ChatRoom.Shell.Tests`
6. 构建完整解决方案并检查无额外行为变化

## 验收标准

1. 单 AI 场景的人类消息继续与用户输入文本完全一致
2. 多 AI 场景继续使用 `用户说：` 标明人类来源
3. 其他 AI 历史消息始终保留发送者标签
4. 系统消息、预设信息、自身消息和宿主附加消息行为不变
5. Standard、Coding、MentionOnly 和管理者角色使用同一格式规则
6. 系统提示词准确描述人类原文与带前缀两种形态
7. 动态增删第二个 AI 后，不会因首次系统提示词过时而与实际格式冲突
8. 不新增持久化字段、UI 配置或替代架构
9. 现有自动循环、Mention、会话恢复和 Coding AgentSession 测试继续通过

## 与旧稿的差异

旧稿中的以下任务已经完成，不再列为待实现：

- 提取共享格式化器
- legacy 路径统计非人类角色数量
- Standard 与 Coding 共用格式化后的输入
- 单 AI、多 AI、动态增删和历史 AI 消息测试

旧稿中的以下架构已经从当前项目清理，不应恢复：

- `ChatRoomRoleExecutionRequest.OmitHumanSenderPrefix`
- `ChatRoomCoordinator`
- `IsolatedChatRoomRoleRuntime`
- `Domain/Coordination/Runtime` 双路径一致性测试

当前待办仅保留系统提示词一致性收尾。

实施时应继续以当前扁平架构为准。
