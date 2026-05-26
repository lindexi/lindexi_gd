using Microsoft.Extensions.AI;

using System.Reflection;

namespace AgentLib.Tests.Fakes;

internal sealed class BlockingTool
{
    private readonly TaskCompletionSource _startedTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task Started => _startedTaskCompletionSource.Task;

    public AITool CreateTool(string name, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        MethodInfo methodInfo = GetType().GetMethod(nameof(WaitAsync), BindingFlags.Instance | BindingFlags.Public)
                                ?? throw new InvalidOperationException($"未找到 {nameof(WaitAsync)} 方法。");
        return AIFunctionFactory.Create(methodInfo, this, name, description, serializerOptions: null);
    }

    [global::System.ComponentModel.DescriptionAttribute("阻塞执行，直到外部取消。")]
    public async Task<string> WaitAsync(CancellationToken cancellationToken = default)
    {
        _startedTaskCompletionSource.TrySetResult();
        await Task.Delay(Timeout.Infinite, cancellationToken);
        return "不会到达";
    }
}
