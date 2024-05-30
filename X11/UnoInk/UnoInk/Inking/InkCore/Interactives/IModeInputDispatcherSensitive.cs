namespace UnoInk.Inking.InkCore.Interactives;

/// <summary>
/// 表示对输入调度器敏感，将被注入
/// </summary>
public interface IModeInputDispatcherSensitive
{
    /// <summary>
    /// 输入调度器 此属性将由框架层注入值
    /// </summary>
    ModeInputDispatcher ModeInputDispatcher { set; get; }
    
    void Fx()
    {

    }
}
