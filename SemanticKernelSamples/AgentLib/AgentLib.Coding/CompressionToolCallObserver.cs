using AgentLib.Model;
using AgentLib.Reducers;
using Microsoft.Extensions.AI;

namespace AgentLib.Coding;

internal sealed class CompressionToolCallObserver : IChatReducer
{
    private const string ToolName = "CompressConversation";
    private readonly CodingRunContext _context;
    private readonly CopilotChatManagerToolCallChatReducer _reducer;
    private Task _outputTask = Task.CompletedTask;
    private string? _callId;

    public CompressionToolCallObserver(CodingRunContext context, CopilotChatManagerToolCallChatReducer reducer)
    {
        _context = context;
        _reducer = reducer;
        reducer.CompressionStarted += OnCompressionStarted;
        reducer.CompressionCompleted += OnCompressionCompleted;
        reducer.CompressionFailed += OnCompressionFailed;
    }

    public async Task<IEnumerable<ChatMessage>> ReduceAsync(IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _reducer.ReduceAsync(messages, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await _outputTask.ConfigureAwait(false);
        }
    }

    private void OnCompressionStarted(object? sender, EventArgs e)
    {
        string callId = Guid.NewGuid().ToString("N");
        _callId = callId;
        _outputTask = AppendAsync(_outputTask, () =>
        {
            _context.Output.ClearPlaceholder();
            _context.Output.AppendFunctionCall(
                new FunctionCallContent(callId, ToolName, new Dictionary<string, object?>()));
        });
    }

    private void OnCompressionCompleted(object? sender, IReadOnlyList<ChatMessage> messages)
    {
        string output = string.Join(Environment.NewLine,
            messages.Where(message => message.Role == ChatRole.Assistant).Select(message => message.Text));
        AppendResult(output);
    }

    private void OnCompressionFailed(object? sender, Exception exception) => AppendResult(exception.ToString());

    private void AppendResult(string result)
    {
        string callId = _callId ?? throw new InvalidOperationException("压缩尚未开始。");
        _outputTask = AppendAsync(_outputTask,
            () => _context.Output.AppendFunctionResult(new FunctionResultContent(callId, result)));
    }

    private async Task AppendAsync(Task previous, Action action)
    {
        await previous.ConfigureAwait(false);
        await _context.InvokeOutputAsync(action).ConfigureAwait(false);
    }
}
