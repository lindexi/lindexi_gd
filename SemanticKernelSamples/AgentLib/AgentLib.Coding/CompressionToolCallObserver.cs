using AgentLib.Model;
using AgentLib.Reducers;

using Microsoft.Extensions.AI;

namespace AgentLib.Coding;

internal sealed class CompressionToolCallObserver(
    CopilotChatMessage assistantMessage,
    IMainThreadDispatcher? mainThreadDispatcher) : ICopilotChatCompressionObserver
{
    private const string ToolName = "压缩对话";
    private const string PrimaryText = "自动压缩上下文";

    private string? _callId;

    public Task CompressionStartedAsync(
        CopilotChatCompressionStatistics statistics,
        CancellationToken cancellationToken)
    {
        return UpdateMessageAsync(() =>
        {
            _callId = Guid.NewGuid().ToString("N");
            assistantMessage.AppendFunctionCall(
                new FunctionCallContent(_callId, ToolName, CreateArguments(statistics)),
                new ToolCallPresentation(PrimaryText, "正在压缩", null));
        }, cancellationToken);
    }

    public Task CompressionCompletedAsync(
        CopilotChatCompressionResult result,
        CancellationToken cancellationToken)
    {
        return UpdateMessageAsync(() =>
        {
            string callId = GetCallId();
            assistantMessage.AppendFunctionCall(
                new FunctionCallContent(callId, ToolName, CreateArguments(result.Statistics)),
                new ToolCallPresentation(
                    PrimaryText,
                    $"{result.Statistics.OriginalMessageCount} 条 → {result.ReducedMessageCount} 条",
                    null));
            assistantMessage.AppendFunctionResult(
                new FunctionResultContent(callId, FormatSummary(result.SummaryContents)));
            _callId = null;
        }, cancellationToken);
    }

    public Task CompressionFailedAsync(
        CopilotChatCompressionStatistics statistics,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return UpdateMessageAsync(() =>
        {
            string callId = GetCallId();
            assistantMessage.AppendFunctionCall(
                new FunctionCallContent(callId, ToolName, CreateArguments(statistics)),
                new ToolCallPresentation(PrimaryText, "压缩失败", null));
            assistantMessage.AppendFunctionResult(
                new FunctionResultContent(
                    callId,
                    $"自动压缩失败，已保留原始上下文。{Environment.NewLine}{exception.Message}"));
            _callId = null;
        }, cancellationToken);
    }

    private Task UpdateMessageAsync(Action action, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (mainThreadDispatcher is not null && !mainThreadDispatcher.CheckAccess())
        {
            return mainThreadDispatcher.InvokeAsync(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                action();
                return Task.CompletedTask;
            });
        }

        action();
        return Task.CompletedTask;
    }

    private string GetCallId() => _callId
        ?? throw new InvalidOperationException("自动压缩尚未开始。");

    private static Dictionary<string, object?> CreateArguments(CopilotChatCompressionStatistics statistics) => new()
    {
        ["压缩前消息数"] = statistics.OriginalMessageCount,
        ["压缩消息数"] = statistics.CompressedMessageCount,
        ["压缩字符数"] = statistics.OriginalCharacterCount,
        ["触发阈值"] = statistics.CharacterThreshold,
    };

    private static string FormatSummary(IReadOnlyList<AIContent> summaryContents)
    {
        string summary = string.Concat(summaryContents
            .OfType<TextContent>()
            .Select(content => content.Text));
        return string.IsNullOrWhiteSpace(summary)
            ? "对话已压缩，但压缩器未提供可展示的文本摘要。"
            : summary;
    }
}
