using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.TestsFramework;
using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests.Carets;

[TestClass]
public class CaretManagerTest
{
    [ContractTestCase]
    public void TestRunProperty()
    {
        "光标在文本最后，设置当前光标文本字符属性，追加字符串文本，追加的字符串将使用当前光标文本字符属性".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();

            // Action
            // 设置当前光标文本字符属性
            textEditorCore.DocumentManager
                .SetCurrentCaretRunProperty<LayoutOnlyRunProperty>(property => property with
                {
                    FontSize = 1000
                });
            // 追加字符串文本
            textEditorCore.AppendText(TestHelper.PlainNumberText);

            // Action
            // 追加的字符串将使用当前光标文本字符属性
            var runPropertyList = textEditorCore.DocumentManager
                .GetDifferentRunPropertyRange(textEditorCore.GetAllDocumentSelection()).ToList();
            Assert.AreEqual(1, runPropertyList.Count);
            Assert.AreEqual(1000, runPropertyList[0].FontSize);
        });

        "修改文本光标在文本中间字符，设置当前光标文本字符属性，在当前光标下输入字符串，输入的字符串将使用当前光标文本字符属性".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();
            // 先准备两个字符，用来设置光标在文本中间
            textEditorCore.DocumentManager
                .SetStyleTextRunProperty<LayoutOnlyRunProperty>(property => property with
                {
                    FontSize = 10
                });
            textEditorCore.AppendText("12");
            textEditorCore.CurrentCaretOffset = new CaretOffset(1);

            // Action
            // 设置当前光标文本字符属性
            textEditorCore.DocumentManager
                .SetCurrentCaretRunProperty<LayoutOnlyRunProperty>(property => property with
                {
                    FontSize = 1000
                });
            // 在当前光标下输入字符串
            textEditorCore.EditAndReplace("3");

            // Assert
            // 输入的字符串将使用当前光标文本字符属性
            var runPropertyList = textEditorCore.DocumentManager
                .GetRunPropertyRange(textEditorCore.GetAllDocumentSelection()).ToList();
            Assert.AreEqual(3, runPropertyList.Count);
            Assert.AreEqual(10, runPropertyList[0].FontSize);
            Assert.AreEqual(1000, runPropertyList[1].FontSize);
            Assert.AreEqual(10, runPropertyList[2].FontSize);
        });
    }
}