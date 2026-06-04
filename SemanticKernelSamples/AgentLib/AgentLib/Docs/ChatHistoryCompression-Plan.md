# CopilotChatManager 对话上下文压缩机制设计计划（修订版）

## 背景

当前 `CopilotChatManager` 在 `CreateChatClientAgentAsync` 方法中创建 `ChatClientAgentOptions` 时，未设置 `ChatHistoryProvider` 属性，导致 `ChatClientAgent` 框架的对话历史压缩机制未启用。随着对话轮次增加，上下文不断膨胀，最终可能超出模型 token 限制。

参考 `FallnayyewelCeehowawcerjur/Program.cs` 的演示代码，`Microsoft.Agents.AI` 框架通过 `ChatClientAgentOptions.ChatHistoryProvider` + `IChatReducer` 模式提供了对话历史压缩能力。

## 框架能力回顾

`ChatClientAgentOptions` 中与压缩相关的属性：

| 属性 | 类型 | 说明 |
|------|------|------|
| `ChatHistoryProvider` | `IChatHistoryProvider?` | 对话历史提供者，内部持有 `IChatReducer`，负责在对话过程中裁剪/压缩消息 |
| `RequirePerServiceCallChatHistoryPersistence` | `bool` | 默认为 `false`；设为 `true` 时，每次工具调用完成后都会触发 `ReduceAsync` |

`InMemoryChatHistoryProvider` 是框架提供的内置实现，通过 `InMemoryChatHistoryProviderOptions.ChatReducer` 注入 `IChatReducer`。

`IChatReducer.ReduceAsync(IEnumerable<ChatMessage>, CancellationToken)` 接收当前全部消息，返回裁剪后的消息集合。

## 设计目标

1. 为 `CopilotChatManager` 增加可选的对话上下文压缩能力
2. 保持向后兼容：默认不启用压缩，行为与现状一致
3. 允许调用方自定义压缩策略（`IChatReducer` 实现）
4. 压缩机制与现有的 `AgentSession`（withHistory）模式协同工作
5. 每个 `SendMessageRequest` 可以独立决定是否启用压缩

## 核心设计决策

### 决策 1：入口层级 —— 单一入口

仅在 `SendMessageRequest` 层面暴露参数，不在 `CopilotChatManager` 上提供全局属性。

理由：
- 每个请求的压缩策略可能不同（不同对话场景需要不同的裁剪阈值）
- 避免 Manager 上的有状态 reducer 在多会话间共享引起的问题
- 保持 API 简洁，调用方在每次 `SendMessage` 时显式传入

### 决策 2：RequirePerServiceCallChatHistoryPersistence 开关

同样仅在 `SendMessageRequest` 层面提供，默认 `false`。

> 此开关仅在 `ChatReducer != null`（即启用了压缩）时才有意义。

### 决策 3：内置默认实现

已存在内置 `IChatReducer` 实现 `CopilotChatManagerChatReducer`（`AgentLib\CopilotChatManagerChatReducer.cs`）：

- 基于 LLM 摘要的压缩策略：提取 System Prompt，对剩余消息调用 `IChatClient.GetResponseAsync` 生成摘要
- 使用 `DefaultSummarizationPrompt`（要求生成不超过五句话的对话总结）
- 当前为 `internal class`，需要在集成时决定是否改为 `public`

> 注：当前 `CopilotChatManagerChatReducer` 尚未与 `CopilotChatManager` 集成，这正是本计划要解决的问题。`ReduceSessionAsync` 方法在当前代码库中尚不存在。

### 3. 可能新增 / 修改的文件

- `AgentLib\CopilotChatManagerChatReducer.cs` — 可能需要将 `CopilotChatManagerChatReducer` 改为 `public`，让调用方可直接实例化

### 决策 4：与 withHistory/AgentSession 的关系

- **UI 显示完整历史**：`CopilotChatSession.ChatMessages` 不变，始终包含所有消息
- **模型看到压缩上下文**：`ChatHistoryProvider` 只裁剪传递给模型的消息列表
- **`WithHistory = false` 时**：`AgentSession` 为 null，框架会跳过压缩逻辑（没有历史可供压缩），行为合理
- **每次新的 `InMemoryChatHistoryProvider`**：框架内部自行管理状态，不会丢失之前的压缩状态

### 决策 5：日志记录

不在 `CopilotChatManager` 中强制集成压缩日志。调用方可使用装饰器模式包装自己的 `IChatReducer` 实现来记录压缩事件。

## 需要变更的文件

### 1. `AgentLib\Model\SendMessages_\SendMessageRequest.cs`

新增两个可选参数：

```csharp
public readonly record struct SendMessageRequest(
    IReadOnlyList<AIContent> Contents,
    bool WithHistory = true,
    bool CreateNewSession = false,
    IEnumerable<AITool>? Tools = null,
    ChatToolMode? ToolMode = null,
    string? SystemPrompt = null,
    CancellationToken CancellationToken = default,
    // 新增
    IChatReducer? ChatReducer = null,
    bool RequirePerServiceCallChatHistoryPersistence = false
)
```

`FromText` 工厂方法同步新增参数。

### 2. `AgentLib\CopilotChatManager.cs`

**不新增任何属性**。仅在内部方法 `CreateChatClientAgentAsync` 中：

- 从 `request.ChatReducer` 读取压缩器
- 若 `ChatReducer != null`，创建 `InMemoryChatHistoryProvider` 并赋值给 `ChatClientAgentOptions.ChatHistoryProvider`
- 同时将 `ChatClientAgentOptions.RequirePerServiceCallChatHistoryPersistence` 设置为 `request` 中对应的值

### 3. 不变更的文件

- `CopilotChatSession.cs` — 无需修改，UI 消息列表保持完整
- `SendMessageResult.cs` — 无需修改，压缩对调用方透明
- `SendMessageAsync` 重载方法 — 保持现有签名不变（默认不压缩）

## 已解决的开放问题

1. **`IChatReducer` 类型来源**：✅ 已确认。`Microsoft.Agents.AI.OpenAI` 依赖链已间接引用 `Microsoft.Extensions.AI`，其中定义了 `IChatReducer`。

2. **压缩后是否回写 UI 消息列表**：✅ 不回写。UI 显示完整历史，模型看到压缩上下文。这是期望行为。

3. **多会话共享 reducer 实例**：✅ 不适用。去掉 Manager 级别属性后，每个请求独立传入 reducer，不存在跨会话共享问题。

4. **`InMemoryChatHistoryProvider` 生命周期**：✅ 每次新建没有问题。框架内部自行管理状态。

5. **`WithHistory = false` 组合**：✅ 框架自动跳过压缩，行为合理。

## 实施步骤

1. 修改 `SendMessageRequest`，新增 `ChatReducer` 和 `RequirePerServiceCallChatHistoryPersistence` 参数
2. 修改 `SendMessageRequest.FromText` 工厂方法支持新参数
3. 修改 `CopilotChatManager` 的 `CreateChatClientAgentAsync` 内部方法，根据 request 参数注入 `InMemoryChatHistoryProvider`
4. 编写单元测试验证：ChatReducer 为 null 时行为不变、ChatReducer 不为 null 时正确注入
5. 编译验证通过
