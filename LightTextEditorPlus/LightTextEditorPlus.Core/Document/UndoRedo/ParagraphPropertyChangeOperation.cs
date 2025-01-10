namespace LightTextEditorPlus.Core.Document.UndoRedo;

/// <summary>
/// 段落属性变更撤销重做
/// </summary>
public class ParagraphPropertyChangeOperation : TextValueChangeOperation<ParagraphProperty>
{
    /// <summary>
    /// 创建段落属性变更撤销重做
    /// </summary>
    /// <param name="textEditor"></param>
    /// <param name="oldValue"></param>
    /// <param name="newValue"></param>
    /// <param name="paragraphIndex"></param>
    public ParagraphPropertyChangeOperation(TextEditorCore textEditor, ParagraphProperty oldValue, ParagraphProperty newValue, int paragraphIndex) : base(textEditor, oldValue, newValue)
    {
        ParagraphIndex = paragraphIndex;
    }

    /// <summary>
    /// 段落序号
    /// </summary>
    public int ParagraphIndex { get; }

    /// <summary>
    /// 段落 调试用的属性
    /// </summary>
    private ParagraphData ParagraphData => TextEditor.DocumentManager.ParagraphManager.GetParagraph(ParagraphIndex);

    /// <inheritdoc />
    public override TextOperationType TextOperationType => TextOperationType.ChangeTextData;

    /// <inheritdoc />
    protected override void Do(ParagraphProperty value)
    {
        TextEditor.DocumentManager.SetParagraphProperty(ParagraphIndex, value);
    }
}