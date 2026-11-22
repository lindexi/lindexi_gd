namespace XiaoXiIme.SandboxRunner;

internal static class Program
{
    private const string ProjectTermSmokeCommand = "project-term-smoke";
    private const string NativeImeVirtualKeyProbeCommand = "native-ime-vkey-probe";

    private static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
        {
            WriteUsage();
            return 2;
        }

        return args[0] switch
        {
            "natural-daz-validation" when args.Length == 3 => await DebugFuzzyValidation.RunAsync
                (args[1], args[2], experience: true, natural: true),
            "experience-lock-validation" when args.Length == 3 => await ExperienceLockValidation.RunAsync
                (args[1], args[2]),
            "experience-validation" when args.Length == 3 => await DebugFuzzyValidation.RunAsync
                (args[1], args[2], experience: true),
            "debug-click-diagnostic" when args.Length == 3 => await DebugFuzzyValidation.RunAsync
                (args[1], args[2], minimal: true),
            "debug-fuzzy-validation" when args.Length == 3 => await DebugFuzzyValidation.RunAsync
                (args[1], args[2]),
            "debug-six-scheme-validation" when args.Length == 3 => await DebugFuzzyValidation.RunAsync
                (args[1], args[2], sixSchemes: true),
            "product-ui-validation" when args.Length == 4 => await ProductUiValidation.RunAsync
                (args[1], args[2], args[3]),
            "product-ui-batch" when args.Length == 3 => await RunProductBatchAsync
                (ResolveSandboxPath(args[1]), ResolveSandboxPath(args[2])),
            "product-ui-compact" when args.Length == 1 => await ProductUiValidation.RunAsync
            (
                Path.Combine(AppContext.BaseDirectory, "XiaoXiIme.exe"), AppContext.BaseDirectory,
                Path.Combine(AppContext.BaseDirectory, "evidence")
            ),
            "g02-recover-evidence" when args.Length == 1 => G02EvidenceRecovery.Run(),
            "g02-native-validation" when args.Length == 3 => await G02NativeValidation.RunAsync
                (ResolveSandboxPath(args[1]), ResolveSandboxPath(args[2])),
            "check-vm-install" when args.Length == 2 => await VmDeployment.CheckAsync
                (ResolveSandboxPath(args[1])),
            "deploy-vm" when args.Length == 3 => VmDeployment.Run
                (ResolveSandboxPath(args[1]), ResolveSandboxPath(args[2])),
            "install-notepad-smoke" when args.Length == 3 => await Imm32Validation.RunInstallSmokeAsync
                (ResolveSandboxPath(args[1]), ResolveSandboxPath(args[2]), notepad: true),
            "install-keyboard-smoke" when args.Length == 3 => await Imm32Validation.RunInstallSmokeAsync
                (ResolveSandboxPath(args[1]), ResolveSandboxPath(args[2])),
            "imm32-validation" when args.Length == 3 => await Imm32Validation.RunAsync
                (ResolveSandboxPath(args[1]), ResolveSandboxPath(args[2])),
            ProjectTermSmokeCommand when args.Length == 2 => ProjectTermSmoke.Run(args[1]),
            NativeImeVirtualKeyProbeCommand when args.Length == 2 => NativeImeVirtualKeyProbe.Run(args[1]),
            _ => WriteInvalidArguments(args[0]),
        };
    }

    private static string ResolveSandboxPath(string path) => Path.GetFullPath(path, AppContext.BaseDirectory);

    private static async Task<int> RunProductBatchAsync(string distribution, string output)
    {
        var imm = await Imm32Validation.RunAsync(distribution, Path.Combine(output, "imm32"));
        if (imm != 0)
        {
            return imm;
        }

        return await ProductUiValidation.RunAsync
        (
            Path.Combine(distribution, "XiaoXiIme.exe"), distribution, Path.Combine(output, "product-ui")
        );
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
        Console.Error.WriteLine("  XiaoXiIme.SandboxRunner imm32-validation <distribution> <output-directory>");
        Console.Error.WriteLine("  XiaoXiIme.SandboxRunner g02-native-validation <distribution> <output-directory>");
        Console.Error.WriteLine("  XiaoXiIme.SandboxRunner product-ui-batch <distribution> <output-directory>");
        Console.Error.WriteLine("  XiaoXiIme.SandboxRunner project-term-smoke <output-directory>");
        Console.Error.WriteLine("  XiaoXiIme.SandboxRunner native-ime-vkey-probe <ime-path>");
    }
}