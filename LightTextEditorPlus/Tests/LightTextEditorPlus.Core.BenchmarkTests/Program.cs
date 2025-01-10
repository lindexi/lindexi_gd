using BenchmarkDotNet.Running;

namespace LightTextEditorPlus.Core.BenchmarkTests;

internal class Program
{
    static void Main(string[] args)
    {
        BenchmarkRunner.Run<LightTextEditorPlusTest>();
    }
}