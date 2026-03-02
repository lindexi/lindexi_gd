using System.Collections.Generic;
using System.Linq;
using LightTextEditorPlus;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Document;
using Markdig.Syntax;

namespace SimpleWrite.Business.TextEditors;

readonly record struct TextRunPropertySetter(TextEditor TextEditor)
{
    public DocumentOffset StartOffset { get; init; } = 0;

    public void SetRunProperty(ConfigRunProperty config, SourceSpan span)
    {
        span = span with
        {
            Start = span.Start + StartOffset,
            End = span.End + StartOffset
        };
        var selection = SourceSpanToSelection(span);

        TextEditor.TextEditorCore.SetUndoRedoEnable(false, "框架内部设置文本样式，防止将内容动作记录");

        TextEditor.SetRunProperty(config, selection);

        TextEditor.TextEditorCore.SetUndoRedoEnable(true, "完成框架内部设置文本样式，启用撤销恢复");
    }

    public void TrySetRunProperty(SkiaTextRunProperty runProperty, SourceSpan span)
    {
        span = span with
        {
            Start = span.Start + StartOffset,
            End = span.End + StartOffset
        };
        var selection = SourceSpanToSelection(span);

        TextEditor.TextEditorCore.SetUndoRedoEnable(false, "框架内部设置文本样式，防止将内容动作记录");
        IEnumerable<SkiaTextRunProperty> runPropertyRange = TextEditor.GetRunPropertyRange(selection);
        var same = runPropertyRange.All(t => t == runProperty);
        if (!same)
        {
            TextEditor.SetRunProperty(runProperty, selection);
        }
        TextEditor.TextEditorCore.SetUndoRedoEnable(true, "完成框架内部设置文本样式，启用撤销恢复");
    }

    private Selection SourceSpanToSelection(SourceSpan span) => new Selection(new CaretOffset(span.Start), span.Length);
}