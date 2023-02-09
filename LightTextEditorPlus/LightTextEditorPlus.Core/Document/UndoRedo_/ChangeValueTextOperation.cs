namespace LightTextEditorPlus.Core.Document;

public abstract class ChangeValueTextOperation<T> : TextOperation, ITextOperation
{
    protected ChangeValueTextOperation(TextEditorCore textEditor, T newValue, T oldValue) : base(textEditor)
    {
        NewValue = newValue;
        OldValue = oldValue;
    }

    public T NewValue { get; }
    public T OldValue { get; }

    protected override void OnUndo()
    {
        ApplyValue(OldValue);
    }

    protected override void OnRedo()
    {
        ApplyValue(NewValue);
    }

    protected abstract void ApplyValue(T value);
}