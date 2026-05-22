using System.ClientModel.Primitives;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.AI;
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
            var type = agentRunResponseUpdate.GetType();
            Debug.WriteLine(type.FullName);

            var contentIsEmpty = string.IsNullOrEmpty(agentRunResponseUpdate.Text);
            var hasReasoningContent = false;

            foreach (var aiContent in agentRunResponseUpdate.Contents)
            {
                if (aiContent is FunctionCallContent functionCallContent)
                {
                    Debug.WriteLine($"FunctionCallContent {functionCallContent.Name}");
                }
                else if (aiContent is TextReasoningContent textReasoningContent)
                {
                    hasReasoningContent = true;

                    bool isFirstThinking = false;
                    if (isThinking is null)
                    {
                        isThinking = true;
                        isFirstThinking = true;
                    }

                    if (isThinking is true)
                    {
                        yield return new ReasoningAgentResponseUpdate(agentRunResponseUpdate)
                        {
                            Reasoning = textReasoningContent.Text,
                            IsFirstThinking = isFirstThinking,
                            IsFirstOutputContent = false,
                            IsThinkingEnd = false,
                        };
                    }
                    else
                    {
                        Debug.Fail("不能在输出内容之后，再次进入思考");
                    }
                }
                else if (aiContent is TextContent textContent)
                {
                    if (!string.IsNullOrEmpty(textContent.Text))
                    {
                        contentIsEmpty = false;
                    }
                }
            }

            if (contentIsEmpty && hasReasoningContent)
            {
                continue;
            }

            var responseUpdate = new ReasoningAgentResponseUpdate(agentRunResponseUpdate);

            if (!contentIsEmpty)
            {
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
            else
            {
                // 有内容，直接输出即可
                yield return responseUpdate;
            }
        }
    }
}