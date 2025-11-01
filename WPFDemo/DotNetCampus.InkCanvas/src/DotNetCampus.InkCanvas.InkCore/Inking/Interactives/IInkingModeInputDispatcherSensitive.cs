namespace DotNetCampus.Inking.Interactives;

/// <summary>
/// 表示对输入调度器敏感，将被注入
/// </summary>
public interface IInkingModeInputDispatcherSensitive
{
    /// <summary>
    /// 输入调度器 此属性将由框架层注入值
    /// </summary>
    InkingModeInputDispatcher ModeInputDispatcher { set; get; }
}