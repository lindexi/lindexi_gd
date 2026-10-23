namespace AgentLib.ChatRoom.Configuration;

/// <summary>
/// 模型配置项。
/// </summary>
public sealed class ModelSetting
{
    /// <summary>
    /// 模型名称。
    /// </summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>
    /// 实际传给 API 的模型 ID。为 <see langword="null"/> 时与 <see cref="ModelName"/> 相同。
    /// </summary>
    public string? ModelId { get; set; }

    /// <summary>
    /// 是否为快速模型（轻量标记）。
    /// </summary>
    public bool IsFlash { get; set; }

    /// <summary>
    /// 是否支持视觉输入。
    /// </summary>
    public bool IsVision { get; set; }
}
