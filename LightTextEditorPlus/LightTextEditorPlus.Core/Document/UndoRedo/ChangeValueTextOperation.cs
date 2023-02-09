namespace LightTextEditorPlus.Core.Document.UndoRedo;

/// <summary>
/// 修改值的动作
/// </summary>
/// <typeparam name="T"></typeparam>
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