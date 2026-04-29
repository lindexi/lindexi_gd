using LightTextEditorPlus;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Highlighters.CodeHighlighters;

using Markdig.Syntax;

using Microsoft.CodeAnalysis.Text;

using System;
using System.Collections.Generic;

#if USE_AVALONIA
using RunProperty = LightTextEditorPlus.Document.SkiaTextRunProperty;
using LightTextEditorPlus.Primitive;
using SkiaSharp;
#elif USE_WPF
using RunProperty = LightTextEditorPlus.Document.RunProperty;
#endif

namespace LightTextEditorPlus.Highlighters;

internal sealed record TextEditorColorCode : IColorCode
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

internal sealed class ColorCodeStyleManager
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


        RunProperty CreateRunProperty(string color)
        {
            return textEditor.StyleRunProperty with
            {
                Foreground = CreateForeground(color)
            };
        }
    }

    private readonly RunProperty _plaintTextRunProperty;
    private readonly Dictionary<ScopeType, RunProperty> _dictionary;

    public RunProperty GetRunProperty(ScopeType scope)
    {
        return _dictionary.GetValueOrDefault(scope, _plaintTextRunProperty);
    }

#if USE_AVALONIA
    private static SolidColorSkiaTextBrush CreateForeground(string color)
        => new(SKColor.Parse(color));
#elif USE_WPF
    private static ImmutableBrush CreateForeground(string color)
    {
        var mediaColor = (System.Windows.Media.Color) System.Windows.Media.ColorConverter.ConvertFromString(color)!;
        var brush = new System.Windows.Media.SolidColorBrush(mediaColor);
        brush.Freeze();
        return new ImmutableBrush(brush);
    }
#endif
}
