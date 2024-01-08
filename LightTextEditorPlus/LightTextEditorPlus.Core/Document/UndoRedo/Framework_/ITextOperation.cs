namespace LightTextEditorPlus.Core.Document.UndoRedo;

/// <summary>
/// 表示一个文本动作
/// </summary>
public interface ITextOperation
{
    /// <summary>
    /// 撤销
    /// </summary>
    void Undo();

    /// <summary>
    /// 恢复
    /// </summary>
    void Redo();

    /// <summary>
    /// 文本动作影响类型
    /// </summary>
    TextOperationType TextOperationType { get; }
}