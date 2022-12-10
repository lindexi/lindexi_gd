namespace LightTextEditorPlus.Core.Document;

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
}