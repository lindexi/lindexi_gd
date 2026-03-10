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

        _textRunPropertySetter.TrySetRunProperty(runProperty, new SourceSpan(span.Start, span.End));
    }
}

class ColorCodeStyleManager
{
    public ColorCodeStyleManager(TextEditor textEditor)
    {
        _dictionary = [];
        _dictionary[ScopeType.Keyword] = C("#569CD1");
        _dictionary[ScopeType.PlainText] = _plaintTextRunProperty = textEditor.StyleRunProperty;
        _dictionary[ScopeType.ClassName] = C("#4EC9B0");

        SkiaTextRunProperty C(string color)
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
