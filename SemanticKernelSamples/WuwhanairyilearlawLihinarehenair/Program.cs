using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace WuwhanairyilearlawLihinarehenair;
#pragma warning disable MAAI001

/// <summary>
/// 演示 RequirePerServiceCallChatHistoryPersistence 对 ReduceAsync 调用时机的影响。
/// 通过 FakeChatClient 模拟 LLM 多次工具调用，对比启用/不启用 Persistence 时 ReduceAsync 的调用频率。
/// </summary>
public class Program
{
    /// <summary>
    /// 程序入口：依次运行启用和不启用 Persistence 的演示。
    /// </summary>
    public static async Task Main()
    {
        Console.WriteLine("=== 演示：RequirePerServiceCallChatHistoryPersistence 对 ReduceAsync 调用时机的影响 ===");
        Console.WriteLine();
        Console.WriteLine("本演示通过 FakeChatClient 模拟 LLM 的多次工具调用（GetWeather → GetTime → 最终回复），");
        Console.WriteLine("对比启用 / 不启用 RequirePerServiceCallChatHistoryPersistence 时 ReduceAsync 的调用频率。");
        Console.WriteLine();

        // ── 演示 1：启用 RequirePerServiceCallChatHistoryPersistence ──
        await DemonstrateWithPersistenceAsync();

        Console.WriteLine();
        Console.WriteLine("============================================================");
        Console.WriteLine();

        // ── 演示 2：不启用 RequirePerServiceCallChatHistoryPersistence ──
        await DemonstrateWithoutPersistenceAsync();

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=== 演示结束 ===");
        Console.ResetColor();
    }

    /// <summary>
    /// 演示 1：启用 RequirePerServiceCallChatHistoryPersistence，
    /// 每次工具调用完成后 ReduceAsync 都会被调用，防止上下文堆积。
    /// </summary>
    private static async Task DemonstrateWithPersistenceAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("【演示 1】RequirePerServiceCallChatHistoryPersistence = true");
        Console.ResetColor();
        Console.WriteLine("预期：每次工具调用完成后，ReduceAsync 都会被调用一次，防止上下文堆积。");
        Console.WriteLine();

        var loggingReducer = new LoggingChatReducer("演示1-Reducer");

        var fakeChatClient = CreateToolCallingFakeChatClient(maxToolCallRounds: 2);

        var agent = fakeChatClient.AsAIAgent(new ChatClientAgentOptions()
        {
            ChatHistoryProvider = new InMemoryChatHistoryProvider(new InMemoryChatHistoryProviderOptions()
            {
                ChatReducer = loggingReducer
            }),
            ChatOptions = new ChatOptions()
            {
                Tools =
                [
                    AIFunctionFactory.Create(GetWeather, nameof(GetWeather)),
                    AIFunctionFactory.Create(GetTime, nameof(GetTime)),
                ],
            },
            RequirePerServiceCallChatHistoryPersistence = true,
        });

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(">>> 开始运行 Agent（启用 Persistence）...");
        Console.ResetColor();
        Console.WriteLine();

