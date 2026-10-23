using AgentLib.Model;

using Microsoft.Extensions.AI;

namespace AgentLib.Tools;

/// <summary>
/// 提供不可变的工具名称到展示规则查找。
/// </summary>
public sealed class ToolRegistrationRegistry
{
    private const int MaxPrimaryTextLength = 160;
    private const int MaxSecondaryTextLength = 100;
    private readonly IReadOnlyDictionary<string, ToolRegistration> _registrations;

    /// <summary>
    /// 创建工具展示规则注册表。
    /// </summary>
    /// <param name="registrations">工具注册项。</param>
    public ToolRegistrationRegistry(IEnumerable<ToolRegistration> registrations)
    {
        ArgumentNullException.ThrowIfNull(registrations);
        var registrationMap = new Dictionary<string, ToolRegistration>(StringComparer.Ordinal);
        foreach (ToolRegistration registration in registrations)
        {
            ArgumentNullException.ThrowIfNull(registration);
            if (!registrationMap.TryAdd(registration.Tool.Name, registration))
            {
                throw new InvalidOperationException($"工具名称重复注册：{registration.Tool.Name}");
            }
        }

        _registrations = registrationMap;
    }

    /// <summary>
    /// 获取空注册表。
    /// </summary>
    public static ToolRegistrationRegistry Empty { get; } = new([]);

    /// <summary>
    /// 尝试为函数调用生成安全、限长的展示快照。
    /// </summary>
    /// <param name="functionCallContent">函数调用内容。</param>
    /// <returns>已注册工具的展示快照；未知工具返回 <see langword="null"/>。</returns>
    public ToolCallPresentation? CreatePresentation(FunctionCallContent functionCallContent)
    {
        ArgumentNullException.ThrowIfNull(functionCallContent);
        if (!_registrations.TryGetValue(functionCallContent.Name, out ToolRegistration? registration))
        {
            return null;
        }

        ToolCallPresentation presentation;
        try
        {
            presentation = registration.CreatePresentation?.Invoke(functionCallContent.Arguments ?? new Dictionary<string, object?>())
                ?? new ToolCallPresentation(null, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            presentation = new ToolCallPresentation(null, null);
        }

        return presentation with
        {
            PrimaryText = Truncate(presentation.PrimaryText, MaxPrimaryTextLength),
            SecondaryText = Truncate(presentation.SecondaryText, MaxSecondaryTextLength),
            FullTargetText = Normalize(presentation.FullTargetText),
        };
    }

    private static string? Truncate(string? value, int maxLength)
    {
        string? normalizedValue = Normalize(value);
        if (normalizedValue is null || normalizedValue.Length <= maxLength)
        {
            return normalizedValue;
        }

        int length = maxLength - 1;
        if (length > 0 && char.IsHighSurrogate(normalizedValue[length - 1]))
        {
            length--;
        }

        return normalizedValue[..length] + "…";
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
