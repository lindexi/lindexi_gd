using Microsoft.Extensions.AI;

using System.Reflection;

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

        MethodInfo methodInfo = GetType().GetMethod(nameof(ThrowAsync), BindingFlags.Instance | BindingFlags.Public)
                                ?? throw new InvalidOperationException($"未找到 {nameof(ThrowAsync)} 方法。");
        return AIFunctionFactory.Create(methodInfo, this, name, description, serializerOptions: null);
    }

    [global::System.ComponentModel.DescriptionAttribute("调用时抛出异常。")]
    public Task<string> ThrowAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromException<string>(_exceptionToThrow);
    }
}
