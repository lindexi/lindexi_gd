namespace AgentLib.Core.AgentApiManagers.Contexts;

/// <summary>
/// 单模态的输入/输出能力描述。
/// 与 VS Copilot、opencode ProviderModalities 字段完全对应。
/// </summary>
public sealed record LlmModalityCapability
{
    /// <summary>
    /// 是否支持文本。
    /// </summary>
    public bool Text { get; init; } = true;

    /// <summary>
    /// 是否支持图片。
    /// </summary>
    public bool Image { get; init; } = false;

    /// <summary>
    /// 是否支持音频。
    /// </summary>
    public bool Audio { get; init; } = false;

    /// <summary>
    /// 是否支持视频。
    /// </summary>
    public bool Video { get; init; } = false;

    /// <summary>
    /// 是否支持 PDF。
    /// </summary>
    public bool Pdf { get; init; } = false;
}