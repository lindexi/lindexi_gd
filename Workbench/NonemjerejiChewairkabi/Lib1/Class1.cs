using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Lib1;

public static class Class1
{
    [UnmanagedCallersOnly(EntryPoint = "Run", CallConvs = [typeof(CallConvCdecl)])]
    public static int Run()
    {
        Console.WriteLine($"Run");
        return 2;
    }
}
