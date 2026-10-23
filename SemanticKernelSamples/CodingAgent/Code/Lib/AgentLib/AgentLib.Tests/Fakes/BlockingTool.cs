using Microsoft.Extensions.AI;

namespace AgentLib.Tests.Fakes;

internal sealed class BlockingTool
{
    private readonly TaskCompletionSource _startedTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task Started => _startedTaskCompletionSource.Task;

    public AITool CreateTool(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(description));

        return AIFunctionFactory.Create(WaitAsync, name, description);
    }

    [global::System.ComponentModel.DescriptionAttribute("阻塞执行，直到外部取消。")]
    public async Task<string> WaitAsync(CancellationToken cancellationToken = default)
    {
        _startedTaskCompletionSource.TrySetResult();
        await Task.Delay(Timeout.Infinite, cancellationToken);
        return "不会到达";
    }
}
