using Microsoft.Extensions.AI;

namespace PrismCodingAgent.AvaloniaShell.ViewModels;

/// <summary>
/// 表示聊天界面中可选择的思考强度。
/// </summary>
public sealed class ReasoningEffortOptionViewModel : ViewModelBase
{
    private string _displayName;

    /// <summary>初始化思考强度选项。</summary>
    public ReasoningEffortOptionViewModel(string displayName, ReasoningEffort? value)
    {
        _displayName = displayName;
        Value = value;
    }

    /// <summary>获取当前语言下的显示名称。</summary>
    public string DisplayName
    {
        get => _displayName;
        internal set => SetField(ref _displayName, value);
    }

    /// <summary>获取发送请求使用的思考强度。</summary>
    public ReasoningEffort? Value { get; }
}
