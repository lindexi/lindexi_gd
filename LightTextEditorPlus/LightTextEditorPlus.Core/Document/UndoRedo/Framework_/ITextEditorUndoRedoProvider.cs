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
}