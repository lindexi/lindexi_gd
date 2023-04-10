namespace LightTextEditorPlus.Core.Document.UndoRedo;

public class ParagraphPropertyChangeOperation : TextValueChangeOperation<ParagraphProperty>
{
    public ParagraphPropertyChangeOperation(TextEditorCore textEditor, ParagraphProperty oldValue, ParagraphProperty newValue, int paragraphIndex) : base(textEditor, oldValue, newValue)
    {
        ParagraphIndex = paragraphIndex;
    }

    public int ParagraphIndex { get; }

    /// <summary>
    /// 段落 调试用的属性
    /// </summary>
    private ParagraphData ParagraphData => TextEditor.DocumentManager.ParagraphManager.GetParagraph(ParagraphIndex);

    public override TextOperationType TextOperationType => TextOperationType.ChangeTextData;

    protected override void Do(ParagraphProperty value)
    {
        TextEditor.DocumentManager.SetParagraphProperty(ParagraphIndex, value);
    }
}