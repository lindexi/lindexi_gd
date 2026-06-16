using AgentLib.Model;

namespace ChatRoomAvaloniaDemo.Models;

/// <summary>
/// 单个模型配置模型。描述一个 LLM 模型的定义信息。
/// </summary>
public sealed class ModelItemConfig : NotifyBase
{
    private string _modelName = string.Empty;
    private string _modelId = string.Empty;
    private string _provider = string.Empty;
    private bool _isFlash;

    /// <summary>
    /// 模型显示名称，如 "deepseek-v4-pro"、"Doubao-Seed-2.0-pro"。
    /// </summary>
    public string ModelName
    {
        get => _modelName;
        set => SetField(ref _modelName, value);
    }

    /// <summary>
    /// 实际传给 API 的模型 ID。为空时使用 <see cref="ModelName"/>。
    /// </summary>
    public string ModelId
    {
        get => _modelId;
        set => SetField(ref _modelId, value);
    }

    /// <summary>
    /// 模型供应商名称，如 "deepseek"、"Doubao"。
    /// </summary>
    public string Provider
    {
        get => _provider;
        set => SetField(ref _provider, value);
    }

    /// <summary>
    /// 是否为 Flash（轻量快速）模型。
    /// </summary>
    public bool IsFlash
    {
        get => _isFlash;
        set => SetField(ref _isFlash, value);
    }

    /// <summary>
    /// 用于界面显示的文本。格式为 "ModelName (Provider)"。
    /// </summary>
    public string DisplayText => string.IsNullOrEmpty(ModelId) || ModelId == ModelName
        ? $"{ModelName} ({Provider})"
        : $"{ModelName} [{ModelId}] ({Provider})";
}
