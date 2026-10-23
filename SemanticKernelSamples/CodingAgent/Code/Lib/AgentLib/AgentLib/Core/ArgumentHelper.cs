using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace AgentLib.Core;

/// <summary>
/// 参数验证辅助方法，兼容 net6.0 和 net9.0。
/// </summary>
internal static class ArgumentHelper
{
    public static void ThrowIfNullOrWhiteSpace([NotNull] string? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        if (string.IsNullOrWhiteSpace(argument))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", paramName);
        }
    }
}