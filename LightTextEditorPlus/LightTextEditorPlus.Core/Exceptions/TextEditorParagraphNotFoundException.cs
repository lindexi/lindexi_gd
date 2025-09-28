using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Core.Exceptions;

/// <summary>
/// 在文本框找不到传入的段落时抛出此异常
/// </summary>
public class TextEditorParagraphNotFoundException : TextEditorException
{
    internal TextEditorParagraphNotFoundException(TextEditorCore textEditor, ParagraphData paragraphData)
        : base(textEditor)
    {
        ParagraphData = paragraphData;
    }

    internal ParagraphData ParagraphData { get; }

    /// <summary>
    /// 段落
    /// </summary>
    public ITextParagraph Paragraph => ParagraphData;

    /// <inheritdoc />
    public override string Message => "传入的段落没有从段落列表找到，段落被删除。方法不接受传入被删除段落";
}