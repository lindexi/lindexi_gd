using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using AgentLib.Logging;
using AgentLib.Model;
using AgentLib.Tools;

using Microsoft.Extensions.AI;

namespace AgentLib.Tests.Fakes;

internal sealed class CopilotChatManagerTestContext
{
    public CopilotChatManagerTestContext(FakeChatClient primaryChatClient, FakeChatClient? flashChatClient = null, IMainThreadDispatcher? mainThreadDispatcher = null)
    {
        PrimaryChatClient = primaryChatClient ?? throw new ArgumentNullException(nameof(primaryChatClient));
        FlashChatClient = flashChatClient;

        ChatManager = new CopilotChatManager(new EmptyCopilotChatLogger())
        {
            MainThreadDispatcher = mainThreadDispatcher,
        };
        // 对象初始化器在构造函数之后执行，构造函数中创建的会话不带 dispatcher。
        // 创建一个新会话替换之，新会话通过对象初始化器拿到 dispatcher。
        if (mainThreadDispatcher is not null)
        {
            var newSession = new CopilotChatSession
            {
                MainThreadDispatcher = mainThreadDispatcher,
            };
            ChatManager.ChatSessions.Insert(0, newSession);
            ChatManager.SelectedSession = newSession;
        }
        ChatManager.AgentApiEndpointManager.RegisterLanguageModelProvider(new FakeLanguageModelProvider([CreatePrimaryModel()]));

        if (flashChatClient is not null)
        {
            ChatManager.AgentApiEndpointManager.RegisterLanguageModelProvider(new FakeLanguageModelProvider([CreateFlashModel(flashChatClient)]));
        }
    }

    public CopilotChatManager ChatManager { get; }

    public FakeChatClient PrimaryChatClient { get; }

    public FakeChatClient? FlashChatClient { get; }

    public static CopilotChatManagerTestContext Create(FakeChatClient primaryChatClient, FakeChatClient? flashChatClient = null, IMainThreadDispatcher? mainThreadDispatcher = null)
    {
        return new CopilotChatManagerTestContext(primaryChatClient, flashChatClient, mainThreadDispatcher);
    }

    public static ChatResponseUpdate AssistantText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return new ChatResponseUpdate(ChatRole.Assistant, [new TextContent(text)]);
    }

    public static ChatResponseUpdate AssistantReasoning(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return new ChatResponseUpdate(ChatRole.Assistant, [new TextReasoningContent(text)]);
    }

    public static ChatResponseUpdate AssistantFunctionCall(string callId, string name, IDictionary<string, object?>? arguments = null)
    {
        if (string.IsNullOrWhiteSpace(callId)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(callId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
        return new ChatResponseUpdate(ChatRole.Assistant, [new FunctionCallContent(callId, name, arguments)]);
    }

    public static ChatResponseUpdate AssistantFunctionResult(string callId, object? result)
    {
        if (string.IsNullOrWhiteSpace(callId)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(callId));
        return new ChatResponseUpdate(ChatRole.Assistant, [new FunctionResultContent(callId, result)]);
    }

    public static ChatResponseUpdate AssistantUsage(UsageDetails usageDetails)
    {
        ArgumentNullException.ThrowIfNull(usageDetails);
        return new ChatResponseUpdate(ChatRole.Assistant, [new UsageContent(usageDetails)]);
    }

    public static string GetWorkspaceToolName(string methodName)
    {
        if (string.IsNullOrWhiteSpace(methodName)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(methodName));
        return typeof(WorkspaceToolProvider).GetMethod(methodName)?.Name
               ?? throw new InvalidOperationException($"未找到工作区工具方法 {methodName}。");
    }

    private FakeLanguageModel CreatePrimaryModel()
    {
        return new FakeLanguageModel(PrimaryChatClient)
        {
            ModelDefinition = new ModelDefinition
            {
                Provider = "test",
                ModelName = "primary-model",
                ModelId = "primary-model",
                Capabilities = new LlmModelCapabilities
                {
                    ToolCall = true,
                    Reasoning = true,
                    Input = new LlmModalityCapability { Text = true },
                    Output = new LlmModalityCapability { Text = true }
                }
            }
        };
    }

    private static FakeLanguageModel CreateFlashModel(FakeChatClient flashChatClient)
    {
        return new FakeLanguageModel(flashChatClient)
        {
            ModelDefinition = new ModelDefinition
            {
                Provider = "test",
                ModelName = "flash-model",
                ModelId = "flash-model",
                Capabilities = new LlmModelCapabilities
                {
                    ToolCall = true,
                    Reasoning = true,
                    IsFlash = true,
                    Input = new LlmModalityCapability { Text = true },
                    Output = new LlmModalityCapability { Text = true }
                }
            }
        };
    }
}
