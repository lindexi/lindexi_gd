using System.Text;

using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Utils;

using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests.Utils;

[TestClass]
public class TextIndexConverterTest
{
    [ContractTestCase]
    public void ConvertUtf16IndexToDocumentOffset()
    {
        "纯 ASCII 文本，UTF-16 索引与文档偏移 1:1 对应".Test(() =>
        {
            // Arrange
            var text = "Hello World";

            // Action & Assert
            Assert.AreEqual(new DocumentOffset(0), TextIndexConverter.ConvertUtf16IndexToDocumentOffset(text, 0));
            Assert.AreEqual(new DocumentOffset(5), TextIndexConverter.ConvertUtf16IndexToDocumentOffset(text, 5));
            Assert.AreEqual(new DocumentOffset(11), TextIndexConverter.ConvertUtf16IndexToDocumentOffset(text, 11));
        });

        "含单个 emoji（代理对字符）的文本，代理对算 1 个文档字符".Test(() =>
        {
            // Arrange
            var text = "a💡b";

            // "a" = 1 utfc16, "a💡" = 3 utf16, "a💡b" = 4 utf16
            // "a" = 1 doc, "a💡" = 2 doc (💡 is 1 doc char), "a💡b" = 3 doc
            // Action & Assert
            Assert.AreEqual(new DocumentOffset(0), TextIndexConverter.ConvertUtf16IndexToDocumentOffset(text, 0));
            Assert.AreEqual(new DocumentOffset(1), TextIndexConverter.ConvertUtf16IndexToDocumentOffset(text, 1));
            Assert.AreEqual(new DocumentOffset(2), TextIndexConverter.ConvertUtf16IndexToDocumentOffset(text, 3));
            Assert.AreEqual(new DocumentOffset(3), TextIndexConverter.ConvertUtf16IndexToDocumentOffset(text, 4));
        });

        "含多个 emoji 的文本".Test(() =>
        {
            // Arrange
            var text = "💡💡";

            // Action & Assert
            Assert.AreEqual(new DocumentOffset(0), TextIndexConverter.ConvertUtf16IndexToDocumentOffset(text, 0));
            Assert.AreEqual(new DocumentOffset(1), TextIndexConverter.ConvertUtf16IndexToDocumentOffset(text, text.EnumerateRunes().First().Utf16SequenceLength));
            Assert.AreEqual(new DocumentOffset(2), TextIndexConverter.ConvertUtf16IndexToDocumentOffset(text, text.Length));
        });

        "\\r\\n 折叠为 1 个文档字符".Test(() =>
        {
            // Arrange
            var text = "a\r\nb";

            // "a\r\nb" = 4 utf16 chars, but 3 document chars (a, \r\n-folded, b)
            // '\r' 在遍历时立即 +1，'\n' 被折叠跳过不增加
            // Action & Assert
            Assert.AreEqual(new DocumentOffset(0), TextIndexConverter.ConvertUtf16IndexToDocumentOffset(text, 0));
            Assert.AreEqual(new DocumentOffset(1), TextIndexConverter.ConvertUtf16IndexToDocumentOffset(text, 1));  // after 'a'
            Assert.AreEqual(new DocumentOffset(2), TextIndexConverter.ConvertUtf16IndexToDocumentOffset(text, 2));  // after 'a\r' - '\r' 已计入
            Assert.AreEqual(new DocumentOffset(2), TextIndexConverter.ConvertUtf16IndexToDocumentOffset(text, 3));  // after 'a\r\n' - '\n' 被折叠跳过
            Assert.AreEqual(new DocumentOffset(3), TextIndexConverter.ConvertUtf16IndexToDocumentOffset(text, 4));  // after 'a\r\nb'
        });

        "混合 emoji 与 \\r\\n".Test(() =>
        {
            // Arrange
            var text = "💡\r\n💡";
            var emojiLen = text.EnumerateRunes().First().Utf16SequenceLength;

            // \uD83D\uDCA1 (2 utf16) \r\n (2 utf16) \uD83D\uDCA1 (2 utf16)
            // document: 1 (💡) + 1 (\r\n folded) + 1 (💡) = 3 doc chars
            // Action & Assert
            Assert.AreEqual(new DocumentOffset(0), TextIndexConverter.ConvertUtf16IndexToDocumentOffset(text, 0));
            Assert.AreEqual(new DocumentOffset(1), TextIndexConverter.ConvertUtf16IndexToDocumentOffset(text, emojiLen));
            // At text index after \r\n: emojiLen + 2 = 4
            Assert.AreEqual(new DocumentOffset(2), TextIndexConverter.ConvertUtf16IndexToDocumentOffset(text, text.Length - emojiLen));
            // At end
            Assert.AreEqual(new DocumentOffset(3), TextIndexConverter.ConvertUtf16IndexToDocumentOffset(text, text.Length));
        });

        "\\uFFFC（内联元素占位符）按 1 个文档字符计算".Test(() =>
        {
            // Arrange
            var text = "a\uFFFcb";

            // Action & Assert
            Assert.AreEqual(new DocumentOffset(0), TextIndexConverter.ConvertUtf16IndexToDocumentOffset(text, 0));
            Assert.AreEqual(new DocumentOffset(1), TextIndexConverter.ConvertUtf16IndexToDocumentOffset(text, 1));
            Assert.AreEqual(new DocumentOffset(2), TextIndexConverter.ConvertUtf16IndexToDocumentOffset(text, 2));
            Assert.AreEqual(new DocumentOffset(3), TextIndexConverter.ConvertUtf16IndexToDocumentOffset(text, 3));
        });

        "边界条件：索引为 0 返回 0".Test(() =>
        {
            Assert.AreEqual(new DocumentOffset(0), TextIndexConverter.ConvertUtf16IndexToDocumentOffset("abc", 0));
        });

        "边界条件：索引等于 text.Length 返回对应的文档长度".Test(() =>
        {
            var text = "abc";
            Assert.AreEqual(new DocumentOffset(3), TextIndexConverter.ConvertUtf16IndexToDocumentOffset(text, text.Length));
        });

        "边界条件：空字符串".Test(() =>
        {
            Assert.AreEqual(new DocumentOffset(0), TextIndexConverter.ConvertUtf16IndexToDocumentOffset(string.Empty, 0));
            Assert.AreEqual(new DocumentOffset(0), TextIndexConverter.ConvertUtf16IndexToDocumentOffset(string.Empty, 5));
        });

        "边界条件：负索引".Test(() =>
        {
            Assert.AreEqual(new DocumentOffset(-1), TextIndexConverter.ConvertUtf16IndexToDocumentOffset("abc", -1));
        });

        "边界条件：索引超过 text.Length".Test(() =>
        {
            var text = "abc";
            Assert.AreEqual(new DocumentOffset(3), TextIndexConverter.ConvertUtf16IndexToDocumentOffset(text, 100));
        });
    }

