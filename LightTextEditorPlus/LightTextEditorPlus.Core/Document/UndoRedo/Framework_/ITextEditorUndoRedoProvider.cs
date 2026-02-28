namespace LightTextEditorPlus.Core.Document.UndoRedo;

/// <summary>
/// 文本撤销恢复提供
/// </summary>
public interface ITextEditorUndoRedoProvider
{
    /// <summary>
    /// 插入一个撤销恢复动作
    /// </summary>
    /// <param name="textOperation"></param>
    void Insert(ITextOperation textOperation);

    /// <summary>
    /// 执行撤销操作
    /// </summary>
    void Undo();

    /// <summary>
    /// 执行恢复操作
    /// </summary>
    void Redo();
}