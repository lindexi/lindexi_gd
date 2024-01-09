using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;

namespace JebeyeenalwhigemFenichecerde
{
    [DisassemblyDiagnoser(printInstructionAddresses: true, syntax: DisassemblySyntax.Masm, printSource: true)]
    [DryJob]
    public class IntroDisassembly
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<IntroDisassembly>();
        }

        [Benchmark]
        public int Foo()
        {
            var fxx = 5;
            var c = 0;
            for (int i = 0; i < fxx; i++)
            {
                c += fxx * i - c;
            }

            return c;
        }
    }
}