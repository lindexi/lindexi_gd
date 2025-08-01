namespace LightTextEditorPlus.Core.Document.UndoRedo;

/// <summary>
/// 文本动作影响类型
/// </summary>
public enum TextOperationType
{
    /// <summary>
    /// 修改数据，修改数据的要求按照顺序撤销
    /// </summary>
    ChangeTextData,

    /// <summary>
    /// 修改文本的状态，修改状态本身可以不记录撤销栈
    /// </summary>
    ChangeState,
}