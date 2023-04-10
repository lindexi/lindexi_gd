namespace LightTextEditorPlus.Core.Document.UndoRedo;

public abstract class TextValueChangeOperation<T> : TextOperation
{
    protected TextValueChangeOperation(TextEditorCore textEditor, T oldValue, T newValue) : base(textEditor)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }

    protected T OldValue { get; }
    protected T NewValue { get; }

    protected override void OnUndo()
    {
        Do(OldValue);
    }

    protected override void OnRedo()
    {
        Do(NewValue);
    }

    protected abstract void Do(T value);
}