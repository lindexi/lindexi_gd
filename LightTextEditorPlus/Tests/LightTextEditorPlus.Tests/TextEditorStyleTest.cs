using System.Windows;

using dotnetCampus.UITest.WPF;

using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Document;

using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Tests;

[TestClass]
public class TextEditorStyleTest
{
    [UIContractTestCase]
    public void ChangeStyle()
    {
        "未选择时，修改当前光标字符属性样式，只触发 StyleChanging 和 StyleChanged 事件，不触发布局变更".Test(() =>
        {
            // Arrange
            using var context = TestFramework.CreateTextEditorInNewWindow();
            var textEditor = context.TextEditor;
            // 只触发 StyleChanging 和 StyleChanged 事件。不用测试了，交给其他单元测试
            textEditor.LayoutCompleted += (sender, args) =>
            {
                // Assert
                // 未选择时，修改当前光标字符属性样式，只触发 StyleChanging 和 StyleChanged 事件，不触发布局变更
                Assert.Fail();
            };

            // Action
            textEditor.ToggleBold();
        });

        "修改样式时，先触发 StyleChanging 事件，再触发 StyleChanged 事件，且只触发一次".Test(() =>
        {
            // Arrange
            using var context = TestFramework.CreateTextEditorInNewWindow();
            var textEditor = context.TextEditor;

            int count = 0;
            textEditor.StyleChanging += (sender, args) =>
            {
                Assert.AreEqual(0, count);
                count++;
            };
            textEditor.StyleChanged += (sender, args) =>
            {
                Assert.AreEqual(1, count);
                count++;
            };

            // Action
            textEditor.ToggleBold();

            // Assert
            // 只触发一次，一共两个事件
            Assert.AreEqual(2, count);
        });
    }

    [UIContractTestCase]
    public void ToggleBold()
    {
        "未选择时，调用 ToggleBold 给文本当前光标设置加粗，追加文本之后，当前光标的字符属性是加粗".Test(async () =>
        {
            // Arrange
            using var context = TestFramework.CreateTextEditorInNewWindow();
            var textEditor = context.TextEditor;

            // Action
            // 这里获取到的是未选择的
            // 直接调用 ToggleBold 即可
            textEditor.ToggleBold();

            // 追加文本，预期追加文本不会导致当前光标的字符属性不加粗
            textEditor.TextEditorCore.AppendText("a");

            // Assert
            // 当前光标的字符属性是加粗
            Assert.AreEqual(FontWeights.Bold, textEditor.CurrentCaretRunProperty.FontWeight);
            await TestFramework.FreezeTestToDebug();
        });

        "未选择时，可以调用 ToggleBold 给文本当前光标设置加粗".Test(async () =>
        {
            // Arrange
            using var context = TestFramework.CreateTextEditorInNewWindow();
            var textEditor = context.TextEditor;

            // Action
            // 这里获取到的是未选择的
            // 直接调用 ToggleBold 即可
            textEditor.ToggleBold();

            // Assert
            Assert.AreEqual(FontWeights.Bold, textEditor.CurrentCaretRunProperty.FontWeight);

            // 重新调用 ToggleBold 可以去掉加粗
            textEditor.ToggleBold();
            Assert.AreEqual(FontWeights.Normal, textEditor.CurrentCaretRunProperty.FontWeight);

            await TestFramework.FreezeTestToDebug();
        });
    }

