namespace LightTextEditorPlus.Core.Document.UndoRedo;

/// <summary>
/// 文本库里面的值变更撤销重做
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class TextValueChangeOperation<T> : TextOperation
{
    /// <summary>
    /// 创建文本库里面的值变更撤销重做
    /// </summary>
    /// <param name="textEditor"></param>
    /// <param name="oldValue"></param>
    /// <param name="newValue"></param>
    protected TextValueChangeOperation(TextEditorCore textEditor, T oldValue, T newValue) : base(textEditor)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }

    /// <summary>
    /// 旧值
    /// </summary>
    protected T OldValue { get; }

    /// <summary>
    /// 新值
    /// </summary>
    protected T NewValue { get; }

    /// <inheritdoc />
    protected override void OnUndo()
    {
        Do(OldValue);
    }

    /// <inheritdoc />
    protected override void OnRedo()
    {
        Do(NewValue);
    }

    /// <summary>
    /// 应用值
    /// </summary>
    /// <param name="value"></param>
    protected abstract void Do(T value);
}