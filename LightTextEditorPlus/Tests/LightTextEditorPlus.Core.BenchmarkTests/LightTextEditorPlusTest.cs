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

    [Benchmark()]
    [Arguments(TestHelper.PlainNumberText, 10)]
    [Arguments(TestHelper.PlainNumberText, 100)]
    [Arguments(TestHelper.PlainNumberText, 1000)]
    public void AppTextMultiLine(string text, int time)
    {
        text = text + "\r\n";
        var textEditor = TestHelper.GetTextEditorCore();
        for (int i = 0; i < time; i++)
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