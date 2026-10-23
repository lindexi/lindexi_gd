# AgentSessionStreamingHelper 设计文档

## 背景

在业务端（如 `StreamingSlideGenerator`）使用 `IManualSendMessageContext` 手动控制 AgentFramework 流式调用时，存在以下痛点：

1. **取消补全逻辑重复且易错**：业务端需要手动处理 `shouldAppendInputMessages`、`shouldAppendAssistantMessages`、`FunctionCallContent` 去重、`FunctionResultContent` 补全等细节。
2. **历史修复与业务消费耦合**：业务端在 `await foreach` 循环中既要消费 `AgentResponseUpdate`，又要收集 update 用于退出时补全，且补全时机和条件判断容易写错。
3. **下一轮继续调用风险高**：取消后如果历史未正确补全，下一轮 Agent 调用会收到残缺的上下文，可能导致模型行为异常。
4. **自定义 ChatClient 场景不友好**：`IManualSendMessageContext` 通过 `ChatClient` 属性暴露底层客户端，但历史补全逻辑仍然需要业务端自己实现。

## 核心思路

抽象出一个**与 `IManualSendMessageContext` 无关**的辅助 API：

- 接收 **`ChatClientAgent` + `AgentSession` + 输入消息**，不关心它们是怎么创建的。
- 返回 `IAsyncEnumerable<AgentResponseUpdate>`，调用方用 `await foreach` 自然消费。
- **迭代器退出时自动补全 `AgentSession` 历史**，无论退出方式是：正常完成、`CancellationToken` 取消、还是调用方 `break`。

## 设计

### 命名空间

```
namespace AgentLib;
```

### 类型与 API

仅增加一个静态类和一个扩展方法：

```csharp
namespace AgentLib;

/// <summary>
/// 运行流式 Agent 调用的辅助方法。
/// 在迭代器退出时（正常完成 / 取消 / break），
/// 自动补全 <see cref="AgentSession"/> 中的输入消息和助手响应历史。
/// </summary>
public static class AgentSessionStreamingHelper
{
    /// <summary>
    /// 运行流式 Agent 调用。
    /// 每个 <see cref="AgentResponseUpdate"/> 逐一 yield 给调用方。
    /// 调用方可在 <c>await foreach</c> 循环中自由 <c>break</c> 或触发取消；
    /// 无论以何种方式退出循环，<paramref name="session"/> 的历史都会在迭代器内部自动补全。
    /// </summary>
    /// <param name="agent">已配置好的 <see cref="ChatClientAgent"/>。</param>
    /// <param name="inputMessages">本轮要发送的输入消息。</param>
    /// <param name="session">代理会话，其历史将在退出时自动补全。</param>
    /// <param name="cancellationToken">取消令牌。取消时，已收集的助手更新会被补全进会话历史。</param>
    /// <returns>流式响应更新序列。</returns>
    public static IAsyncEnumerable<AgentResponseUpdate> RunWithHistoryCompletionAsync(
        ChatClientAgent agent,
        IReadOnlyList<ChatMessage> inputMessages,
        AgentSession session,
        CancellationToken cancellationToken = default);
}
```

### 内部实现要点

迭代器内部伪代码：

```
收集已 yield 的 update 列表
try
    foreach (var update in agent.RunStreamingAsync(inputMessages, session, cancellationToken))
        yield return update
        收集 update
finally
    CompleteCancelledRunHistory(session, inputMessages, collectedUpdates)
```

#### `CompleteCancelledRunHistory` 补全规则

1. 调用 `session.TryGetInMemoryChatHistory` 获取当前历史。
2. **输入消息补全**：
   - 如果历史末尾（倒数 `inputMessages.Count` 条）与 `inputMessages` 不完全一致，则将 `inputMessages` 追加到末尾。
   - 此处的比较是**内容对比**（role + text），而不是引用对比，避免 AgentFramework 已自动持久化后的重复追加。
3. **助手消息补全**：
   - 从 `collectedUpdates` 中收集所有 `Contents`。
   - 过滤掉 `CallId` 已在历史中出现的 `FunctionCallContent`（避免重复追加 AgentFramework 已持久化的工具调用请求）。
   - 如果过滤后的 `Contents` 为空，则跳过助手消息追加。
   - 如果历史末尾已经是 Assistant 角色且其 `Text` 与过滤后的 `Contents` 的 `Text` 拼接结果相同，则跳过（幂等）。
   - 否则，将过滤后的 `Contents` 作为 `ChatMessage(ChatRole.Assistant, contents)` 追加到历史末尾。