        await agent.RunStreamingAndLogToConsoleAsync([new ChatMessage(ChatRole.User, "今天天气怎么样？现在几点了？")]);

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[结果] ReduceAsync 共被调用了 {loggingReducer.ReduceCallCount} 次");
        Console.ResetColor();
    }

    /// <summary>
    /// 演示 2：不启用 RequirePerServiceCallChatHistoryPersistence（默认 false），
    /// ReduceAsync 只在初始阶段被调用，不会在每次工具调用后触发。
    /// </summary>
    private static async Task DemonstrateWithoutPersistenceAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("【演示 2】RequirePerServiceCallChatHistoryPersistence = false（默认值）");
        Console.ResetColor();
        Console.WriteLine("预期：ReduceAsync 只在对话流程结束后才被调用，不会在每次工具调用后触发。");
        Console.WriteLine();

        var loggingReducer = new LoggingChatReducer("演示2-Reducer");

        var fakeChatClient = CreateToolCallingFakeChatClient(maxToolCallRounds: 2);

        var agent = fakeChatClient.AsAIAgent(new ChatClientAgentOptions()
        {
            ChatHistoryProvider = new InMemoryChatHistoryProvider(new InMemoryChatHistoryProviderOptions()
            {
                ChatReducer = loggingReducer
            }),
            ChatOptions = new ChatOptions()
            {
                Tools =
                [
                    AIFunctionFactory.Create(GetWeather, nameof(GetWeather)),
                    AIFunctionFactory.Create(GetTime, nameof(GetTime)),
                ],
            },
            // RequirePerServiceCallChatHistoryPersistence 默认为 false，不设置
        });

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(">>> 开始运行 Agent（不启用 Persistence）...");
        Console.ResetColor();
        Console.WriteLine();

        await agent.RunStreamingAndLogToConsoleAsync([new ChatMessage(ChatRole.User, "今天天气怎么样？现在几点了？")]);

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[结果] ReduceAsync 共被调用了 {loggingReducer.ReduceCallCount} 次");
        Console.ResetColor();
    }

    /// <summary>
    /// 工具方法：获取天气。
    /// </summary>
    /// <returns>天气信息字符串</returns>
    private static string GetWeather()
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  [工具调用] GetWeather 被执行，返回：晴天 25°C");
        Console.ResetColor();
        return "晴天，25°C";
    }

    /// <summary>
    /// 工具方法：获取时间。
    /// </summary>
    /// <returns>当前时间字符串</returns>
    private static string GetTime()
    {
        var time = DateTime.Now.ToString("HH:mm:ss");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  [工具调用] GetTime 被执行，返回：{time}");
        Console.ResetColor();
        return time;
    }

    /// <summary>
    /// 创建支持工具调用的 FakeChatClient。
    /// 使用闭包计数器维护调用状态，按顺序返回：
    /// 第 1 次 LLM 请求 → 工具调用 GetWeather
    /// 第 2 次 LLM 请求 → 工具调用 GetTime
    /// 第 3 次及以后 LLM 请求 → 最终文本回复
    /// </summary>
    /// <param name="maxToolCallRounds">最大工具调用轮次数</param>
    /// <returns>配置好的 FakeChatClient 实例</returns>
    private static FakeChatClient CreateToolCallingFakeChatClient(int maxToolCallRounds)
    {
        var callCount = 0;

        var fakeChatClient = new FakeChatClient()
        {
            OnGetStreamingResponseAsync = (messages, options, cancellationToken) => CreateResponseSequenceAsync()
        };

        return fakeChatClient;

        async IAsyncEnumerable<ChatResponseUpdate> CreateResponseSequenceAsync()
        {
            var currentCall = Interlocked.Increment(ref callCount);

            if (currentCall <= maxToolCallRounds)
            {
                var toolName = currentCall == 1 ? nameof(GetWeather) : nameof(GetTime);
                var callId = $"call_{currentCall}";

                yield return new ChatResponseUpdate(ChatRole.Assistant, new List<AIContent>()
                {
                    new FunctionCallContent(callId, toolName, new Dictionary<string, object?>()
                    {
                        { "location", "深圳" }
                    })
                })
                {
                    FinishReason = ChatFinishReason.ToolCalls,
                };
            }
            else
            {
                yield return new ChatResponseUpdate(ChatRole.Assistant,
                    "根据查询结果，今天深圳是晴天，气温 25°C，现在时间是 " + DateTime.Now.ToString("HH:mm:ss") + "。")
                {
                    FinishReason = ChatFinishReason.Stop,
                };
            }
        }
    }
}

/// <summary>
/// 带有日志的 ChatReducer，记录每次 ReduceAsync 的调用以观察调用时机。
/// 当上下文消息超过 6 条时，裁剪保留最近 4 条。
/// </summary>
sealed class LoggingChatReducer : IChatReducer
{
    private readonly string _name;

