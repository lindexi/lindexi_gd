using System.Text;

using BenchmarkDotNet.Attributes;

using LightTextEditorPlus.Core.TestsFramework;

namespace LightTextEditorPlus.Core.BenchmarkTests;

[MemoryDiagnoser]
public class LightTextEditorPlusTest
{
    [Benchmark()]
    [Arguments(TestHelper.PlainNumberText)]
    [Arguments(TestHelper.PlainLongNumberText)]
    public void AppendText(string text)
    {
        var textEditor = TestHelper.GetTextEditorCore();
        textEditor.AppendText(text);
    }

    [Benchmark()]
    [Arguments(TestHelper.PlainNumberText, 10)]
    [Arguments(TestHelper.PlainNumberText, 100)]
    [Arguments(TestHelper.PlainNumberText, 1000)]
    public void AppendTextMultiTime(string text, int time)
    {
        var textEditor = TestHelper.GetTextEditorCore();
        for (int i = 0; i < time; i++)
        {
            textEditor.AppendText(text);
        }
    }

    /// <summary>
    /// 多段追加
    /// </summary>
    /// <param name="text"></param>
    /// <param name="paragraphCount"></param>
    [Benchmark()]
    [Arguments(TestHelper.PlainNumberText, 10)]
    [Arguments(TestHelper.PlainNumberText, 100)]
    [Arguments(TestHelper.PlainNumberText, 1000)]
    public void AppendTextMultiParagraph(string text, int paragraphCount)
    {
        text = text + "\r\n";
        var textEditor = TestHelper.GetTextEditorCore();
        for (int i = 0; i < paragraphCount; i++)
        {
            textEditor.AppendText(text);
        }
    }

    /// <summary>
    /// 多段多行追加
    /// </summary>
    /// <param name="text"></param>
    /// <param name="paragraphCount"></param>
    [Benchmark()]
    [Arguments(TestHelper.PlainLongNumberText, 10)]
    [Arguments(TestHelper.PlainLongNumberText, 100)]
    [Arguments(TestHelper.PlainLongNumberText, 1000)]
    public void AppendMultiParagraphAndLine(string text, int paragraphCount)
    {
        text = text + "\r\n";
        var textEditor = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider());
        textEditor.DocumentManager.DocumentWidth = 15 * 100 + 10;// 采用 FixCharSizePlatformProvider 即可让一个字符固定是 FontSize 也就是 15 的宽度，传入是 200 个字符，也一行布局 100 个字符，可以足够布局

        for (int i = 0; i < paragraphCount; i++)
        {
            textEditor.AppendText(text);
        }
    }

    [Benchmark()]
    [ArgumentsSource(nameof(GetLongTextArgument))]
    public void LongText(string text)
    {
        var textEditor = TestHelper.GetTextEditorCore();
        textEditor.AppendText(text);
    }

    public IEnumerable<string> GetLongTextArgument()
    {
        var stringBuilder = new StringBuilder();
        for (int i = 0; i < 100000; i++)
        {
            stringBuilder.Append('T');
        }

        yield return stringBuilder.ToString();
    }
}