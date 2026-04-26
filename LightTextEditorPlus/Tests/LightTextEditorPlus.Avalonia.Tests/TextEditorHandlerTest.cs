using Avalonia.Threading;

using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Editing;

namespace LightTextEditorPlus.Avalonia.Tests;

[TestClass]
public class TextEditorHandlerTest
{
    [TestMethod("Shift + 鼠标单击时，应从当前光标扩展选择范围")]
    public async Task ShiftClickShouldExtendSelectionFromCaret()
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            using var context = TestFramework.CreateTextEditorInNewWindow();
            var textEditor = context.TextEditor;
            textEditor.Text = "abcdefg";
            await textEditor.WaitForRenderCompletedAsync();

            var startOffset = new CaretOffset(2);
            var endOffset = new CaretOffset(5);
            TextPoint clickPoint = GetClickPoint(textEditor, endOffset);
            textEditor.CurrentCaretOffset = startOffset;

            var handler = new TestTextEditorHandler(textEditor);

            bool isHandled = handler.SingleClick(clickPoint, isExtendSelection: true);

            Assert.IsTrue(isHandled);
            Assert.AreEqual(new Selection(startOffset, endOffset), textEditor.CurrentSelection);
        });
    }

    [TestMethod("Shift + 鼠标单击时，应保持原选择锚点并扩展到新的点击位置")]
    public async Task ShiftClickShouldKeepSelectionAnchor()
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            using var context = TestFramework.CreateTextEditorInNewWindow();
            var textEditor = context.TextEditor;
            textEditor.Text = "abcdefg";
            await textEditor.WaitForRenderCompletedAsync();

            var selectionAnchor = new CaretOffset(4);
            var clickOffset = new CaretOffset(6);
            TextPoint clickPoint = GetClickPoint(textEditor, clickOffset);
            textEditor.CurrentSelection = new Selection(selectionAnchor, new CaretOffset(2));

            var handler = new TestTextEditorHandler(textEditor);

            bool isHandled = handler.SingleClick(clickPoint, isExtendSelection: true);

            Assert.IsTrue(isHandled);
            Assert.AreEqual(new Selection(selectionAnchor, clickOffset), textEditor.CurrentSelection);
        });
    }

    private static TextPoint GetClickPoint(TextEditor textEditor, CaretOffset caretOffset)
    {
        RenderInfoProvider renderInfoProvider = textEditor.TextEditorCore.GetRenderInfo();
        return renderInfoProvider.GetCaretRenderInfo(caretOffset).GetCaretBounds(caretThickness: 1).Center;
    }

    private sealed class TestTextEditorHandler(TextEditor textEditor) : TextEditorHandler(textEditor)
    {
        public bool SingleClick(in TextPoint clickPoint, bool isExtendSelection)
        {
            return HandleSingleClick(clickPoint, isExtendSelection);
        }
    }
}
