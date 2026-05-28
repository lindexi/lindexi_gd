using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using AgentLib.Tools;

using Microsoft.Extensions.AI;

namespace AgentLib.Tests.Fakes;

internal sealed class CopilotChatManagerTestContext
{
    public CopilotChatManagerTestContext(FakeChatClient primaryChatClient, FakeChatClient? flashChatClient = null)
    {
        PrimaryChatClient = primaryChatClient ?? throw new ArgumentNullException(nameof(primaryChatClient));
        FlashChatClient = flashChatClient;

        ChatManager = new CopilotChatManager();
        ChatManager.AgentApiEndpointManager.RegisterLanguageModelProvider(new FakeLanguageModelProvider([CreatePrimaryModel()]));

        if (flashChatClient is not null)
        {
            ChatManager.AgentApiEndpointManager.RegisterLanguageModelProvider(new FakeLanguageModelProvider([CreateFlashModel(flashChatClient)]));
        }
    }

    public CopilotChatManager ChatManager { get; }

    public FakeChatClient PrimaryChatClient { get; }

    public FakeChatClient? FlashChatClient { get; }

    public static CopilotChatManagerTestContext Create(FakeChatClient primaryChatClient, FakeChatClient? flashChatClient = null)
    {
        return new CopilotChatManagerTestContext(primaryChatClient, flashChatClient);
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
        ArgumentException.ThrowIfNullOrWhiteSpace(callId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new ChatResponseUpdate(ChatRole.Assistant, [new FunctionCallContent(callId, name, arguments)]);
    }

    public static ChatResponseUpdate AssistantFunctionResult(string callId, object? result)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(callId);
        return new ChatResponseUpdate(ChatRole.Assistant, [new FunctionResultContent(callId, result)]);
    }

    public static ChatResponseUpdate AssistantUsage(UsageDetails usageDetails)
    {
        ArgumentNullException.ThrowIfNull(usageDetails);
        return new ChatResponseUpdate(ChatRole.Assistant, [new UsageContent(usageDetails)]);
    }

    public static string GetWorkspaceToolName(string methodName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(methodName);
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
