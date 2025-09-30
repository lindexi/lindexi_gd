#if TopApiTextEditorDefinition
using System.Collections.Generic;
using System.Text;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Document;

/// <summary>
/// 表示文本的一个段落
/// </summary>
public readonly struct TextEditorParagraph : ITextEditorParagraph
{
    internal TextEditorParagraph(ITextParagraph textParagraph)
    {
        _textParagraph = textParagraph;
    }

    private readonly ITextParagraph _textParagraph;

    /// <inheritdoc />
    public ParagraphProperty ParagraphProperty => _textParagraph.ParagraphProperty;

    /// <inheritdoc />
    public IReadOnlyRunProperty ParagraphStartRunProperty => _textParagraph.ParagraphStartRunProperty;

    /// <inheritdoc />
    public ParagraphIndex Index => _textParagraph.Index;

    /// <inheritdoc />
    public bool IsEmptyParagraph => _textParagraph.IsEmptyParagraph;

    /// <inheritdoc />
    public int CharCount => _textParagraph.CharCount;

    /// <inheritdoc />
    public string GetText()
    {
        return _textParagraph.GetText();
    }

    /// <inheritdoc />
    public void GetText(StringBuilder stringBuilder)
    {
        _textParagraph.GetText(stringBuilder);
    }

    /// <inheritdoc />
    public IReadOnlyList<CharInfo> GetParagraphCharDataList()
    {
        return CharInfoConverter.ToCharInfoList(_textParagraph.GetParagraphCharDataList());
    }

    /// <inheritdoc />
    public DocumentOffset GetParagraphStartOffset()
    {
        return _textParagraph.GetParagraphStartOffset();
    }

    TextReadOnlyListSpan<CharData> ITextParagraph.GetParagraphCharDataList()
    {
        return _textParagraph.GetParagraphCharDataList();
    }

    /// <inheritdoc />
    public override string? ToString()
    {
        return _textParagraph.ToString();
    }
}

#endif
