namespace XiaoXiIme.SandboxRunner;

internal static class Program
{
    private const string ProjectTermSmokeCommand = "project-term-smoke";
    private const string NativeImeVirtualKeyProbeCommand = "native-ime-vkey-probe";

    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            WriteUsage();
            return 2;
        }

        return args[0] switch
        {
            ProjectTermSmokeCommand when args.Length == 2 => ProjectTermSmoke.Run(args[1]),
            NativeImeVirtualKeyProbeCommand when args.Length == 2 => NativeImeVirtualKeyProbe.Run(args[1]),
            _ => WriteInvalidArguments(args[0]),
        };
    }

    private static int WriteInvalidArguments(string command)
    {
        Console.Error.WriteLine($"Unknown command or invalid arguments: {command}");
        WriteUsage();
        return 2;
    }

    private static void WriteUsage()
    {
        Console.Error.WriteLine("Usage:");
        Console.Error.WriteLine("  XiaoXiIme.SandboxRunner project-term-smoke <output-directory>");
        Console.Error.WriteLine("  XiaoXiIme.SandboxRunner native-ime-vkey-probe <ime-path>");
    }
}