    /// <summary>
    /// 初始化日志记录 ChatReducer 的新实例。
    /// </summary>
    /// <param name="name">用于日志输出区分的名称</param>
    public LoggingChatReducer(string name)
    {
        _name = name;
    }

    /// <summary>
    /// 获取 ReduceAsync 被调用的总次数。
    /// </summary>
    public int ReduceCallCount { get; private set; }

    /// <summary>
    /// 对聊天消息列表进行裁剪，防止上下文过长。
    /// 每次调用时在控制台记录调用信息和当前消息数。
    /// </summary>
    /// <param name="messages">当前聊天消息列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>裁剪后的聊天消息列表</returns>
    public Task<IEnumerable<ChatMessage>> ReduceAsync(IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken)
    {
        var messageList = messages.ToList();
        ReduceCallCount++;

        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"  [{_name}] ReduceAsync 第 {ReduceCallCount} 次被调用，当前消息数：{messageList.Count}");
        Console.ResetColor();

        // 简单策略：消息超过 6 条时裁剪，仅保留最近 4 条
        if (messageList.Count > 6)
        {
            var reduced = messageList.TakeLast(4).ToList();
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine($"  [{_name}]   上下文消息数从 {messageList.Count} 裁剪为 {reduced.Count}");
            Console.ResetColor();
            return Task.FromResult<IEnumerable<ChatMessage>>(reduced);
        }

        return Task.FromResult<IEnumerable<ChatMessage>>(messageList);
    }
}

/// <summary>
/// 简化的流式运行扩展方法，直接调用 agent.RunStreamingAsync() 输出文本和工具调用信息。
/// 推理处理对于本演示不重要，故略去。
/// </summary>
public static class AIAgentStreamingExtensions
{
    /// <summary>
    /// 对流式运行 Agent 并将输出写入控制台。
    /// </summary>
    /// <param name="agent">要运行的 AIAgent</param>
    /// <param name="messages">初始消息列表</param>
    /// <param name="session">可选会话</param>
    /// <param name="options">可选运行选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static async Task RunStreamingAndLogToConsoleAsync(
        this AIAgent agent,
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await foreach (var update in agent.RunStreamingAsync(messages, session, options, cancellationToken))
        {
            Console.Write(update.Text);
        }

        Console.WriteLine();
    }
}

/// <summary>
/// 简单的 FakeChatClient，通过委托注入响应流，方便单元测试和演示。
/// </summary>
public sealed class FakeChatClient : IChatClient
{
    /// <summary>
    /// 获取或设置 GetResponseAsync 的委托。
    /// </summary>
    public Func<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken, Task<ChatResponse>>?
        OnGetResponseAsync
    { get; set; }

    /// <summary>
    /// 获取或设置 GetStreamingResponseAsync 的委托。
    /// </summary>
    public Func<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken, IAsyncEnumerable<ChatResponseUpdate>>?
        OnGetStreamingResponseAsync
    { get; set; }

    /// <summary>
    /// 获取或设置 GetService 的委托。
    /// </summary>
    public Func<Type, object?, object?>? OnGetService { get; set; }

    /// <summary>
    /// 获取或设置 Dispose 的委托。
    /// </summary>
    public Action? OnDispose { get; set; }

    /// <inheritdoc />
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (OnGetResponseAsync is null)
        {
            throw new InvalidOperationException(
                $"{nameof(FakeChatClient)}.{nameof(OnGetResponseAsync)} has not been configured.");
        }

        return OnGetResponseAsync(messages, options, cancellationToken);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (OnGetStreamingResponseAsync is null)
        {
            throw new InvalidOperationException(
                $"{nameof(FakeChatClient)}.{nameof(OnGetStreamingResponseAsync)} has not been configured.");
        }

        return OnGetStreamingResponseAsync(messages, options, cancellationToken);
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (OnGetService is null)
        {
            return null;
        }

        return OnGetService(serviceType, serviceKey);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        OnDispose?.Invoke();
    }
}