using AgentLib.Model;

using Microsoft.Extensions.AI;

namespace AgentLib.Coding;

internal sealed class CompressionToolCallObserver
{
    private const string ToolName = "CompressConversation";

    private readonly CopilotChatMessage _assistantMessage;
    private readonly IMainThreadDispatcher? _mainThreadDispatcher;
    private string? _callId;

    public CompressionToolCallObserver(
        CopilotChatMessage assistantMessage,
        IMainThreadDispatcher? mainThreadDispatcher,
        CopilotChatManagerToolCallChatReducer reducer)
    {
        _assistantMessage = assistantMessage;
        _mainThreadDispatcher = mainThreadDispatcher;
        reducer.CompressionStarted += OnCompressionStarted;
        reducer.CompressionCompleted += OnCompressionCompleted;
        reducer.CompressionFailed += OnCompressionFailed;
    }

    private void OnCompressionStarted(object? sender, EventArgs e)
    {
        _callId = Guid.NewGuid().ToString("N");
        Invoke(() =>
        {
            if (_assistantMessage.Content == CopilotChatMessage.PlaceholderContent)
            {
                _assistantMessage.ClearMessageItems();
            }

            _assistantMessage.AppendFunctionCall(
                new FunctionCallContent(_callId, ToolName, new Dictionary<string, object?>()));
        });
    }

    private void OnCompressionCompleted(object? sender, IReadOnlyList<ChatMessage> messages)
    {
        string output = string.Join(
            Environment.NewLine,
            messages.Where(message => message.Role == ChatRole.Assistant).Select(message => message.Text));
        Invoke(() => _assistantMessage.AppendFunctionResult(new FunctionResultContent(_callId!, output)));
    }

    private void OnCompressionFailed(object? sender, Exception exception)
    {
        Invoke(() => _assistantMessage.AppendFunctionResult(new FunctionResultContent(_callId!, exception.ToString())));
    }

    private void Invoke(Action action)
    {
        if (_mainThreadDispatcher is not null && !_mainThreadDispatcher.CheckAccess())
        {
            _ = _mainThreadDispatcher.InvokeAsync(() =>
            {
                action();
                return Task.CompletedTask;
            });
            return;
        }

        action();
    }
}
