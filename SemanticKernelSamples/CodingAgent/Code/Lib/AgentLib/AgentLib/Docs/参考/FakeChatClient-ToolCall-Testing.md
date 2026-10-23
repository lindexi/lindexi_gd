# 使用 FakeChatClient 模拟工具调用 — 测试指南

## 背景

在 AgentLib 的单元测试中，我们经常需要验证 `ChatClientAgent` 的工具调用（Tool Call）行为，而不依赖真实的 LLM API 服务。`FakeLanguageModelProvider` 及其内部的 `FakeChatClient` 正是为此设计的伪实现。

然而，`FakeChatClient` 的 `OnGetStreamingResponseAsync` 是一个纯粹的「返回什么就是什么」的委托——它不会像真实 LLM 那样自动决定何时调用工具。要模拟工具调用场景，需要理解 `ChatClientAgent` 内部的工具调用循环机制，并手动构造正确的流式响应序列。

本文档通过一个可运行的完整示例，说明如何使用 `FakeChatClient` 模拟一次完整的工具调用流程，包括：用户提问 → Agent 请求工具调用 → Agent 执行工具 → Agent 返回最终结果。

## 核心原理

`ChatClientAgent` 内部的 `RunStreamingAsync` 循环（简化描述）：

```
用户消息 → 调用 IChatClient.GetStreamingResponseAsync
         → 检查 ChatResponseUpdate.Contents 中是否有 FunctionCallContent
         → 若有：执行对应工具函数
                ↓
         将工具结果（FunctionResultContent）作为新消息追加到历史
                ↓
         再次调用 IChatClient.GetStreamingResponseAsync（携带工具结果）
                ↓
         收到最终文本响应
```

关键洞察：

1. **`ChatResponseUpdate` 没有 `FinishReason` 属性**。这与 OpenAI 原生 API不同。`ChatClientAgent` 完全通过 `Contents` 中是否包含 `FunctionCallContent` 来判断是否需要执行工具。
2. **不需要额外的标记**。只要 `ChatResponseUpdate.Contents` 中存在 `FunctionCallContent`，`ChatClientAgent` 就会自动执行工具并进入下一轮循环。
3. **`FakeChatClient` 需要实现状态机**，因为 `GetStreamingResponseAsync` 会被调用多次：第一次返回工具调用，后续返回最终结果。

## 相关类型

| 类型 | 命名空间 | 作用 |
|------|----------|------|
| `FakeChatClient` | `AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes` | 伪聊天客户端，通过 `OnGetStreamingResponseAsync` 委托模拟流式响应 |
| `FakeLanguageModelProvider` | 同上 | 将 `FakeChatClient` 包装为 `ILanguageModelProvider` |
| `FunctionCallContent` | `Microsoft.Extensions.AI` | 表示工具调用请求，放入 `ChatResponseUpdate.Contents` 触发工具执行 |
| `FunctionResultContent` | `Microsoft.Extensions.AI` | 表示工具执行结果，由 `ChatClientAgent` 自动创建并追加到历史 |
| `ChatResponseUpdate` | `Microsoft.Extensions.AI` | 流式响应的单个更新块，通过 `Contents` 携带各种内容类型 |
| `ChatClientAgent` | `Microsoft.Agents.AI` | Agent 运行时，内部自动管理工具调用循环 |

## 完整示例

以下示例来自 `Program.cs`，可直接运行验证。

### 1. 创建 FakeChatClient 并配置工具调用状态机

```csharp
var callCount = 0;

var fakeChatClient = new FakeChatClient()
{
    OnGetStreamingResponseAsync = (messages, options, cancellationToken) =>
    {
        var currentCall = Interlocked.Increment(ref callCount);

        if (currentCall == 1)
        {
            // 第一次调用：返回工具调用
            return GetToolCallStreamAsync(options, cancellationToken);
        }
        else
        {
            // 第二次及后续调用：返回最终文本响应
            return GetFinalResponseStreamAsync(cancellationToken);
        }
    }
};
```

### 2. 生成工具调用流

第一次 `GetStreamingResponseAsync` 调用时，返回包含 `FunctionCallContent` 的 `ChatResponseUpdate`：

```csharp
async IAsyncEnumerable<ChatResponseUpdate> GetToolCallStreamAsync(
    ChatOptions? options, [EnumeratorCancellation] CancellationToken cancellationToken)
{
    // 从 ChatOptions.Tools 中获取工具名称（可选），确保与注册的工具名称一致
    var toolName = options?.Tools?.FirstOrDefault() is AIFunction tool
        ? tool.Name
        : "GetWeather";

    var functionCallContent = new FunctionCallContent(
        callId: "call_001",               // 调用 ID，需唯一
        name: toolName,                    // 工具名称
        arguments: new Dictionary<string, object?>()); // 工具参数

    yield return new ChatResponseUpdate(ChatRole.Assistant, [functionCallContent])
    {
        CreatedAt = DateTimeOffset.UtcNow,
    };

    await Task.CompletedTask;
}
```

> `ChatClientAgent` 收到此 `ChatResponseUpdate` 后，会从 `Contents` 中解析 `FunctionCallContent`，匹配已在 `ChatOptions.Tools` 中注册的工具（通过 `AIFunctionFactory.Create` 创建），然后自动执行该工具函数。

### 3. 生成最终文本响应

`ChatClientAgent` 执行完工具后，会自动将 `FunctionResultContent` 作为 `tool` 角色的消息追加到历史，然后再次调用 `GetStreamingResponseAsync`。此时应返回最终的回答文本：

