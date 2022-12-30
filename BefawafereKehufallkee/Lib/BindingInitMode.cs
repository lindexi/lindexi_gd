namespace BefawafereKehufallkee;

/// <summary>
/// 绑定初始化时的值传递方向
/// </summary>
public enum BindingInitMode
{
    /// <summary>
    /// 使用被绑定对象的值设置当前对象的值（默认）
    /// </summary>
    SourceToTarget = 0,

    /// <summary>
    /// 啥都不干
    /// </summary>
    None = 1,

    /// <summary>
    /// 使用当前对象的值设置被绑定对象的值
    /// </summary>
    TargetToSource = 2,
}