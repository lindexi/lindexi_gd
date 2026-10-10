using AgentLib.Model;

namespace CodingChatRoom.AvaloniaShell.ViewModels;

/// <summary>
/// 为 Shell ViewModel 提供属性变更通知和通用忙碌状态。
/// </summary>
public abstract class ViewModelBase : NotifyBase
{
    private bool _isBusy;

    /// <summary>
    /// 获取或设置当前 ViewModel 是否正在执行异步操作。
    /// </summary>
    public bool IsBusy
    {
        get => _isBusy;
        protected set => SetField(ref _isBusy, value);
    }
}
