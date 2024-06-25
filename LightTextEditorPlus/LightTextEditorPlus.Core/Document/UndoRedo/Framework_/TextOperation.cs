namespace LightTextEditorPlus.Core.Document.UndoRedo;

/// <inheritdoc cref="T:LightTextEditorPlus.Core.Document.UndoRedo.ITextOperation"/>
public abstract class TextOperation : ITextOperation
{
    /// <inheritdoc cref="T:LightTextEditorPlus.Core.Document.UndoRedo.ITextOperation"/>
    protected TextOperation(TextEditorCore textEditor)
    {
        TextEditor = textEditor;
    }

    /// <summary>
    /// 作用的文本对象
    /// </summary>
    public TextEditorCore TextEditor { get; }

    /// <summary>
    /// 动作类型
    /// </summary>
    public abstract TextOperationType TextOperationType { get; }

    /// <inheritdoc />
    public void Undo()
    {
        TextEditor.EnterUndoRedoMode();
        try
        {
            OnUndo();
        }
        finally
        {
            TextEditor.QuitUndoRedoMode();
        }
    }

    /// <inheritdoc cref="Undo"/>
    protected abstract void OnUndo();

    /// <inheritdoc/>
    public void Redo()
    {
        TextEditor.EnterUndoRedoMode();
        try
        {
            OnRedo();
        }
        finally
        {
            TextEditor.QuitUndoRedoMode();
        }
    }

    /// <inheritdoc cref="Redo"/>
    protected abstract void OnRedo();
}