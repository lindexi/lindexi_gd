using System.ClientModel.Primitives;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using OpenAI.Chat;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Microsoft.Agents.AI.Reasoning;

public static class ReasoningAIAgentExtension
{
    public static IAsyncEnumerable<ReasoningAgentResponseUpdate> RunReasoningStreamingAsync(this AIAgent agent, ChatMessage message,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return RunReasoningStreamingAsync(agent, [message], session, options, cancellationToken);
    }

    public static async IAsyncEnumerable<ReasoningAgentResponseUpdate> RunReasoningStreamingAsync(this AIAgent agent, IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        [EnumeratorCancellation]
        CancellationToken cancellationToken = default)
    {
        bool? isThinking = null;
        bool isFirstOutputContent = true;

        await foreach (AgentResponseUpdate agentRunResponseUpdate in agent.RunStreamingAsync(messages, session, options, cancellationToken))
        {
            var contentIsEmpty = string.IsNullOrEmpty(agentRunResponseUpdate.Text);

            if (contentIsEmpty && agentRunResponseUpdate.RawRepresentation is Microsoft.Extensions.AI.ChatResponseUpdate streamingChatCompletionUpdate)
            {
                if (streamingChatCompletionUpdate.RawRepresentation is StreamingChatCompletionUpdate chatCompletionUpdate)
                {
#pragma warning disable SCME0001 // Patch 属性是实验性内容
                    ref JsonPatch patch = ref chatCompletionUpdate.Patch;
                    if (patch.TryGetJson("$.choices[0].delta"u8, out var data))
                    {
                        var jsonElement = JsonElement.Parse(data.Span);
                        if (jsonElement.TryGetProperty("reasoning_content", out var reasoningContent))
                        {
                            // 拿到的 reasoningContent 就是思考内容
                        }
                    }

#pragma warning restore SCME0001
                }
            }

            if (!contentIsEmpty)
            {
                var responseUpdate = new ReasoningAgentResponseUpdate(agentRunResponseUpdate);

                if (isFirstOutputContent)
                {
                    responseUpdate.IsFirstOutputContent = true;
                }

                if (isThinking is true && isFirstOutputContent)
                {
                    responseUpdate.IsThinkingEnd = true;
                }

                isFirstOutputContent = false;
                isThinking = false;

                yield return responseUpdate;
            }
        }
    }
}