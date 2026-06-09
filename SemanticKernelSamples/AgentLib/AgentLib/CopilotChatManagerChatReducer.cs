using Microsoft.Extensions.AI;

namespace AgentLib;

/// <summary>
/// 基于 LLM 摘要的 <see cref="IChatReducer"/> 实现。
/// 提取开头的 System Prompt 保留，对剩余消息调用 LLM 生成摘要以压缩上下文。
/// </summary>
public class CopilotChatManagerChatReducer : IChatReducer
{
    public CopilotChatManagerChatReducer(IChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    private readonly IChatClient _chatClient;

    public async Task<IEnumerable<ChatMessage>> ReduceAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken)
    {
        var input = messages.ToList();

        if (input.Count == 0)
        {
            return input;
        }

        List<ChatMessage> firstSystemPromptMessage = [];

        while (input.Count > 0)
        {
            if (input[0].Role == ChatRole.System)
            {
                firstSystemPromptMessage.Add(input[0]);
                input.RemoveAt(0);
            }
            else
            {
                break;
            }
        }

        // 前后都应该加上系统提示词
        input.Add(new ChatMessage(ChatRole.System, "你是一个总结助手，将以下的对话内容进行总结。请不要回答任何的问题，只做总结对话的工作"));

        input.Add(new ChatMessage(ChatRole.System, DefaultSummarizationPrompt));

        var chatResponse = await _chatClient.GetResponseAsync(input, cancellationToken: cancellationToken);
        var result = firstSystemPromptMessage;
        result.AddRange(chatResponse.Messages);

        return result;
    }

    public const string DefaultSummarizationPrompt
        = """
          **Generate a clear and complete summary of the entire conversation in no more than five sentences.**

          The summary must always:
          - Reflect contributions from both the user and the assistant
          - Preserve context to support ongoing dialogue
          - Incorporate any previously provided summary
          - Emphasize the most relevant and meaningful points

          The summary must never:
          - Offer critique, correction, interpretation, or speculation
          - Highlight errors, misunderstandings, or judgments of accuracy
          - Comment on events or ideas not present in the conversation
          - Omit any details included in an earlier summary
          """;
}