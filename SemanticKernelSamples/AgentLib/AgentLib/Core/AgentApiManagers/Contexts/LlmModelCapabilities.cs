namespace AgentLib.Core.AgentApiManagers.Contexts;

/// <summary>
/// 模型完整能力画像。
/// 复刻 VS Copilot、 opencode ProviderCapabilities 的全部字段。
/// </summary>
public sealed record LlmModelCapabilities
{
    /// <summary>
    /// 是否支持温度参数调节。
    /// </summary>
    public bool Temperature { get; init; } = true;

    /// <summary>
    /// 是否支持推理（思维链）。
    /// </summary>
    public bool Reasoning { get; init; } = false;

    /// <summary>
    /// 是否支持附件。
    /// </summary>
    public bool Attachment { get; init; } = false;

    /// <summary>
    /// 是否支持工具调用。
    /// </summary>
    public bool ToolCall { get; init; } = true;

    /// <summary>
    /// 输入模态能力。
    /// </summary>
    public LlmModalityCapability Input { get; init; } = new();

    /// <summary>
    /// 输出模态能力。
    /// </summary>
    public LlmModalityCapability Output { get; init; } = new();

    /// <summary>
    /// 是否支持交错多模态。
    /// </summary>
    public bool Interleaved { get; init; } = false;

    /// <summary>
    /// 是否快速模型
    /// </summary>
    public bool IsFlash { get; init; } = false;

    /// <summary>
    /// 结构化输出
    /// </summary>
    public bool ResponseFormat { get; init; } = false;
}