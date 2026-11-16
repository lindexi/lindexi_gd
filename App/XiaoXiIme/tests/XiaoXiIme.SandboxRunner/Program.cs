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
            "natural-daz-validation" when args.Length == 3 => DebugFuzzyValidation.RunAsync
                (args[1], args[2], experience: true, natural: true).GetAwaiter().GetResult(),
            "experience-lock-validation" when args.Length == 3 => ExperienceLockValidation.RunAsync
                (args[1], args[2]).GetAwaiter().GetResult(),
            "experience-validation" when args.Length == 3 => DebugFuzzyValidation.RunAsync
                (args[1], args[2], experience: true).GetAwaiter().GetResult(),
            "debug-click-diagnostic" when args.Length == 3 => DebugFuzzyValidation.RunAsync
                (args[1], args[2], minimal: true).GetAwaiter().GetResult(),
            "debug-fuzzy-validation" when args.Length == 3 => DebugFuzzyValidation.RunAsync
                (args[1], args[2]).GetAwaiter().GetResult(),
            "debug-six-scheme-validation" when args.Length == 3 => DebugFuzzyValidation.RunAsync
                (args[1], args[2], sixSchemes: true).GetAwaiter().GetResult(),
            "product-ui-validation" when args.Length == 4 => ProductUiValidation.RunAsync
                (args[1], args[2], args[3]).GetAwaiter().GetResult(),
            "product-ui-batch" when args.Length == 1 => RunProductBatchAsync().GetAwaiter().GetResult(),
            "product-ui-compact" when args.Length == 1 => ProductUiValidation.RunAsync
            (
                Path.Combine(AppContext.BaseDirectory, "XiaoXiIme.ImeUi.Avalonia.exe"), AppContext.BaseDirectory,
                Path.Combine(AppContext.BaseDirectory, "evidence")
            ).GetAwaiter().GetResult(),
            "g02-recover-evidence" when args.Length == 1 => G02EvidenceRecovery.Run(),
            "g02-native-validation" when args.Length == 1 => G02NativeValidation.RunAsync().GetAwaiter().GetResult(),
            "imm32-validation" when args.Length == 1 => Imm32Validation.RunAsync().GetAwaiter().GetResult(),
            ProjectTermSmokeCommand when args.Length == 2 => ProjectTermSmoke.Run(args[1]),
            NativeImeVirtualKeyProbeCommand when args.Length == 2 => NativeImeVirtualKeyProbe.Run(args[1]),
            _ => WriteInvalidArguments(args[0]),
        };
    }

    private static async Task<int> RunProductBatchAsync()
    {
        var imm = await Imm32Validation.RunAsync();
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        var payload = Path.Combine(root, "sandbox-output", "payload");
        var ui = await ProductUiValidation.RunAsync
        (
            Path.Combine(payload, "app", "ui", "XiaoXiIme.ImeUi.Avalonia.exe"),
            Path.Combine(payload, "app", "host"), Path.Combine(root, "final-evidence", "product-ui")
        );
        return imm == 0 && ui == 0 ? 0 : 1;
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
        Console.Error.WriteLine
            ("  XiaoXiIme.SandboxRunner product-ui-validation <ui-executable> <package-root> <output-directory>");
        Console.Error.WriteLine("  XiaoXiIme.SandboxRunner imm32-validation");
        Console.Error.WriteLine("  XiaoXiIme.SandboxRunner project-term-smoke <output-directory>");
        Console.Error.WriteLine("  XiaoXiIme.SandboxRunner native-ime-vkey-probe <ime-path>");
    }
}