    [UIContractTestCase]
    public void TestSetRunProperty()
    {
        "整段文本字符属性设置之后，经过撤销恢复，能够还原状态".Test(async () =>
        {
            // Arrange
            using var context = TestFramework.CreateTextEditorInNewWindow();
            var textEditor = context.TextEditor;

            RunProperty runProperty = textEditor.CreateRunProperty(property => property with
            {
                FontSize = 62.5,
                FontWeight = FontWeights.Bold,
            });
            textEditor.Text = "123\nabc123";

            await textEditor.WaitForRenderCompletedAsync();

            textEditor.CurrentCaretOffset = new CaretOffset("123\n".Length, isAtLineStart: true);
            ITextParagraph paragraph = textEditor.GetCurrentCaretOffsetParagraph();

            var selection = textEditor.GetParagraphSelection(paragraph);
            var originRunProperty = textEditor.GetRunPropertyRange(in selection).First();

            // Action
            textEditor.SetRunProperty(runProperty, selection);

            // Assert
            RunProperty runProperty1 = textEditor.GetRunPropertyRange(in selection).First();
            Assert.AreEqual(runProperty, runProperty1, "设置进去的字符属性，应该能够设置成功，能够拿到传入的字符属性");
            // 预期此时就和原始的不相同的了
            Assert.AreNotEqual(originRunProperty, runProperty1);
            // 原始的应该和样式相同
            Assert.AreEqual(textEditor.StyleRunProperty, originRunProperty);

            // 再测试撤销恢复
            for (int i = 0; i < 10; i++)
            {
                // Action
                // 撤销之后，应该和原始的相同
                textEditor.TextEditorCore.UndoRedoProvider.Undo();
                // Assert
                RunProperty runProperty2 = textEditor.GetRunPropertyRange(in selection).First();
                Assert.AreEqual(originRunProperty, runProperty2, "撤销之后，应该能还原为和原来的相同的文本字符属性");

                if (i > 5)
                {
                    // 同时也要测试经过了 UI 布局渲染之后的情况
                    await Task.Delay(TimeSpan.FromMilliseconds(200));
                }

                // Action
                textEditor.TextEditorCore.UndoRedoProvider.Redo();
                // Assert
                RunProperty runProperty3 = textEditor.GetRunPropertyRange(in selection).First();
                Assert.AreEqual(runProperty, runProperty3);

                if (i > 5)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(200));
                }
            }

            await TestFramework.FreezeTestToDebug();
        });

        "设置文本字符属性之后，经过撤销恢复，能够还原状态".Test(async () =>
        {
            // Arrange
            using var context = TestFramework.CreateTextEditorInNewWindow();
            var textEditor = context.TextEditor;

            RunProperty runProperty = textEditor.CreateRunProperty(property => property with
            {
                FontSize = 62.5,
                FontWeight = FontWeights.Bold,
            });
            textEditor.Text = "123abc123";

            var selection = new Selection(new CaretOffset("123".Length), "abc".Length);
            var originRunProperty = textEditor.GetRunPropertyRange(in selection).First();

            // Action
            textEditor.SetRunProperty(runProperty, selection);

            // Assert
            RunProperty runProperty1 = textEditor.GetRunPropertyRange(in selection).First();
            Assert.AreEqual(runProperty, runProperty1, "设置进去的字符属性，应该能够设置成功，能够拿到传入的字符属性");
            // 预期此时就和原始的不相同的了
            Assert.AreNotEqual(originRunProperty, runProperty1);
            // 原始的应该和样式相同
            Assert.AreEqual(textEditor.StyleRunProperty, originRunProperty);

            // 再测试撤销恢复
            for (int i = 0; i < 10; i++)
            {
                // Action
                // 撤销之后，应该和原始的相同
                textEditor.TextEditorCore.UndoRedoProvider.Undo();
                // Assert
                RunProperty runProperty2 = textEditor.GetRunPropertyRange(in selection).First();
                Assert.AreEqual(originRunProperty, runProperty2, "撤销之后，应该能还原为和原来的相同的文本字符属性");

                if (i > 5)
                {
                    // 同时也要测试经过了 UI 布局渲染之后的情况
                    await Task.Delay(TimeSpan.FromMilliseconds(200));
                }

                // Action
                textEditor.TextEditorCore.UndoRedoProvider.Redo();
                // Assert
                RunProperty runProperty3 = textEditor.GetRunPropertyRange(in selection).First();
                Assert.AreEqual(runProperty, runProperty3);

                if (i > 5)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(200));
                }
            }

            await TestFramework.FreezeTestToDebug();
        });
    }
}