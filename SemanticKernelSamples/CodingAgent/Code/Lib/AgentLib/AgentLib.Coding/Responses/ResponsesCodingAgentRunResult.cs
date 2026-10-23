using AgentLib.Model;
using Microsoft.Extensions.AI;

namespace AgentLib.Coding;

internal sealed class ResponsesCodingAgentRunResult : ICodingAgentRunResult
{
    private readonly ResponsesCodingAgentRunner _runner;

    internal ResponsesCodingAgentRunResult(ResponsesCodingAgentRunner runner)
    {
        _runner = runner;
        AssistantChatMessage = runner.MessageContext.AssistantChatMessage;
        CompletionTask = runner.RunAsync();
    }

    public CopilotChatMessage AssistantChatMessage { get; }
    public Task<string?> CompletionTask { get; }

    public Task InjectMessageAsync(IReadOnlyList<AIContent> contents,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contents);
        if (contents.Count == 0)
        {
            throw new ArgumentException("消息内容不能为空。", nameof(contents));
        }
        return _runner.InjectMessageAsync(contents, cancellationToken);
    }
}
