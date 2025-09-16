using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Utils;

namespace LightTextEditorPlus.Core.Exceptions;

/// <summary>
/// 字符数据被加入到多个段落的异常
/// </summary>
public class CharDataBeAddedToMultiParagraphException : TextEditorException
{
    internal CharDataBeAddedToMultiParagraphException(CharData charData, ParagraphData oldParagraph,
        ParagraphData newParagraph) : base("The CharData be added to multi Paragraph")
    {
        CharData = charData;
        _oldParagraph = oldParagraph;
        _newParagraph = newParagraph;
        TextEditor = oldParagraph.ParagraphManager.TextEditor;
    }

    private readonly ParagraphData _oldParagraph;

    private readonly ParagraphData _newParagraph;

    /// <summary>
    /// 被加入的字符
    /// </summary>
    public CharData CharData { get; }

    /// <summary>
    /// 旧的段落
    /// </summary>
    public ITextParagraph OldParagraph => _oldParagraph;

    /// <summary>
    /// 新的段落
    /// </summary>
    public ITextParagraph NewParagraph => _newParagraph;

    /// <inheritdoc />
    public override string Message =>
        $"The CharData be added to multi Paragraph. CharData={CharData.CharObject.ToText().LimitTrim(20)};OldParagraphIndex={OldParagraph.Index.Index};NewParagraphIndex={NewParagraph.Index.Index};TextEditor={TextEditor}";

    internal static void Throw
        (CharData charData, ParagraphData newParagraph) =>
        throw new CharDataBeAddedToMultiParagraphException(charData, charData.CharLayoutData!.Paragraph, newParagraph);
}