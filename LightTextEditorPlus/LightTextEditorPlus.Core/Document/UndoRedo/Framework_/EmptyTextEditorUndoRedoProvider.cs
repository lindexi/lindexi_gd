namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 默认的空白文本撤销恢复提供
/// </summary>
class EmptyTextEditorUndoRedoProvider : ITextEditorUndoRedoProvider
{
    public void Insert(ITextOperation textOperation)
    {
    }
}