```csharp
async IAsyncEnumerable<ChatResponseUpdate> GetFinalResponseStreamAsync(
    [EnumeratorCancellation] CancellationToken cancellationToken)
{
    var finalText = "根据查询结果，当前温度是100度，非常热！请注意防暑降温。";

    foreach (var ch in finalText)
    {
        yield return new ChatResponseUpdate(ChatRole.Assistant,
            [new TextContent(ch.ToString())])
        {
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    await Task.CompletedTask;
}
```

### 4. 配置 Agent 并运行

```csharp
// 使用 FakeChatClient 创建 ChatClientAgent
ChatClientAgent agent = fakeChatClient.AsAIAgent(new ChatClientAgentOptions()
{
    ChatOptions = new ChatOptions()
    {
        Tools = [AIFunctionFactory.Create(GetWeather)]
    },
#pragma warning disable MAAI001
    RequirePerServiceCallChatHistoryPersistence = true,
#pragma warning restore MAAI001
});

var session = await agent.CreateSessionAsync();
var userMessage = $"今天是 {DateTime.Now}。请问天气多少";

await foreach (var update in agent.RunReasoningStreamingAsync(
                   [new ChatMessage(ChatRole.User, userMessage)], session))
{
    // update.Text — 助手文本
    // update.Reasoning — 推理内容（本示例未产生）
    // update.Contents — 可包含 FunctionCallContent / FunctionResultContent 等

    foreach (var content in update.Contents)
    {
        if (content is FunctionCallContent fcc)
            Console.WriteLine($"工具调用: {fcc.Name}({fcc.Arguments})");
        else if (content is FunctionResultContent frc)
            Console.WriteLine($"工具结果: {frc.Result}");
    }
}

// 工具函数
[Description("获取温度")]
string GetWeather()
{
    return "温度100度";
}
```

### 5. 验证消息历史

运行结束后，可以通过 session 查看完整的消息历史：

```csharp
session.TryGetInMemoryChatHistory(out var messageList);
if (messageList is not null)
{
    foreach (var message in messageList)
    {
        Console.WriteLine($"[{message.Role}] {message.Text}");
        foreach (var content in message.Contents)
        {
            if (content is FunctionCallContent fcc)
                Console.WriteLine($"  工具调用: {fcc.Name}({fcc.Arguments})");
            if (content is FunctionResultContent frc)
                Console.WriteLine($"  工具结果: {frc.Result}");
        }
    }
}
```

输出示例：

```
[user] 今天是 2026/7/2 8:54:42。请问天气多少
[assistant] 
  工具调用: _Main_g_GetWeather_0_3(...)
[tool] 
  工具结果: 温度100度
[assistant] 根据查询结果，当前温度是100度，非常热！请注意防暑降温。
```

## 消费端检测工具调用的方式

在 `ReasoningAIAgentResponseUpdate`（或直接消费 `AgentResponseUpdate`）的循环中，通过遍历 `Contents` 来感知工具调用：

```csharp
await foreach (var update in agent.RunReasoningStreamingAsync(messages, session))
{
    foreach (var content in update.Contents)
    {
        if (content is FunctionCallContent fcc)
        {
            // 模型要求执行工具
            Console.WriteLine($"工具调用: {fcc.Name}, Arguments: {fcc.Arguments}");
        }
        else if (content is FunctionResultContent frc)
        {
            // 工具执行完成（消费端通常不需要处理此情况，因为 ChatClientAgent 会自动处理）
            Console.WriteLine($"工具结果: {frc.Result}");
        }
        else if (content is TextContent text)
        {
            // 普通文本输出
            Console.Write(text.Text);
        }
    }
}
```

## 注意事项

### 1. FunctionCallContent 的 CallId 需唯一

`callId` 用于关联 `FunctionCallContent` 和其对应的 `FunctionResultContent`。在模拟中，一个工具调用对应一个唯一的 `callId`（如 `"call_001"`）。如果模拟多个并发的工具调用，每个的 `callId` 必须不同。

### 2. 工具名称匹配

`FunctionCallContent.Name` 必须与注册工具的最终名称一致。`AIFunctionFactory.Create(GetWeather)` 生成的名称是编译器生成的 `_Main_g_GetWeather_0_3` 这样的名称。在测试中，可以通过两种方式获取：

- 从 `options?.Tools` 中读取（推荐，如上文示例所示）
- 直接硬编码已知的名称（脆弱，不推荐）

### 3. ChatResponseUpdate 的 Contents 是核心渠道

所有信息（文本、工具调用、推理内容等）都应通过 `Contents` 集合传递。`ChatResponseUpdate` 没有 `FinishReason` 属性，工具调用的触发完全依赖 `Contents` 中是否包含 `FunctionCallContent`。

### 4. Streaming vs Non-Streaming

`FakeChatClient` 同时提供了 `OnGetStreamingResponseAsync` 和 `OnGetResponseAsync` 两个委托。如果测试的是非流式场景，应该设置 `OnGetResponseAsync`。当前 `ChatClientAgent` 默认使用流式路径，因此 `OnGetStreamingResponseAsync` 是必须配置的。

### 5. Multi-turn 工具调用

如果需要模拟多轮工具调用（Agent 连续调用多个工具），只需在第一次流中返回多个 `FunctionCallContent`，或让第二次 `OnGetStreamingResponseAsync` 再次返回工具调用而非最终文本，`ChatClientAgent` 会持续循环直到收到不含 `FunctionCallContent` 的响应。

## 查看示例源码

完整的可运行代码位于：
- `SemanticKernelSamples\HaylarhefairjejeceYiheciju\Program.cs`

运行方式：
```bash
cd SemanticKernelSamples\HaylarhefairjejeceYiheciju
dotnet run
```