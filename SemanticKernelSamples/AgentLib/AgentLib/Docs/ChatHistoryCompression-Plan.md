# CopilotChatManager 对话上下文压缩机制设计计划

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

### 决策 1：入口层级

**采用双重入口**：
- **Manager 级别**（全局默认）：`CopilotChatManager` 新增 `ChatReducer` 属性，作为所有会话的默认压缩器。类似 `WorkspacePath` 模式。
- **Request 级别**（单次覆盖）：`SendMessageRequest` 新增可选的 `ChatReducer` 参数，允许单次调用使用不同的压缩策略（或显式传 `null` 禁用压缩）。

当 `SendMessageRequest.ChatReducer` 不为 `null` 时，使用该值；否则回退到 `CopilotChatManager.ChatReducer`；两者都为 `null` 时不启用压缩。

### 决策 2：RequirePerServiceCallChatHistoryPersistence 开关

同样采用双重入口：
- **Manager 级别**：`CopilotChatManager.RequirePerServiceCallChatHistoryPersistence` 属性（默认 `false`）
- **Request 级别**：`SendMessageRequest` 新增可选参数

> 此开关仅在 `ChatReducer != null`（即启用了压缩）时才有意义。

### 决策 3：内置默认实现

提供可选的内置 `IChatReducer` 实现，但**不作为默认值**。默认情况 `ChatReducer` 为 `null`，不启用压缩。

可考虑提供的内置实现：
- `TokenBasedChatReducer`：基于 token 数量阈值裁剪（需依赖 tokenizer）
- `CountBasedChatReducer`：基于消息数量阈值裁剪（如 `FallnayyewelCeehowawcerjur` 中的 `LoggingChatReducer`）

> 第一期可以只提供接口，内置实现后续迭代。调用方自行实现 `IChatReducer`。

### 决策 4：与 withHistory/AgentSession 的关系

当 `ChatReducer != null` 时，`ChatHistoryProvider` 应由 `ChatClientAgentOptions.ChatHistoryProvider` 接收。`InMemoryChatHistoryProvider` 内部管理其自己的消息历史，与 `CopilotChatSession.ChatMessages` 独立。

但是，`AgentSession` 通过 `GetOrCreateAgentSessionAsync` 创建并持久化到 `CopilotChatSession`。这意味着：
- `withHistory = true`：复用已有的 `AgentSession`，对话状态由 `AgentSession` 内部维护
- 启用压缩后，`ChatHistoryProvider` 会对**传递给模型的**消息列表进行裁剪，但不影响 `CopilotChatSession.ChatMessages`（UI 显示的消息列表）

**这是期望行为**：UI 显示完整消息历史，但模型只看到压缩后的上下文。

### 决策 5：日志记录

在 ChatReducer 执行压缩时，可记录：
- 压缩前后的消息数量
- 被裁剪的消息范围

但这不是 `CopilotChatManager` 的直接职责。可以通过以下方式支持：
1. 提供 `LoggingChatReducer` 装饰器（类似 FallnayyewelCeehowawcerjur 中的模式），由调用方自行决定是否包装
2. 或者，暂时不做日志集成，由 `IChatReducer` 的实现者自行处理

> 第一期建议不强制集成日志，保持简单。

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

### 2. `AgentLib\CopilotChatManager.cs`

新增属性和修改内部逻辑：

- **新增属性**：
  ```csharp
  public IChatReducer? ChatReducer { get; set; }
  public bool RequirePerServiceCallChatHistoryPersistence { get; set; }
  ```

- **修改 `CreateChatClientAgentAsync` 内部方法**：
  根据 `ChatReducer` 决定是否创建 `InMemoryChatHistoryProvider` 并赋值给 `ChatClientAgentOptions.ChatHistoryProvider`。
  同时设置 `RequirePerServiceCallChatHistoryPersistence`。

### 3. 可能新增文件

- `AgentLib\ChatReducers\` 目录（可选，用于内置 `IChatReducer` 实现）
- 如果提供内置实现，可能新增 `CountBasedChatReducer.cs` 等

## 待讨论的开放问题

1. **`IChatReducer` 类型来源**：`IChatReducer` 在 `Microsoft.Extensions.AI` 包中定义。AgentLib 是否已间接引用？需验证 `Microsoft.Agents.AI.OpenAI 1.6.2` 的依赖链。

2. **压缩后是否需回写 `CopilotChatSession.ChatMessages`**：当前设计是 UI 保留完整历史，模型看到压缩后的上下文。这是否符合预期？还是需要同步裁剪 UI 消息列表？

3. **多会话场景**：如果全局设置 `CopilotChatManager.ChatReducer`，所有会话共享同一个 reducer 实例。这对有状态的 reducer（如累计 token 计数）可能有问题。是否需要每个会话创建独立的 reducer 实例？

4. **`InMemoryChatHistoryProvider` 的生命周期**：当前每次 `SendMessage` 都会 `new ChatClientAgentOptions()`。如果启用压缩，每次新建 `InMemoryChatHistoryProvider` 是否会丢失之前的压缩状态？需要验证是否需要同一个 provider 实例跨多次调用共享。

5. **与 `SendMessageRequest.WithHistory = false` 的组合**：当不带历史时，`AgentSession` 为 null，此时 `ChatHistoryProvider` 的行为是什么？框架是跳过压缩还是有默认行为？

## 实施步骤（草案）

1. 验证 `IChatReducer` 在 AgentLib 依赖链中的可用性
2. 修改 `SendMessageRequest`，新增 `ChatReducer` 和 `RequirePerServiceCallChatHistoryPersistence` 参数
3. 在 `CopilotChatManager` 新增 `ChatReducer` 和 `RequirePerServiceCallChatHistoryPersistence` 属性
4. 修改 `CreateChatClientAgentAsync` 内部逻辑，根据 ChatReducer 决定是否注入 `InMemoryChatHistoryProvider`
5. 编写单元测试验证压缩机制的行为
6. 更新 `SendMessageRequest.FromText` 工厂方法支持新参数
