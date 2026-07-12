using System.Reflection;
using System.Text;

namespace XiaoXiIme.Cli;

internal static class CliApplication
{
    internal static int Run(
        string[] args,
        TextWriter output,
        TextWriter error,
        Func<IImeInstaller> createInstaller)
    {
        var command = args.Length > 0 ? args[0] : "help";
        var commandArguments = args.Skip(1).ToArray();

        switch (command.ToLowerInvariant())
        {
            case "install":
                return Install(commandArguments, output, error, createInstaller);
            case "install-checklist":
                PrintInstallChecklist(commandArguments, output);
                return 0;
            case "uninstall-checklist":
                PrintUninstallChecklist(output);
                return 0;
            case "publish-checklist":
                PrintPublishChecklist(output);
                return 0;
            case "export-checklist":
                PrintExportChecklist(commandArguments, output);
                return 0;
            case "help":
            case "--help":
            case "-h":
                PrintHelp(output);
                return 0;
            default:
                error.WriteLine($"Unknown command: {command}");
                PrintHelp(error);
                return 1;
        }
    }

    private static int Install(
        string[] args,
        TextWriter output,
        TextWriter error,
        Func<IImeInstaller> createInstaller)
    {
        var imeFile = args.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(imeFile))
        {
            error.WriteLine("The install command requires the full path to an .ime file.");
            return 2;
        }

        var fullPath = Path.GetFullPath(imeFile);
        if (!File.Exists(fullPath))
        {
            error.WriteLine($"IME file not found: {fullPath}");
            return 4;
        }

        if (!string.Equals(Path.GetExtension(fullPath), ".ime", StringComparison.OrdinalIgnoreCase))
        {
            error.WriteLine($"Installation refused. The target file must use the .ime extension: {fullPath}");
            return 4;
        }

        var result = createInstaller().Install(fullPath, "XiaoXi IME");
        if (!result.Succeeded)
        {
            error.WriteLine(result.Message);
            return 5;
        }

        output.WriteLine(result.Message);
        output.WriteLine("Record the returned HKL/layout id, then sign out or restart Windows before the smoke test if the input method is not visible.");
        return 0;
    }

    private static void PrintHelp(TextWriter output)
    {
        output.WriteLine("XiaoXiIme.Cli");
        output.WriteLine();
        output.WriteLine("Commands:");
        output.WriteLine("  publish-checklist                 Print Native AOT publish verification steps.");
        output.WriteLine("  export-checklist [ime-file]        Print export verification commands for an IME binary.");
        output.WriteLine("  install <ime-file>                  Install the IME by calling the Windows ImmInstallIME API.");
        output.WriteLine("  install-checklist [ime-file]       Print manual Windows IME installation checklist.");
        output.WriteLine("  uninstall-checklist                Print manual Windows IME uninstall and rollback checklist.");
    }

    private static void PrintPublishChecklist(TextWriter output)
    {
        output.WriteLine("Native AOT publish checklist");
        output.WriteLine("1. Run: dotnet publish src\\XiaoXiIme.ImeModule\\XiaoXiIme.ImeModule.csproj -c Release -r win-x64 --self-contained true -p:PublishAot=true");
        output.WriteLine("2. Verify publish output exists under src\\XiaoXiIme.ImeModule\\bin\\Release\\net10.0\\win-x64\\publish\\.");
        output.WriteLine("3. Verify the native shared library exports traditional IME entry points: ImeInquire, ImeProcessKey, ImeToAsciiEx, ImeSelect, NotifyIME.");
        output.WriteLine("4. Copy or rename the published native library to the planned .ime file name only after export verification.");
        output.WriteLine("5. Record all Native AOT warnings; known dotnetCampus.Ipc package warnings must not be confused with project-side reflection paths.");
    }

    private static void PrintExportChecklist(string[] args, TextWriter output)
    {
        var imeFile = args.Length > 0 ? args[0] : "src\\XiaoXiIme.ImeModule\\bin\\Release\\net10.0\\win-x64\\publish\\XiaoXiIme.ImeModule.dll";
        var commands = new StringBuilder()
            .AppendLine("Export verification checklist")
            .AppendLine("This command does not inspect or modify the system. Run one of the following checks manually before registration.")
            .AppendLine($"Target binary: {imeFile}")
            .AppendLine("1. Visual Studio Developer Command Prompt:")
            .AppendLine($"   dumpbin /exports \"{imeFile}\"")
            .AppendLine("2. PowerShell with Visual Studio tools on PATH:")
            .AppendLine($"   dumpbin /exports \"{imeFile}\" | Select-String \"ImeInquire|ImeProcessKey|ImeToAsciiEx|ImeSelect|NotifyIME\"")
            .AppendLine("3. Required export names:")
            .AppendLine("   ImeInquire, ImeProcessKey, ImeToAsciiEx, ImeSelect, NotifyIME")
            .AppendLine("4. Do not call ImmInstallIME until all required exports are present.");

        output.Write(commands.ToString());
    }

    private static void PrintInstallChecklist(string[] args, TextWriter output)
    {
        var imeFile = args.Length > 0 ? args[0] : "<published XiaoXiIme .ime path>";
        output.WriteLine("Windows IME install checklist");
        output.WriteLine($"1. Confirm the IME file exists in a stable location: {imeFile}");
        output.WriteLine($"2. Run from an elevated process: install \"{imeFile}\"");
        output.WriteLine("3. Record the returned HKL / layout id.");
        output.WriteLine("4. Verify HKLM\\SYSTEM\\CurrentControlSet\\Control\\Keyboard Layouts\\<layout id> contains the IME metadata.");
        output.WriteLine("5. Sign out/sign in or restart Windows if the layout does not appear immediately.");
        output.WriteLine("6. Switch to XiaoXi IME and run the manual smoke test.");
    }

    private static void PrintUninstallChecklist(TextWriter output)
    {
        output.WriteLine("Manual Windows IME uninstall and rollback checklist");
        output.WriteLine("Administrative privileges are required. This command does not modify the system.");
        output.WriteLine("1. Switch away from XiaoXi IME in all user sessions.");
        output.WriteLine("2. Unload the recorded HKL with UnloadKeyboardLayout where possible.");
        output.WriteLine("3. Remove current-user Keyboard Layout\\Preload entries that reference the XiaoXi layout id.");
        output.WriteLine("4. Remove HKEY_USERS\\.DEFAULT\\Keyboard Layout\\Preload entries that reference the XiaoXi layout id if they were added.");
        output.WriteLine("5. Remove Control Panel\\International\\User Profile entries that reference the layout id if present.");
        output.WriteLine("6. Remove HKLM\\SYSTEM\\CurrentControlSet\\Control\\Keyboard Layouts\\<layout id> after backing it up.");
        output.WriteLine("7. Delete the installed .ime file only after the layout is unloaded and no process locks it.");
        output.WriteLine("8. Restart affected applications or Windows if the layout remains visible.");
        output.WriteLine($"9. Keep this CLI version for audit: {Assembly.GetExecutingAssembly().GetName().Version}");
    }
}
