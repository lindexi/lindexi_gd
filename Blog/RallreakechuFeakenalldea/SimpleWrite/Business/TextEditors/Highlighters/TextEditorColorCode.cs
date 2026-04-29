using LightTextEditorPlus;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Primitive;

using Microsoft.CodeAnalysis.Text;

using SimpleWrite.Business.TextEditors.Highlighters.CodeHighlighters;

using SkiaSharp;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Markdig.Syntax;

namespace SimpleWrite.Business.TextEditors.Highlighters;

internal record TextEditorColorCode : IColorCode
{
    public TextEditorColorCode(TextEditor textEditor, DocumentOffset startOffset)
    {
        var setter = new TextRunPropertySetter(textEditor)
        {
            StartOffset = startOffset
        };
        _textRunPropertySetter = setter;

        _styleManager = new ColorCodeStyleManager(textEditor);
    }

    private readonly ColorCodeStyleManager _styleManager;

    private readonly TextRunPropertySetter _textRunPropertySetter;

    public void FillCodeColor(TextSpan span, ScopeType scope)
    {
        var runProperty = _styleManager.GetRunProperty(scope);

        _textRunPropertySetter.TrySetRunProperty(scope, runProperty, new SourceSpan(span.Start, span.End - 1/*为什么要减1呢？这是因为 SourceSpan 是左右都闭，而TextSpan 是左闭右开的*/));
    }
}

class ColorCodeStyleManager
{
    public ColorCodeStyleManager(TextEditor textEditor)
    {
        _dictionary = [];
        _plaintTextRunProperty = textEditor.StyleRunProperty;

        Span<(ScopeType Scope, string Color)> span =
        [
            (ScopeType.Comment,"#579A4C"),
            (ScopeType.ClassName,"#4EC9B0"),
            (ScopeType.Comment,"#579A4C"),
            (ScopeType.Keyword,"#569CD1"),
            (ScopeType.String,"#BD9283"),
            (ScopeType.Number,"#A7BC9C"),
            (ScopeType.Brackets,"#179FFF"),
            (ScopeType.Variable,"#9CDCFD"),
            (ScopeType.Invocation,"#DCDCAA"),
            (ScopeType.DeclarationTypeSyntax,"#4EC9B0"),
        ];

        foreach (var (scope, color) in span)
        {
            _dictionary[scope] = CreateRunProperty(color);
        }

        _dictionary[ScopeType.PlainText] = _plaintTextRunProperty = textEditor.StyleRunProperty;


        SkiaTextRunProperty CreateRunProperty(string color)
        {
            return textEditor.StyleRunProperty with
            {
                Foreground = new SolidColorSkiaTextBrush(SKColor.Parse(color))
            };
        }
    }

    private readonly SkiaTextRunProperty _plaintTextRunProperty;
    private readonly Dictionary<ScopeType, SkiaTextRunProperty> _dictionary;

    public SkiaTextRunProperty GetRunProperty(ScopeType scope)
    {
        return _dictionary.GetValueOrDefault(scope, _plaintTextRunProperty);
    }
}
