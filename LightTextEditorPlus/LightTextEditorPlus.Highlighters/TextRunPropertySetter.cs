using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LightTextEditorPlus;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Highlighters.CodeHighlighters;

using Markdig.Syntax;

#if USE_AVALONIA
using RunProperty = LightTextEditorPlus.Document.SkiaTextRunProperty;
#elif USE_WPF
using RunProperty = LightTextEditorPlus.Document.RunProperty;
#endif

namespace LightTextEditorPlus.Highlighters;

internal readonly record struct TextRunPropertySetter(TextEditor TextEditor)
{
    public DocumentOffset StartOffset { get; init; } = 0;

    public string? PlainText { get; init; }

    public void SetRunProperty(ConfigRunProperty config, SourceSpan span)
    {
        var selection = SourceSpanToSelection(span);

        TextEditor.TextEditorCore.SetUndoRedoEnable(false, "框架内部设置文本样式，防止将内容动作记录");

        TextEditor.SetRunProperty(config, selection);

        TextEditor.TextEditorCore.SetUndoRedoEnable(true, "完成框架内部设置文本样式，启用撤销恢复");
    }

    public void TrySetRunProperty(ScopeType scopeType, RunProperty runProperty, SourceSpan span)
    {
        _ = scopeType;
        var selection = SourceSpanToSelection(span);

#if DEBUG
        var text = TextEditor.GetText(in selection);
        GC.KeepAlive(text);
        GC.KeepAlive(scopeType);
#endif

        TrySetRunProperty(runProperty, in selection);
    }

    /// <summary>
    /// 尝试设置字符属性
    /// </summary>
    /// <param name="runProperty"></param>
    /// <param name="selection">绝对坐标，不会叠加 <see cref="StartOffset"/> 属性</param>
    public void TrySetRunProperty(RunProperty runProperty, in Selection selection)
    {
        TextEditor.TextEditorCore.SetUndoRedoEnable(false, "框架内部设置文本样式，防止将内容动作记录");
        IEnumerable<RunProperty> runPropertyRange = TextEditor.GetRunPropertyRange(selection);
        var same = runPropertyRange.All(t => t.Equals(runProperty));
        if (!same)
        {
            TextEditor.SetRunProperty(runProperty, selection);
        }
        TextEditor.TextEditorCore.SetUndoRedoEnable(true, "完成框架内部设置文本样式，启用撤销恢复");
    }

    private Selection SourceSpanToSelection(SourceSpan span)
    {
        var absoluteStartOffset = StartOffset.Offset;

        if (span.IsEmpty)
        {
            var caretOffset = new CaretOffset(absoluteStartOffset + GetDocumentCharOffsetFromPlainText(span.Start));
            return new Selection(caretOffset, 0);
        }

        var start = absoluteStartOffset + GetDocumentCharOffsetFromPlainText(span.Start);
        var endExclusive = absoluteStartOffset + GetDocumentCharOffsetFromPlainText(span.End + 1);
        return new Selection(new CaretOffset(start), endExclusive - start);
    }

    /// <summary>
    /// 获取文档字符偏移。先尝试从 <see cref="PlainText"/> 获取文本，
    /// 若为空则从编辑器获取，再调用 <see cref="TextIndexConverter.ConvertUtf16IndexToDocumentOffset(string, int)"/>。
    /// </summary>
    private int GetDocumentCharOffsetFromPlainText(int utf16Index)
    {
        var plainText = PlainText;
        if (string.IsNullOrEmpty(plainText))
        {
            var allSelection = TextEditor.GetAllDocumentSelection();
            var selection = new Selection(new CaretOffset(StartOffset.Offset), allSelection.BehindOffset);
            plainText = TextEditor.GetText(in selection);
        }

        return TextIndexConverter.ConvertUtf16IndexToDocumentOffset(plainText ?? string.Empty, utf16Index).Offset;
    }
}
