using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.TestsFramework;

using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests.Document.DocumentManagers;

[TestClass()]
public class DocumentManagerTests
{
    [ContractTestCase]
    public void GetCharDataRange()
    {
        //"对包含 abc 三个字符的文本框，调用 DocumentManager.GetCharDataRange 传入文档全选，可以选择出 abc 三个字符".Test(() =>
        //{
        //    // Arrange
        //    var textEditorCore = TestHelper.GetTextEditorCore();
        //    // 追加一些文本
        //    textEditorCore.AppendText("abc");

        //    // Action
        //    // 调用 DocumentManager.GetCharDataRange 传入文档全选
        //    var selection = textEditorCore.DocumentManager.GetAllDocumentSelection();
        //    var charDataRange = textEditorCore.DocumentManager.GetCharDataRange(selection).ToList();

        //    // Assert
        //    Assert.AreEqual(selection.Length, charDataRange.Count);
        //    Assert.AreEqual("a", charDataRange[0].CharObject.ToText());
        //    Assert.AreEqual("b", charDataRange[1].CharObject.ToText());
        //    Assert.AreEqual("c", charDataRange[2].CharObject.ToText());
        //});

        "对包含 abc 三个字符的文本框，调用 DocumentManager.GetCharDataRange 传入从 1 到 2 的选择，可以选择出 b 单个字符".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();
            // 追加一些文本
            textEditorCore.AppendText("abc");

            // Action
            // 调用 DocumentManager.GetCharDataRange 传入从 1 到 2 的选择
            var selection = new Selection(new CaretOffset(1), new CaretOffset(2));
            var charDataRange = textEditorCore.DocumentManager.GetCharDataRange(selection).ToList();

            // Assert
            Assert.AreEqual(selection.Length, charDataRange.Count);
            Assert.AreEqual("b", charDataRange[0].CharObject.ToText());
        });

        "非空文本，调用 DocumentManager.GetCharDataRange 传入空白选择，返回空集合".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();
            // 追加一些文本
            textEditorCore.AppendText(TestHelper.PlainNumberText);

            // Action
            // 调用 DocumentManager.GetCharDataRange 传入空白选择
            var charDataRange = textEditorCore.DocumentManager.GetCharDataRange(new Selection(textEditorCore.CurrentCaretOffset, 0));

            // Assert
            // 返回空集合
            Assert.AreEqual(false, charDataRange.Any());
        });

        "对空文本，调用 DocumentManager.GetCharDataRange 传入空白选择，返回空集合".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();
            // 啥都不做，这是一个空文本

            // Action
            // 调用 DocumentManager.GetCharDataRange 传入空白选择
            var charDataRange = textEditorCore.DocumentManager.GetCharDataRange(new Selection(new CaretOffset(0), 0));

            // Assert
            // 返回空集合
            Assert.AreEqual(false, charDataRange.Any());
        });
    }

    [ContractTestCase]
    public void SetCurrentCaretRunProperty()
    {
        "对当前的光标设置文本字符属性，在当前的光标继续输入文本，输入的文本可以使用当前的光标设置文本字符属性".Test(() =>
        {
            // Arrange
            var testPlatformProvider = new RenderManagerTestPlatformProvider();
            var testRenderManager = testPlatformProvider.TestRenderManager;
            var textEditorCore = new TextEditorCore(testPlatformProvider);

            // 对当前的光标设置文本字符属性
            var fontSize = 0d;
            // 对当前的光标设置文本字符属性
            textEditorCore.DocumentManager.SetCurrentCaretRunProperty<LayoutOnlyRunProperty>(runProperty =>
            {
                fontSize = runProperty.FontSize + 10;
                runProperty.FontSize = fontSize;
            });

            // Action
            // 在当前的光标继续输入文本
            // 当前的文本是空文本，只需要追加
            textEditorCore.AppendText("1");

            // Assert
            // 输入的文本可以使用当前的光标设置文本字符属性
            var provider = testRenderManager.CurrentRenderInfoProvider;
            Assert.IsNotNull(provider);
            // 取第一段第一行第一个字符，因为这是在空文本加上一个字符
            var paragraphRenderInfoList = provider.GetParagraphRenderInfoList().ToList();
            var line = paragraphRenderInfoList[0].GetLineRenderInfoList().ToList()[0];
            var charData = line.Argument.CharList[0];
            Assert.AreEqual(fontSize, charData.RunProperty.FontSize);
        });

        "对当前的光标设置文本字符属性，在光标移动之后，将会清空当前的设置".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();

            // 先加一个字符，让光标可以移动
            textEditorCore.AppendText("1");

            // Action
            var fontSize = 0d;
            var oldFontSize = 0d;
            // 对当前的光标设置文本字符属性
            textEditorCore.DocumentManager.SetCurrentCaretRunProperty<LayoutOnlyRunProperty>(runProperty =>
            {
                oldFontSize = runProperty.FontSize;
                fontSize = runProperty.FontSize + 10;
                runProperty.FontSize = fontSize;
            });

            // 先证明已设置成功
            Assert.AreEqual(fontSize, textEditorCore.DocumentManager.CurrentCaretRunProperty.FontSize);

            // 移动光标，期望将会清空当前的设置
            textEditorCore.CurrentCaretOffset = new CaretOffset(0);

            // Assert
            // 清空就是返回原来的字号
            Assert.AreEqual(oldFontSize, textEditorCore.DocumentManager.CurrentCaretRunProperty.FontSize);
        });

        "对当前的光标设置文本字符属性，可以获取当前的光标的字符属性获取到设置的属性".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();

            // Action
            var fontSize = 0d;
            // 对当前的光标设置文本字符属性
            textEditorCore.DocumentManager.SetCurrentCaretRunProperty<LayoutOnlyRunProperty>(runProperty =>
            {
                fontSize = runProperty.FontSize + 10;
                runProperty.FontSize = fontSize;
            });

            // Assert
            // 可以获取当前的光标的字符属性获取到设置的属性
            Assert.AreEqual(fontSize, textEditorCore.DocumentManager.CurrentCaretRunProperty.FontSize);
        });
    }

    [ContractTestCase]
    public void GetCharCount()
    {
        "插入两段文本，获取文档字符数量，将会加上换行符".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();

            // Action
            textEditorCore.AppendText("1\r\n23");

            // Assert
            Assert.AreEqual(5, textEditorCore.DocumentManager.CharCount);
        });

        "插入一行123纯文本，获取文档字符数量，可以获取到3个".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();

            // Action
            textEditorCore.AppendText("123");

            // Assert
            Assert.AreEqual(3, textEditorCore.DocumentManager.CharCount);
        });
    }
}