using System.Diagnostics;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.TestsFramework;
using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests;

[TestClass]
public class DefaultRunParagraphSplitterTest
{
    [ContractTestCase]
    public void Split()
    {
        "传入文本中间包含两个换行符，可以输出为三段".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();
            var splitter = textEditorCore.PlatformProvider.GetRunParagraphSplitter();

            // Action
            var textRun = new TextRun("a\r\n\r\nb");
            // 拆分为三段，分别是
            // - a
            // - 空段
            // - b
            var result = splitter.Split(textRun).ToList();

            // Assert
            Assert.AreEqual(3, result.Count);
            List<IImmutableRun> testResult = Split(textRun).ToList();
            Assert.AreEqual(testResult.Count, result.Count);
        });

        "只传入一个换行符，只会返回一次空段".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();
            var splitter = textEditorCore.PlatformProvider.GetRunParagraphSplitter();

            // Action
            // 只传入一个换行符
            var textRun = new TextRun("\r\n");
            var result = splitter.Split(textRun).ToList();

            // Assert
            // 只会返回一次空段
            Assert.AreEqual(1, result.Count);
            List<IImmutableRun> testResult = Split(textRun).ToList();
            Assert.AreEqual(testResult.Count, result.Count);
        });

        "传入的文本的结尾包含连续两个换行符，可以多加两个空段".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();
            var splitter = textEditorCore.PlatformProvider.GetRunParagraphSplitter();

            // Action
            var textRun = new TextRun("123\r\n\r\n");
            // 拆分为三段，分别是
            // - 123
            // - 空段
            // - 空末段
            var result = splitter.Split(textRun).ToList();

            // Assert
            // 123+空段+空末段 = 3
            Assert.AreEqual(3, result.Count);
            List<IImmutableRun> testResult = Split(textRun).ToList();
            Assert.AreEqual(testResult.Count, result.Count);
        });

        "传入的文本的结尾包含换行符，则为两段".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();
            var splitter = textEditorCore.PlatformProvider.GetRunParagraphSplitter();

            // Action
            // 拆分为两段，分别是
            // - 123
            // - 空末段
            var textRun = new TextRun("123\r\n");
            var result = splitter.Split(textRun).ToList();

            // Assert
            // 123+空段 = 2
            Assert.AreEqual(2, result.Count);
            List<IImmutableRun> testResult = Split(textRun).ToList();
            Assert.AreEqual(testResult.Count, result.Count);
        });

        "传入包含多个连续换行符的文本，可以分出空段".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();
            var splitter = textEditorCore.PlatformProvider.GetRunParagraphSplitter();

            // Action
            var textRun = new TextRun("123\r\n\r\n\r\n123\r\n123");
            var result = splitter.Split(textRun).ToList();

            // Assert
            Assert.AreEqual(5, result.Count);
            List<IImmutableRun> testResult = Split(textRun).ToList();
            Assert.AreEqual(testResult.Count, result.Count);
        });

        "传入包含多个换行符的文本，可以根据换行符进行分段".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();
            var splitter = textEditorCore.PlatformProvider.GetRunParagraphSplitter();

            // Action
            var textRun = new TextRun("123\r\n123\r\n123");
            var result = splitter.Split(textRun).ToList();

            // Assert
            Assert.AreEqual(3, result.Count);
            List<IImmutableRun> testResult = Split(textRun).ToList();
            Assert.AreEqual(testResult.Count, result.Count);
        });

        "对于一段文本不包含任何换行符，可以分割之后，返回依然是一段".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();
            var splitter = textEditorCore.PlatformProvider.GetRunParagraphSplitter();

            // Action
            var result = splitter.Split(new TextRun("123")).ToList();

            // Assert
            Assert.AreEqual(1, result.Count);
        });
    }

    private static IEnumerable<IImmutableRun> Split(TextRun textRun)
    {
        var text = textRun.Text;
        foreach (SplitTextResult result in Split(text))
        {
            if (result.IsLineBreak)
            {
                yield return new LineBreakRun(textRun.RunProperty);
            }
            else
            {
                yield return new SpanTextRun(text, result.Start, result.Length, textRun.RunProperty);
                //yield return new TextRun(subText, textRun.RunProperty);
            }
        }
    }

    private readonly record struct SplitTextResult(int Start, int Length)
    {
        public bool IsLineBreak => Length == 0;
    }

    private static IEnumerable<SplitTextResult> Split(string text)
    {
        int position = 0;
        bool endWithBreakLine = false;
        for (int i = 0; i < text.Length; i++)
        {
            var currentChar = text[i];
            // 是否 \r 字符
            bool isCr = currentChar == '\r';
            // 是否 \n 字符
            bool isLf = !isCr && currentChar == '\n';
            if (isCr || isLf)
            {
                if (position == i)
                {
                    yield return new SplitTextResult(i, 0);
                }
                else
                {
                    var length = i - position;
                    yield return new SplitTextResult(position, length); //text.Substring(position, length);

                    endWithBreakLine = true;
                }

                // 如果是 \r 情况下，读取下一个字符，判断是否 \n 字符
                if (isCr && i != text.Length - 1)
                {
                    var nextChar = text[i + 1];
                    if (nextChar is '\n')
                    {
                        i++;
                    }
                }

                position = i + 1;
            }
            else
            {
                endWithBreakLine = false;
            }
        }

        if (position < text.Length)
        {
            Debug.Assert(endWithBreakLine is false);

            var length = text.Length - position;
            yield return new SplitTextResult(position, length);
        }

        if (endWithBreakLine)
        {
            yield return new SplitTextResult(text.Length - 1, 0);
        }
    }
}
