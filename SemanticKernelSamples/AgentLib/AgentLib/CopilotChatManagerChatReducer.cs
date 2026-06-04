using Microsoft.Extensions.AI;

namespace AgentLib;

class CopilotChatManagerChatReducer : IChatReducer
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