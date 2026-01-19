using System.ClientModel.Primitives;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using OpenAI.Chat;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Microsoft.Agents.AI.Reasoning;

public static class ReasoningAIAgentExtension
{
    public static IAsyncEnumerable<ReasoningAgentRunResponseUpdate> RunReasoningStreamingAsync(this AIAgent agent, ChatMessage message,
        AgentThread? thread = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return RunReasoningStreamingAsync(agent, [message], thread, options, cancellationToken);
    }

    public static async IAsyncEnumerable<ReasoningAgentRunResponseUpdate> RunReasoningStreamingAsync(this AIAgent agent, IEnumerable<ChatMessage> messages,
        AgentThread? thread = null,
        AgentRunOptions? options = null,
        [EnumeratorCancellation]
        CancellationToken cancellationToken = default)
    {
        bool? isThinking = null;
        bool isFirstOutputContent = true;

        await foreach (var agentRunResponseUpdate in agent.RunStreamingAsync(messages, thread, options, cancellationToken))
        {
            var contentIsEmpty = string.IsNullOrEmpty(agentRunResponseUpdate.Text);

            if (contentIsEmpty && agentRunResponseUpdate.RawRepresentation is Microsoft.Extensions.AI.ChatResponseUpdate streamingChatCompletionUpdate)
            {
                if (streamingChatCompletionUpdate.RawRepresentation is StreamingChatCompletionUpdate chatCompletionUpdate)
                {
#pragma warning disable SCME0001
                    ref JsonPatch patch = ref chatCompletionUpdate.Patch;
                    if (patch.TryGetJson("$.choices[0].delta"u8, out var data))
                    {
                        var jsonElement = JsonElement.Parse(data.Span);
                        if (jsonElement.TryGetProperty("reasoning_content", out var reasoningContent))
                        {
                            bool isFirstThinking = false;
                            if (isThinking is null)
                            {
                                isThinking = true;
                                isFirstThinking = true;
                            }

                            if (isThinking is true)
                            {
                                yield return new ReasoningAgentRunResponseUpdate(agentRunResponseUpdate)
                                {
                                    Reasoning = reasoningContent.ToString(),
                                    IsFirstThinking = isFirstThinking,
                                    IsFirstOutputContent = false,
                                    IsThinkingEnd = false,
                                };
                                continue;
                            }
                            else
                            {
                                Debug.Fail("不能在输出内容之后，再次进入思考");
                            }
                        }
                    }

#pragma warning restore SCME0001
                }
            }

            if (!contentIsEmpty)
            {
                var responseUpdate = new ReasoningAgentRunResponseUpdate(agentRunResponseUpdate);

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