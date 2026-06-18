using AgentLib.Model;

namespace ChatRoom.AvaloniaShell.ViewModels;

/// <summary>
/// ViewModel 基类。继承 <see cref="NotifyBase"/>，提供公共属性。
/// </summary>
public abstract class ViewModelBase : NotifyBase
{
    private bool _isBusy;

    /// <summary>
    /// 是否正在执行耗时操作。
    /// </summary>
    public bool IsBusy
    {
        get => _isBusy;
        set => SetField(ref _isBusy, value);
    }
}
