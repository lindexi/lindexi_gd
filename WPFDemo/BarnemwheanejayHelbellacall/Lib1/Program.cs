using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Lib1;

public static class Program
{
    [UnmanagedCallersOnly(EntryPoint = "Start", CallConvs = [typeof(CallConvCdecl)])]
    public static int Start()
    {
        Console.WriteLine($"Start run");

        Task.Run(StartInner);

        return 2;
    }

    [UnmanagedCallersOnly(EntryPoint = "SetGreetText", CallConvs = [typeof(CallConvCdecl)])]
    public static void SetGreetText(IntPtr greetText, int charCount)
    {
        _greetText = Marshal.PtrToStringUni(greetText, charCount);
    }

    private static string _greetText = "Hello from Lib1!";

    private static void StartInner()
    {
        var builder = WebApplication.CreateSlimBuilder([]);

        var app = builder.Build();
        app.MapGet("/", () => _greetText);

        app.Run();
    }
}
