#if TopApiTextEditorDefinition
using System;
using System.Collections.Generic;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Document;

/// <summary>
/// 段落列表，一个空文本至少有一个段落
/// </summary>
public sealed class TextEditorParagraphList : TextReadOnlyListConverter<ITextParagraph, TextEditorParagraph>
{
    internal TextEditorParagraphList(ReadOnlyParagraphList list) : base(list, Converter)
    {
    }

    private static TextEditorParagraph Converter(ITextParagraph textParagraph)
    {
        return new TextEditorParagraph(textParagraph);
    }
}

#endif
