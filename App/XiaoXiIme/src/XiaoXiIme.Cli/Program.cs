using System.Reflection;
using System.Text;

var command = args.Length > 0 ? args[0] : "help";

switch (command.ToLowerInvariant())
{
    case "install-checklist":
        PrintInstallChecklist(args.Skip(1).ToArray());
        return 0;
    case "uninstall-checklist":
        PrintUninstallChecklist();
        return 0;
    case "publish-checklist":
        PrintPublishChecklist();
        return 0;
    case "export-checklist":
        PrintExportChecklist(args.Skip(1).ToArray());
        return 0;
    case "help":
    case "--help":
    case "-h":
        PrintHelp();
        return 0;
    default:
        Console.Error.WriteLine($"Unknown command: {command}");
        PrintHelp();
        return 1;
}

static void PrintHelp()
{
    Console.WriteLine("XiaoXiIme.Cli");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  publish-checklist                 Print Native AOT publish verification steps.");
    Console.WriteLine("  export-checklist [ime-file]        Print export verification commands for an IME binary.");
    Console.WriteLine("  install-checklist [ime-file]       Print manual Windows IME installation checklist.");
    Console.WriteLine("  uninstall-checklist                Print manual Windows IME uninstall and rollback checklist.");
}

static void PrintPublishChecklist()
{
    Console.WriteLine("Native AOT publish checklist");
    Console.WriteLine("1. Run: dotnet publish src\\XiaoXiIme.ImeModule\\XiaoXiIme.ImeModule.csproj -c Release -r win-x64 --self-contained true -p:PublishAot=true");
    Console.WriteLine("2. Verify publish output exists under src\\XiaoXiIme.ImeModule\\bin\\Release\\net10.0\\win-x64\\publish\\.");
    Console.WriteLine("3. Verify the native shared library exports traditional IME entry points: ImeInquire, ImeProcessKey, ImeToAsciiEx, ImeSelect, NotifyIME.");
    Console.WriteLine("4. Copy or rename the published native library to the planned .ime file name only after export verification.");
    Console.WriteLine("5. Record all Native AOT warnings; known dotnetCampus.Ipc package warnings must not be confused with project-side reflection paths.");
}

static void PrintExportChecklist(string[] args)
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

    Console.Write(commands.ToString());
}

static void PrintInstallChecklist(string[] args)
{
    var imeFile = args.Length > 0 ? args[0] : "<published XiaoXiIme .ime path>";
    Console.WriteLine("Manual Windows IME install checklist");
    Console.WriteLine("Administrative privileges are required. This command does not modify the system.");
    Console.WriteLine($"1. Confirm the IME file exists: {imeFile}");
    Console.WriteLine("2. Copy the IME file to a stable installation directory, typically %SystemRoot%\\System32, using an elevated shell.");
    Console.WriteLine("3. Register the IME by calling ImmInstallIME with the installed .ime path and display name 'XiaoXi IME'.");
    Console.WriteLine("4. Record the returned HKL / layout id.");
    Console.WriteLine("5. Verify HKLM\\SYSTEM\\CurrentControlSet\\Control\\Keyboard Layouts\\<layout id> contains the IME metadata.");
    Console.WriteLine("6. If required, add current-user and .DEFAULT Keyboard Layout\\Preload entries that reference the layout id.");
    Console.WriteLine("7. Sign out/sign in or restart input services if the layout does not appear immediately.");
    Console.WriteLine("8. Switch to XiaoXi IME from Windows input method UI and run the manual smoke test.");
}

static void PrintUninstallChecklist()
{
    Console.WriteLine("Manual Windows IME uninstall and rollback checklist");
    Console.WriteLine("Administrative privileges are required. This command does not modify the system.");
    Console.WriteLine("1. Switch away from XiaoXi IME in all user sessions.");
    Console.WriteLine("2. Unload the recorded HKL with UnloadKeyboardLayout where possible.");
    Console.WriteLine("3. Remove current-user Keyboard Layout\\Preload entries that reference the XiaoXi layout id.");
    Console.WriteLine("4. Remove HKEY_USERS\\.DEFAULT\\Keyboard Layout\\Preload entries that reference the XiaoXi layout id if they were added.");
    Console.WriteLine("5. Remove Control Panel\\International\\User Profile entries that reference the layout id if present.");
    Console.WriteLine("6. Remove HKLM\\SYSTEM\\CurrentControlSet\\Control\\Keyboard Layouts\\<layout id> after backing it up.");
    Console.WriteLine("7. Delete the installed .ime file only after the layout is unloaded and no process locks it.");
    Console.WriteLine("8. Restart affected applications or Windows if the layout remains visible.");
    Console.WriteLine($"9. Keep this CLI version for audit: {Assembly.GetExecutingAssembly().GetName().Version}");
}
