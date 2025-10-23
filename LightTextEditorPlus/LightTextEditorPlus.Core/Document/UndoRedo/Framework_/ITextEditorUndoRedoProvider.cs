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
    /// 是否启用撤销恢复功能
    /// </summary>
    bool IsEnable => true;
}