    [ContractTestCase]
    public void ConvertDocumentOffsetToUtf16Index()
    {
        "纯 ASCII 文本，文档偏移与 UTF-16 索引 1:1 对应".Test(() =>
        {
            // Arrange
            var text = "Hello";

            // Action & Assert
            Assert.AreEqual(0, TextIndexConverter.ConvertDocumentOffsetToUtf16Index(text, new DocumentOffset(0)));
            Assert.AreEqual(3, TextIndexConverter.ConvertDocumentOffsetToUtf16Index(text, new DocumentOffset(3)));
            Assert.AreEqual(5, TextIndexConverter.ConvertDocumentOffsetToUtf16Index(text, new DocumentOffset(5)));
        });

        "含代理对字符的文本，文档偏移 → UTF-16 索引转换正确".Test(() =>
        {
            // Arrange
            var text = "a💡b";

            // document: a(0) 💡(1) b(2)
            // utf16:   a(0) 💡(1,2) b(3)
            // Action & Assert
            Assert.AreEqual(0, TextIndexConverter.ConvertDocumentOffsetToUtf16Index(text, new DocumentOffset(0)));
            Assert.AreEqual(1, TextIndexConverter.ConvertDocumentOffsetToUtf16Index(text, new DocumentOffset(1)));
            Assert.AreEqual(3, TextIndexConverter.ConvertDocumentOffsetToUtf16Index(text, new DocumentOffset(2)));
            Assert.AreEqual(4, TextIndexConverter.ConvertDocumentOffsetToUtf16Index(text, new DocumentOffset(3)));
        });

        "边界条件：文档偏移为 0".Test(() =>
        {
            Assert.AreEqual(0, TextIndexConverter.ConvertDocumentOffsetToUtf16Index("abc", new DocumentOffset(0)));
        });

        "边界条件：文档偏移等于文本长度（DefaultDocumentOffset）".Test(() =>
        {
            var text = "abc";
            // offset == text.Length maps to text.Length
            Assert.AreEqual(0, TextIndexConverter.ConvertDocumentOffsetToUtf16Index(text, new DocumentOffset(0)));
        });

        "边界条件：空字符串".Test(() =>
        {
            Assert.AreEqual(0, TextIndexConverter.ConvertDocumentOffsetToUtf16Index(string.Empty, new DocumentOffset(0)));
        });
    }

    [ContractTestCase]
    public void GetDocumentLength()
    {
        "纯 ASCII 文本的文档长度计算".Test(() =>
        {
            var text = "Hello";
            Assert.AreEqual(3, TextIndexConverter.GetDocumentLength(text, 1, 3));
        });

        "含 emoji 的文档长度计算".Test(() =>
        {
            var text = "a💡bc";
            // "a" = 1 utf16, "a💡" = 3 utf16, "a💡b" = 4 utf16, "a💡bc" = 5 utf16
            // "a" = 1 doc, "a💡" = 2 doc, "a💡b" = 3 doc, "a💡bc" = 4 doc
            Assert.AreEqual(2, TextIndexConverter.GetDocumentLength(text, 1, 3)); // "💡b" has 3 utf16, 2 doc
            Assert.AreEqual(1, TextIndexConverter.GetDocumentLength(text, 1, 2)); // "💡" has 2 utf16, 1 doc
        });
    }
}