using System.Diagnostics;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: ProcdumpExec.TestRunner <executable> [arguments]");
    return 1;
}

var startInfo = new ProcessStartInfo
{
    FileName = args[0],
    UseShellExecute = false
};

for (var i = 1; i < args.Length; i++)
{
    startInfo.ArgumentList.Add(args[i]);
}

using var process = Process.Start(startInfo)!;
process.WaitForExit();
Console.WriteLine($"PROCDUMP_EXEC_EXIT_CODE={process.ExitCode}");
return 0;
