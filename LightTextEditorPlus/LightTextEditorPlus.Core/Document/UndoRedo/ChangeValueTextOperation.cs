namespace LightTextEditorPlus.Core.Document.UndoRedo;

/// <summary>
/// 修改值的动作
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class ChangeValueTextOperation<T> : TextOperation, ITextOperation
{
    /// <summary>
    /// 创建修改值的动作
    /// </summary>
    /// <param name="textEditor"></param>
    /// <param name="newValue"></param>
    /// <param name="oldValue"></param>
    protected ChangeValueTextOperation(TextEditorCore textEditor, T newValue, T oldValue) : base(textEditor)
    {
        NewValue = newValue;
        OldValue = oldValue;
    }

    /// <summary>
    /// 新的值
    /// </summary>
    public T NewValue { get; }

    /// <summary>
    /// 旧的值
    /// </summary>
    public T OldValue { get; }

    /// <inheritdoc />
    protected override void OnUndo()
    {
        ApplyValue(OldValue);
    }

    /// <inheritdoc />
    protected override void OnRedo()
    {
        ApplyValue(NewValue);
    }

    /// <inheritdoc />
    protected abstract void ApplyValue(T value);
}