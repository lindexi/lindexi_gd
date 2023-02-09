namespace LightTextEditorPlus.Core.Document;

public abstract class TextOperation : ITextOperation
{
    protected TextOperation(TextEditorCore textEditor)
    {
        TextEditor = textEditor;
    }

    public TextEditorCore TextEditor { get; }
    public abstract TextOperationType TextOperationType { get; }

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
    protected abstract void OnUndo();

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
    protected abstract void OnRedo();

}