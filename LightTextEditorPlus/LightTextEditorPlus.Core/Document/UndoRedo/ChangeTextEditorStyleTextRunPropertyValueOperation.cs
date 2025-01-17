namespace LightTextEditorPlus.Core.Document.UndoRedo;

/// <summary>
/// 修改文本的样式字符属性的动作
/// </summary>
public class ChangeTextEditorStyleTextRunPropertyValueOperation : ChangeValueTextOperation<IReadOnlyRunProperty>, ITextOperation
{
    internal ChangeTextEditorStyleTextRunPropertyValueOperation(TextEditorCore textEditor, IReadOnlyRunProperty newValue,
        IReadOnlyRunProperty oldValue) : base(textEditor,newValue, oldValue)
    {
    }

    private DocumentManager DocumentManager => TextEditor.DocumentManager;

    /// <inheritdoc />
    protected override void ApplyValue(IReadOnlyRunProperty value)
    {
        DocumentManager.SetStyleTextRunPropertyByUndoRedo(value);
    }

    /// <inheritdoc />
    public override TextOperationType TextOperationType => TextOperationType.ChangeState;
}
