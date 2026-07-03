using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Editing;
using MSTest.Extensions.Contracts;
using dotnetCampus.UITest.WPF;

namespace LightTextEditorPlus.Tests;

[TestClass]
public class TextEditorHandlerTest
{
    [UIContractTestCase]
    public void ShiftClickShouldExtendSelectionFromCaret()
    {
        "Shift + 鼠标单击时，应从当前光标扩展选择范围".Test(async () =>
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

    [UIContractTestCase]
    public void ShiftClickShouldKeepSelectionAnchor()
    {
        "Shift + 鼠标单击时，应保持原选择锚点并扩展到新的点击位置".Test(async () =>
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

    [UIContractTestCase]
    public void DragSelectShouldSelectToEmptyParagraph()
    {
        "鼠标拖拽选择到空段时，应扩展选择到空段行首".Test(async () =>
        {
            using var context = TestFramework.CreateTextEditorInNewWindow();
            var textEditor = context.TextEditor;
            textEditor.Text = "1\n\n2";
            await textEditor.WaitForRenderCompletedAsync();

            var startOffset = new CaretOffset(0);
            var emptyParagraphOffset = new CaretOffset(2, isAtLineStart: true);
            TextPoint dragPoint = GetClickPoint(textEditor, emptyParagraphOffset);
            textEditor.CurrentSelection = new Selection(startOffset, startOffset);

            var handler = new TestTextEditorHandler(textEditor);
            handler.SetClickCountForTest(1);

            bool isHandled = handler.DragSelect(dragPoint);

            Assert.IsTrue(isHandled);
            Assert.AreEqual(new Selection(startOffset, emptyParagraphOffset), textEditor.CurrentSelection);
        });
    }

    [UIContractTestCase]
    public void ShiftRightShouldExtendSelectionFromHandler()
    {
        "键盘 Shift + Right 处理应通过处理器扩展选择".Test(async () =>
        {
            using var context = TestFramework.CreateTextEditorInNewWindow();
            var textEditor = context.TextEditor;
            textEditor.Text = "abcdefg";
            await textEditor.WaitForRenderCompletedAsync();

            var startOffset = new CaretOffset(2);
            textEditor.CurrentCaretOffset = startOffset;
            var handler = new TestTextEditorHandler(textEditor);

            handler.Select(SelectionType.ShiftRight);

            Assert.AreEqual(new Selection(startOffset, new CaretOffset(3)), textEditor.CurrentSelection);
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

        public bool DragSelect(in TextPoint textPoint)
        {
            return HandleDragSelect(textPoint);
        }

        public void SetClickCountForTest(int clickCount)
        {
            var inputGestureField = typeof(TextEditorHandler).GetField("_inputGesture", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            object inputGesture = inputGestureField!.GetValue(this)!;
            var clickCountField = inputGesture.GetType().GetField("<ClickCount>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            clickCountField!.SetValue(inputGesture, clickCount);
        }

        public new void Select(SelectionType selectionType)
        {
            base.Select(selectionType);
        }
    }
}
