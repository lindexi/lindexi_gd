namespace AgentLib.Core.AgentApiManagers.Contexts;

/// <summary>
/// 模型定义（框架层面），包含模型的核心元数据。
/// 产品侧应在此类型基础上扩展业务字段（定价、温度、描述等）。
/// </summary>
public record ModelDefinition
{
    /// <summary>
    /// 模型供应商名称（如 deepseek、github-copilot）。
    /// </summary>
    public string Provider { get; init; } = string.Empty;

    /// <summary>
    /// 模型名
    /// </summary>
    public string ModelName { get; init; } = string.Empty;

    /// <summary>
    /// 实际传给 API 的模型 ID（如 deepseek-v4-pro、gpt-4.1）。默认不传将和 <see cref="ModelName"/> 相同
    /// </summary>
    public string? ModelId { get; init; }

    /// <summary>
    /// 模型完整能力画像（推理、工具调用、多模态等）。
    /// </summary>
    public LlmModelCapabilities? Capabilities { get; init; }

    /// <summary>
    /// 上下文窗口大小（词元数）。
    /// </summary>
    public int? ContextWindowSize { get; init; }

    /// <summary>
    /// 最大输出词元数。
    /// </summary>
    public int? MaxOutputTokens { get; init; }
}