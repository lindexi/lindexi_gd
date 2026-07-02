

using System.ComponentModel;
using System.Runtime.CompilerServices;
using AgentLib.AgentExtensions;
using AgentLib.Core;
using AgentLib.Core.AgentApiManagers;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using System.Diagnostics;
using Microsoft.Agents.AI.Reasoning;

Console.WriteLine("=== 使用 FakeChatClient 模拟工具调用 ===");

// ========== 方式一：最简单的 FakeChatClient 工具调用演示 ==========
await DemoFakeChatClientToolCallAsync();

// ========== 方式二：使用 FakeLanguageModelProvider 集成到 AgentApiEndpointManager 流程 ==========
// await DemoWithFakeLanguageModelProviderAsync();

Console.WriteLine();
Console.WriteLine("完成！");

/// <summary>
/// 演示如何使用 FakeChatClient 通过 OnGetStreamingResponseAsync 模拟工具调用。
/// 核心原理：
/// 1. ChatClientAgent 调用 GetStreamingResponseAsync 获取流式响应
/// 2. 第一次调用时，返回包含 FunctionCallContent 的 ChatResponseUpdate
/// 3. ChatClientAgent 检测到 FunctionCallContent 后，执行对应的工具函数
/// 4. ChatClientAgent 将工具结果作为新消息，再次调用 GetStreamingResponseAsync
/// 5. 第二次调用时，返回最终文本响应
/// </summary>
async Task DemoFakeChatClientToolCallAsync()
{
    // 记录 GetStreamingResponseAsync 被调用的次数，用于区分"第一次（返回工具调用）"和"第二次（返回最终结果）"
    var callCount = 0;

    var fakeChatClient = new FakeChatClient()
    {
        OnGetStreamingResponseAsync = (messages, options, cancellationToken) =>
        {
            var currentCall = Interlocked.Increment(ref callCount);
            Console.WriteLine($"[FakeChatClient] GetStreamingResponseAsync 第 {currentCall} 次被调用");

            // 打印当前历史消息，便于调试
            foreach (var message in messages)
            {
                Console.WriteLine($"  -> 历史消息: Role={message.Role}, Text={message.Text}");
                foreach (var content in message.Contents)
                {
                    if (content is FunctionResultContent frc)
                    {
                        Console.WriteLine($"     [工具结果] {frc.CallId}: {frc.Result}");
                    }
                }
            }

            if (currentCall == 1)
            {
                // 第一次调用：返回一个工具调用，让 ChatClientAgent 执行 GetWeather 工具
                return GetToolCallStreamAsync(options, cancellationToken);
            }
            else
            {
                // 第二次调用：返回最终文本响应
                return GetFinalResponseStreamAsync(cancellationToken);
            }
        }
    };

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

    Console.WriteLine();
    Console.WriteLine($"[用户] {userMessage}");
    Console.WriteLine();

    try
    {
        await foreach (var reasoningAgentResponseUpdate in agent.RunReasoningStreamingAsync(
                           [new ChatMessage(ChatRole.User, userMessage)], session))
        {
            // 输出推理内容
            if (!string.IsNullOrEmpty(reasoningAgentResponseUpdate.Reasoning))
            {
                Console.Write($"[思考] {reasoningAgentResponseUpdate.Reasoning}");
            }

            // 输出文本内容
            if (!string.IsNullOrEmpty(reasoningAgentResponseUpdate.Text))
            {
                Console.Write($"[助手] {reasoningAgentResponseUpdate.Text}");
            }

            // 检查是否有工具调用
            foreach (var content in reasoningAgentResponseUpdate.Contents)
            {
                if (content is FunctionCallContent fcc)
                {
                    Console.WriteLine($"[工具调用] {fcc.Name}({fcc.Arguments})");
                }
                else if (content is FunctionResultContent frc)
                {
                    Console.WriteLine($"[工具结果] {frc.Result}");
                }
            }
        }

        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[错误] {ex}");
    }

    // 查看最终的消息历史
    session.TryGetInMemoryChatHistory(out var messageList);
    if (messageList is not null)
    {
        Console.WriteLine();
        Console.WriteLine("=== 最终消息历史 ===");
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
}

/// <summary>
/// 生成包含工具调用的流式响应。
/// 关键：ChatResponseUpdate 的 Contents 中包含 FunctionCallContent，
/// ChatClientAgent 会检测到并自动执行工具。
/// </summary>
async IAsyncEnumerable<ChatResponseUpdate> GetToolCallStreamAsync(
    ChatOptions? options, [EnumeratorCancellation] CancellationToken cancellationToken)
{
    // 在调用工具时，需要先确定 ChatFinishReason 为 ToolCalls，
    // 告诉 ChatClientAgent 需要处理工具调用
    // 注意：ChatResponseUpdate 通过 Contents 携带 FunctionCallContent

    // 获取工具列表中的第一个工具名称
    var toolName = options?.Tools?.FirstOrDefault() is AIFunction tool ? tool.Name : "GetWeather";

    var functionCallContent = new FunctionCallContent(
        callId: "call_001",
        name: toolName,
        arguments: new Dictionary<string, object?>()
        {
            // 工具参数（GetWeather 没有参数，但这里展示如何传递）
        });

    // 返回包含 FunctionCallContent 的 ChatResponseUpdate
    // ChatClientAgent 会从 Contents 中解析 FunctionCallContent 并执行工具
    yield return new ChatResponseUpdate(ChatRole.Assistant, [functionCallContent])
    {
        CreatedAt = DateTimeOffset.UtcNow,
        // 注意：某些实现可能需要设置 FinishReason
    };

    await Task.CompletedTask;
}

/// <summary>
/// 生成最终文本响应的流式输出。
/// </summary>
async IAsyncEnumerable<ChatResponseUpdate> GetFinalResponseStreamAsync(
    [EnumeratorCancellation] CancellationToken cancellationToken)
{
    // 逐字输出最终回复
    var finalText = "根据查询结果，当前温度是100度，非常热！请注意防暑降温。";

    foreach (var ch in finalText)
    {
        yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextContent(ch.ToString())])
        {
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    await Task.CompletedTask;
}

[Description("获取温度")]
string GetWeather()
{
    return "温度100度";
}