4. 调用 `session.SetInMemoryChatHistory` 写回。

### 调用示例

#### 业务端正常使用

```csharp
var manualContext = await _copilotChatManager
    .CreateManualSendMessageContextAsync(externalCancellationToken)
    .ConfigureAwait(false);

manualContext.UserChatMessage.AppendText(userMessage);

await manualContext.AppendMessagesToSessionAsync().ConfigureAwait(false);
using var _ = manualContext.StartChatting();

ChatOptions chatOptions = CreateStreamingChatOptions();
AgentSession session = await manualContext.GetAgentSessionAsync(externalCancellationToken)
    .ConfigureAwait(false);

ChatClientAgent agent = await manualContext.GetChatClientAgentAsync(
    options => options.ChatOptions = chatOptions,
    externalCancellationToken).ConfigureAwait(false);

ChatMessage[] inputMessages = [manualContext.UserChatMessage.ToChatMessage()];

var loopToken = errorCancellationTokenSource.Token;

await foreach (var update in AgentSessionStreamingHelper.RunWithHistoryCompletionAsync(
    agent, inputMessages, session, loopToken).ConfigureAwait(false))
{
    manualContext.AppendResponseUpdate(update);

    if (string.IsNullOrEmpty(update.Text))
        continue;

    await streamingPipeline.ProcessIncrementalTextAsync(
        update.Text, context, loopToken).ConfigureAwait(false);

    if (context.Errors.Count > 0)
    {
        errorCancellationTokenSource.Cancel();
        break;
    }
}
// 到这里 session 历史已经自动补全，无需任何手动处理。
```

#### 自定义 ChatClient

```csharp
ChatClientAgent agent = chatClientOverride.AsAIAgent(new ChatClientAgentOptions
{
    ChatOptions = chatOptions,
    ChatHistoryProvider = new InMemoryChatHistoryProvider(new InMemoryChatHistoryProviderOptions()),
    RequirePerServiceCallChatHistoryPersistence = true,
});

AgentSession session = await agent.CreateSessionAsync();

await foreach (var update in AgentSessionStreamingHelper.RunWithHistoryCompletionAsync(
    agent, inputMessages, session, loopToken).ConfigureAwait(false))
{
    // ...
}
// 历史已自动补全，下一轮可直接复用 session
```

## 和现有测试的关系

现有的 `CopilotChatManagerManualSendCancellationTests` 中为验证业务端行为而手工编写的 `AppendCancelledManualSendMessages`、`ContainsMessageSequence`、`GetExistingFunctionCallIds` 等逻辑，将**直接迁移到 `AgentSessionStreamingHelper` 内部实现**。

测试改为直接测 `AgentSessionStreamingHelper.RunWithHistoryCompletionAsync`，覆盖场景不变：

1. **普通流式取消**：输出一段文本后取消，验证历史包含用户输入和助手局部输出。
2. **工具调用后取消并续跑**：工具调用结束后输出两段普通文本再取消，验证历史包含用户输入、工具调用请求、工具结果、助手局部输出，且下一轮可基于此历史继续。

## 对比当前业务端代码

| 对比维度 | 当前业务端代码 | `AgentSessionStreamingHelper` |
|---|---|---|
| 调用形式 | 手动 foreach + try/catch + 手动补全 | `await foreach` + `break` |
| 历史补全 | 业务端自己写 30+ 行逻辑 | 自动，在迭代器 finally 里完成 |
| Func 委托 | - | 不需要 |
| 自定义 ChatClient | `chatClientOverride.AsAIAgent(...)` | 完全一样 |
| 取消后下一轮 | 手动补全历史后再调 | 直接再调，历史已经完整 |
| 输入消息重复追加 | 业务端自己判断（`shouldAppendInputMessages`） | 自动内容对比，幂等 |
| FunctionCallContent 去重 | 业务端自己判断 | 自动比对 CallId |
| 工具结果/文本补全 | 业务端自己判断（`shouldAppendAssistantMessages`） | 自动判断，幂等 |

## 后续实施计划

1. 在 `AgentLib` 项目中创建 `AgentSessionStreamingHelper.cs`，实现 `RunWithHistoryCompletionAsync`。
2. 将现有测试中的辅助逻辑迁移为 `AgentSessionStreamingHelper` 内部方法。
3. 修改 `CopilotChatManagerManualSendCancellationTests`，改为直接测 `AgentSessionStreamingHelper.RunWithHistoryCompletionAsync`。
4. 编译验证。
5. 业务端（`StreamingSlideGenerator`）择机迁移使用。
