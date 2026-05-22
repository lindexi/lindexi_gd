using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;

namespace AgentLib.Core;

/// <summary>
/// 语言模型能力比较器。
/// 比较规则（按优先级从高到低）：
/// <list type="number">
/// <item><description><b>null 处理</b>：两个都为 null 则相等（返回 0）；一个为 null 则非 null 更大。</description></item>
/// <item><description><b>有无 Capabilities</b>：有 <see cref="LlmModalityCapability"/> 的大于没有的。</description></item>
/// <item><description><b>都没有 Capabilities</b>：按 <see cref="ModelDefinition.ContextWindowSize"/> → <see cref="ModelDefinition.MaxOutputTokens"/> 顺序比较，数值大的更大，null 视为更小。</description></item>
/// <item><description><b>输入模态能力（Input）</b>：按 视频（Video）→ 图片（Image）→ 音频（Audio）→ PDF（Pdf）→ 文本（Text）顺序比较，支持的一方更大，双方都支持或都不支持则继续下一项。</description></item>
/// <item><description><b>其他能力</b>：按 工具调用（ToolCall）→ 推理（Reasoning）→ 结构化输出（ResponseFormat）顺序比较，支持的一方更大。</description></item>
/// <item><description><b>快速模型</b>：非快速模型（IsFlash == false）大于快速模型（IsFlash == true）。</description></item>
/// </list>
/// </summary>
public class LanguageModelCapabilityComparer : IComparer<ILanguageModel>
{
    public int Compare(ILanguageModel? x, ILanguageModel? y)
    {
        // ---------- null 处理 ----------
        if (x is null && y is null)
        {
            return 0;
        }

        if (x is not null && y is null)
        {
            return 1;
        }

        if (x is null && y is not null)
        {
            return -1;
        }

        // 理论上不应该走到这里，因为前面已经判断过了，但为了满足编译器的非空警告
        if (x is null || y is null)
        {
            return 0;
        }

        var xCapabilities = x.ModelDefinition.Capabilities;
        var yCapabilities = y.ModelDefinition.Capabilities;

        // ========== 场景1：两个都没有 Capabilities ==========
        if (xCapabilities is null && yCapabilities is null)
        {
            return CompareNullableInt(
                       x.ModelDefinition.ContextWindowSize,
                       y.ModelDefinition.ContextWindowSize)
                   ?? CompareNullableInt(
                       x.ModelDefinition.MaxOutputTokens,
                       y.ModelDefinition.MaxOutputTokens)
                   ?? 0;
        }

        // ========== 场景2：一方有、一方没有：有的一方更大 ==========
        if (xCapabilities is not null && yCapabilities is null)
        {
            return 1;
        }

        if (xCapabilities is null && yCapabilities is not null)
        {
            return -1;
        }

        // ========== 场景3：两个都有 Capabilities ==========

        // 1) 输入模态能力：按 视频 → 图片 → 音频 → PDF → 文本 顺序比较
        var xInput = xCapabilities!.Input;
        var yInput = yCapabilities!.Input;

        int? modalityResult =
            CompareBool(xInput.Video, yInput.Video)
            ?? CompareBool(xInput.Image, yInput.Image)
            ?? CompareBool(xInput.Audio, yInput.Audio)
            ?? CompareBool(xInput.Pdf, yInput.Pdf)
            ?? CompareBool(xInput.Text, yInput.Text);

        if (modalityResult.HasValue)
        {
            return modalityResult.Value;
        }

        // 2) 工具调用 → 推理 → ResponseFormat
        int? capabilityResult =
            CompareBool(xCapabilities.ToolCall, yCapabilities.ToolCall)
            ?? CompareBool(xCapabilities.Reasoning, yCapabilities.Reasoning)
            ?? CompareBool(xCapabilities.ResponseFormat, yCapabilities.ResponseFormat);

        if (capabilityResult.HasValue)
        {
            return capabilityResult.Value;
        }

        // 3) 快速模型比较：非快速模型（IsFlash == false）大于快速模型（IsFlash == true）
        return CompareBool(!xCapabilities.IsFlash, !yCapabilities.IsFlash) ?? 0;
    }

    /// <summary>比较两个 bool 值：true 大于 false；相等时返回 null 以表示需要继续下一层比较。</summary>
    private static int? CompareBool(bool x, bool y)
    {
        if (x == y)
        {
            return null;
        }

        return x ? 1 : -1;
    }

    /// <summary>比较两个可空 int 值：有值大于 null；值越大越大；相等时返回 null 以表示需要继续下一层比较。</summary>
    private static int? CompareNullableInt(int? x, int? y)
    {
        if (x == y)
        {
            return null; // 同为 null 或同为相同数值
        }

        if (x is null)
        {
            return -1; // null 视为更小
        }

        if (y is null)
        {
            return 1;
        }

        return x.Value.CompareTo(y.Value);
    }
}