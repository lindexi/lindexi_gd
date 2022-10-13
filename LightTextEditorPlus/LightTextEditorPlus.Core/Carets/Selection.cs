namespace LightTextEditorPlus.Core.Carets;

/// <summary>
/// 表示选择范围
/// </summary>
/// todo 实现选择范围
internal struct Selection
{
    /// <summary>
    /// 获取当前<see cref="Selection"/>的开始位置偏移量
    /// </summary>
    public CaretOffset StartOffset { get; }

    /// <summary>
    /// 获取当前选择的长度
    /// </summary>
    public int Length { get;}
}