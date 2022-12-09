using BenchmarkDotNet.Running;

using LightTextEditorPlus.Core.TestsFramework;

namespace LightTextEditorPlus.Core.BenchmarkTests;

internal class Program
{
    static void Main(string[] args)
    {
        var textEditor = TestHelper.GetTextEditorCore();

        for (int i = 0; i < 100000; i++)
        {
            textEditor.AppendText("123");
        }

        Console.Read();
        //BenchmarkRunner.Run<LightTextEditorPlusTest>();
    }
}