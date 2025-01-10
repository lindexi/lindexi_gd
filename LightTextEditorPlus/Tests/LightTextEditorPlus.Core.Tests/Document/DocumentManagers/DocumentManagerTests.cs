using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.TestsFramework;

using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests.Document.DocumentManagers;

[TestClass()]
public class DocumentManagerTests
{
    [ContractTestCase]
    public void SetRunProperty()
    {
        "从后向前选择一段内容，调用 DocumentManager.SetRunProperty 设置文本字符属性，不会影响字符顺序".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();
            // 随便写一些字符
            var text = "123123";
            textEditorCore.AppendText(text);

            var fontSize = 100;
            var fontName = "Test";

            // Action
            // 从后向前选择一段内容
            var selection = new Selection(new CaretOffset(5), new CaretOffset(3));
            // 调用 DocumentManager.SetRunProperty 设置文本字符属性
            textEditorCore.DocumentManager.SetRunProperty((LayoutOnlyRunProperty runProperty) =>
            {
                return runProperty with
                {
                    FontSize = fontSize,
                    FontName = new FontName(fontName)
                };
            }, selection);

            // Assert
            // 不会影响字符顺序
            Assert.AreEqual(text, textEditorCore.GetText());

            var runPropertyList = textEditorCore.DocumentManager.GetRunPropertyRange(textEditorCore.GetAllDocumentSelection()).ToList();
            Assert.AreNotEqual(fontSize, runPropertyList[0].FontSize);
            Assert.AreNotEqual(fontSize, runPropertyList[1].FontSize);
            Assert.AreNotEqual(fontSize, runPropertyList[2].FontSize);

            Assert.AreEqual(fontSize, runPropertyList[3].FontSize);
            Assert.AreEqual(fontSize, runPropertyList[4].FontSize);

            Assert.AreNotEqual(fontSize, runPropertyList[5].FontSize);
        });

        "调用 DocumentManager.SetRunProperty 设置文本字符属性，传入修改范围没有长度，将啥都不会干".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();
            // 随便写一些字符
            textEditorCore.AppendText(TestHelper.PlainNumberText);

            var fontSize = 100;
            var fontName = "Test";

            // Action
            Selection? selection = new Selection(new CaretOffset(0), 0);
            textEditorCore.DocumentManager.SetRunProperty((LayoutOnlyRunProperty runProperty) =>
            {
                return runProperty with
                {
                    FontSize = fontSize,
                    FontName = new FontName(fontName)
                };
            }, selection);

            // Assert

            // 啥都不会干
            // 不会修改字符属性，不会修改当前光标字符属性
            Assert.AreNotEqual(fontSize, textEditorCore.DocumentManager.CurrentCaretRunProperty.FontSize);
            Assert.AreNotEqual(fontName, textEditorCore.DocumentManager.CurrentCaretRunProperty.FontName.UserFontName);

            // 不会影响到其他的字符
            var differentRunPropertyRange = textEditorCore.DocumentManager.GetDifferentRunPropertyRange(textEditorCore.GetAllDocumentSelection()).ToList();
            foreach (var runProperty in differentRunPropertyRange)
            {
                Assert.AreNotEqual(fontSize, runProperty.FontSize);
                Assert.AreNotEqual(fontName, runProperty.FontName.UserFontName);
            }
        });

        "调用 DocumentManager.SetRunProperty 设置文本字符属性，传入修改范围是 null 将只修改当前光标的字符属性".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();
            // 随便写一些字符
            textEditorCore.AppendText(TestHelper.PlainNumberText);

            var fontSize = 100;
            var fontName = "Test";

            // Action
            Selection? selection = null;
            textEditorCore.DocumentManager.SetRunProperty((LayoutOnlyRunProperty runProperty) =>
            {
                return runProperty with
                {
                    FontSize = fontSize,
                    FontName = new FontName(fontName)
                };
            }, selection);

            // Assert
            // 只修改当前光标的字符属性
            Assert.AreEqual(fontSize, textEditorCore.DocumentManager.CurrentCaretRunProperty.FontSize);
            Assert.AreEqual(fontName, textEditorCore.DocumentManager.CurrentCaretRunProperty.FontName.UserFontName);

            // 不会影响到其他的字符
            var differentRunPropertyRange = textEditorCore.DocumentManager.GetDifferentRunPropertyRange(textEditorCore.GetAllDocumentSelection()).ToList();
            foreach (var runProperty in differentRunPropertyRange)
            {
                Assert.AreNotEqual(fontSize, runProperty.FontSize);
                Assert.AreNotEqual(fontName, runProperty.FontName.UserFontName);
            }
        });

        "调用 DocumentManager.SetRunProperty 跨段设置文本字符属性，可以成功设置字符属性".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();
            // 追加一些文本，包含三段，用来测试跨三段设置字符属性
            var text = "abc\r\nefg\r\nhij";
            textEditorCore.AppendText(text);

            var fontSize = 100;
            var fontName = "Test";

            // Action
            // 从 c 之前到 h 之后的范围进行设置
            var selection = new Selection(new CaretOffset(2), 1/*c*/+ ParagraphData.DelimiterLength + 3/*efg*/+ ParagraphData.DelimiterLength + 1/*h*/);// 选择范围是 c\r\nefg\r\nh
            textEditorCore.DocumentManager.SetRunProperty((LayoutOnlyRunProperty runProperty) =>
            {
                return runProperty with
                {
                    FontSize = fontSize,
                    FontName = new FontName(fontName)
                };
            }, selection);

            // Assert
            // 不会影响到其他的字符
            var differentRunPropertyRange = textEditorCore.DocumentManager.GetDifferentRunPropertyRange(new Selection(new CaretOffset(0), 2)).ToList();
            foreach (var runProperty in differentRunPropertyRange)
            {
                Assert.AreNotEqual(fontSize, runProperty.FontSize);
                Assert.AreNotEqual(fontName, runProperty.FontName.UserFontName);
            }

            // 设置的字符修改了属性
            foreach (var runProperty in textEditorCore.DocumentManager.GetRunPropertyRange(selection))
            {
                Assert.AreEqual(fontSize, runProperty.FontSize);
                Assert.AreEqual(fontName, runProperty.FontName.UserFontName);
            }

            Assert.AreEqual(text.Replace("\r\n", "\n"), textEditorCore.GetText(), "只修改文本属性，没有修改到文本字符");
        });
    }

    [ContractTestCase]
    public void GetCharDataRange()
    {
        "调用 DocumentManager.GetCharDataRange 跨了三段选择，可以获取到三段的内容".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();
            // 追加一些文本，包含三段，用来测试跨三段选择
            textEditorCore.AppendText("abc\r\nefg\r\nhij");

            // Action
            // 从 c 之前到 h 之后的范围进行选择
            var selection = new Selection(new CaretOffset(2), 1/*c*/+ ParagraphData.DelimiterLength + 3/*efg*/+ ParagraphData.DelimiterLength + 1/*h*/);// 选择范围是 c\r\nefg\r\nh
            var charDataRange = textEditorCore.DocumentManager.GetCharDataRange(selection).ToList();

            // Assert
            Assert.AreEqual("c\nefg\nh", charDataRange.ConvertToString());
        });

        "调用 DocumentManager.GetCharDataRange 跨一段选择，从段末开始选择，可以获取到包含换行字符的列表".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();
            // 追加一些文本，包含两段，用来测试跨一段选择
            textEditorCore.AppendText("abc\r\nef");

            // Action
            var selection = new Selection(new CaretOffset(3), ParagraphData.DelimiterLength + 1/*e*/);// 选择范围是 \r\ne
            var charDataRange = textEditorCore.DocumentManager.GetCharDataRange(selection).ToList();

            // Assert
            Assert.AreEqual("\ne", charDataRange.ConvertToString());
        });

        "调用 DocumentManager.GetCharDataRange 跨一段选择，可以获取到跨段的列表".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();
            // 追加一些文本，包含两段，用来测试跨一段选择
            textEditorCore.AppendText("abc\r\nef");

            // Action
            var selection = new Selection(new CaretOffset(1), 2/*bc*/ + ParagraphData.DelimiterLength + 1/*e*/);// 选择范围是 bc\r\ne
            var charDataRange = textEditorCore.DocumentManager.GetCharDataRange(selection).ToList();

            // Assert
            Assert.AreEqual("bc\ne", charDataRange.ConvertToString());
        });

        "对包含 abc 三个字符的文本框，调用 DocumentManager.GetCharDataRange 传入文档全选，可以选择出 abc 三个字符".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();
            // 追加一些文本
            textEditorCore.AppendText("abc");

            // Action
            // 调用 DocumentManager.GetCharDataRange 传入文档全选
            var selection = textEditorCore.GetAllDocumentSelection();
            var charDataRange = textEditorCore.DocumentManager.GetCharDataRange(selection).ToList();

            // Assert
            Assert.AreEqual(selection.Length, charDataRange.Count);
            Assert.AreEqual("a", charDataRange[0].CharObject.ToText());
            Assert.AreEqual("b", charDataRange[1].CharObject.ToText());
            Assert.AreEqual("c", charDataRange[2].CharObject.ToText());
        });

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
            var textEditorCore = TestHelper.GetTextEditorCore(testPlatformProvider);

            // 对当前的光标设置文本字符属性
            var fontSize = 0d;
            // 对当前的光标设置文本字符属性
            textEditorCore.DocumentManager.SetCurrentCaretRunProperty<LayoutOnlyRunProperty>(runProperty =>
            {
                fontSize = runProperty.FontSize + 10;
                return runProperty with
                {
                    FontSize = fontSize
                };
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
                return runProperty with
                {
                    FontSize = fontSize
                };
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
                return runProperty with
                {
                    FontSize = fontSize
                };
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
            textEditorCore.AppendText("1\r\n2");

            // Assert
            var count = "1".Length + "\n".Length + "2".Length;
            Assert.AreEqual(count, textEditorCore.DocumentManager.CharCount);
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
