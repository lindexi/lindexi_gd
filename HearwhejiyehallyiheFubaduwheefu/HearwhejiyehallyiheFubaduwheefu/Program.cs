using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;

namespace HearwhejiyehallyiheFubaduwheefu
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<RegistryGetBaseKeyFromKeyNameBenchmark>();
        }
    }
}
