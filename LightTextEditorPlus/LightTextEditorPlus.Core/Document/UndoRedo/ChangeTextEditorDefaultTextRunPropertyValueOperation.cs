namespace LightTextEditorPlus.Core.Document.UndoRedo;

/// <summary>
/// 修改文本的默认字符属性的动作
/// </summary>
public class ChangeTextEditorDefaultTextRunPropertyValueOperation : ChangeValueTextOperation<IReadOnlyRunProperty>, ITextOperation
{
    internal ChangeTextEditorDefaultTextRunPropertyValueOperation(TextEditorCore textEditor, IReadOnlyRunProperty newValue,
        IReadOnlyRunProperty oldValue) : base(textEditor,newValue, oldValue)
    {
    }

    public DocumentManager DocumentManager => TextEditor.DocumentManager;

    protected override void ApplyValue(IReadOnlyRunProperty value)
    {
        DocumentManager.SetDefaultTextRunPropertyByUndoRedo(value);
    }

    public override TextOperationType TextOperationType => TextOperationType.ChangeState;
}