using Microsoft.Extensions.AI;

namespace AgentLib.Tests.Fakes;

/// <summary>
/// 用于测试工具异常行为的 Fake 工具，调用时抛出指定异常。
/// </summary>
internal sealed class ThrowingTool
{
    private readonly Exception _exceptionToThrow;

    public ThrowingTool(Exception exceptionToThrow)
    {
        _exceptionToThrow = exceptionToThrow ?? throw new ArgumentNullException(nameof(exceptionToThrow));
    }

    public AITool CreateTool(string name, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        return AIFunctionFactory.Create(ThrowAsync, name, description);
    }

    [global::System.ComponentModel.DescriptionAttribute("调用时抛出异常。")]
    public Task<string> ThrowAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromException<string>(_exceptionToThrow);
    }
}
