using LightTextEditorPlus;
using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Highlighters;
using Xunit;

namespace SimpleWrite.Tests;

public class HighlightStagePerformanceTests
{
    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void MovingUnchangedMultilineLinksDoesNotReapplyStyles(string newline)
    {
        var editor = new TextEditor();
        editor.AppendText($"中文 https://example.com/中文{newline}正文{newline}https://example.org/结束");
        var highlighter = new MarkdownDocumentHighlighter(editor);
        var selection = editor.GetAllDocumentSelection();
        highlighter.ApplyHighlight(editor.TextEditorCore.GetText(in selection));
        editor.TextEditorCore.EditAndReplace("字", new Selection(new CaretOffset(0), 0));
        selection = editor.GetAllDocumentSelection();
        var changes = 0;
        editor.TextEditorCore.DocumentChanged += (_, _) => changes++;

        highlighter.ApplyHighlight(editor.TextEditorCore.GetText(in selection));

        Assert.Equal(0, changes);
    }
}
