using System.Text;
using BenchmarkDotNet.Attributes;

using LightTextEditorPlus.Core.Tests;

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