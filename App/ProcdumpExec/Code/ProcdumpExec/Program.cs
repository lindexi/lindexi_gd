using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

const uint CreateSuspended = 0x00000004;

return Run(args);

static int Run(string[] args)
{
    var separatorIndex = Array.IndexOf(args, "--");
    if (separatorIndex < 0 || separatorIndex == args.Length - 1)
    {
        Console.Error.WriteLine("Usage: ProcdumpExec [ProcDump arguments] -- <executable> [arguments]");
        return 1;
    }

    var targetCommandLine = BuildCommandLine(args.AsSpan(separatorIndex + 1));
    var startupInfo = new StartupInfo
    {
        Size = (uint)Marshal.SizeOf<StartupInfo>()
    };

    if (!CreateProcess
        (
            applicationName: null,
            commandLine: targetCommandLine,
            processAttributes: 0,
            threadAttributes: 0,
            inheritHandles: true,
            creationFlags: CreateSuspended,
            environment: 0,
            currentDirectory: null,
            startupInfo,
            out var processInformation
        ))
    {
        throw new Win32Exception(Marshal.GetLastWin32Error());
    }

    var resumed = false;
    try
    {
        var procDumpFileName = Environment.Is64BitProcess ? "procdump64.exe" : "procdump.exe";
        var procDumpPath = Path.Combine(AppContext.BaseDirectory, procDumpFileName);
        var startInfo = new ProcessStartInfo
        {
            FileName = procDumpPath,
            UseShellExecute = false
        };

        for (var i = 0; i < separatorIndex; i++)
        {
            startInfo.ArgumentList.Add(args[i]);
        }

        startInfo.ArgumentList.Add(processInformation.ProcessId.ToString(CultureInfo.InvariantCulture));

        using var procDump = Process.Start(startInfo)!;
        WaitForProcDumpToAttach(procDump, processInformation.ProcessHandle);

        if (ResumeThread(processInformation.ThreadHandle) == uint.MaxValue)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        resumed = true;
        procDump.WaitForExit();
        return procDump.ExitCode;
    }
    finally
    {
        if (!resumed)
        {
            TerminateProcess(processInformation.ProcessHandle, 1);
        }

        CloseHandle(processInformation.ThreadHandle);
        CloseHandle(processInformation.ProcessHandle);
    }
}

static void WaitForProcDumpToAttach(Process procDump, nint targetProcessHandle)
{
    while (!procDump.HasExited)
    {
        if (!CheckRemoteDebuggerPresent(targetProcessHandle, out var isDebuggerPresent))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        if (isDebuggerPresent)
        {
            return;
        }

        Thread.Sleep(10);
    }
}

static StringBuilder BuildCommandLine(ReadOnlySpan<string> arguments)
{
    var commandLine = new StringBuilder();

    foreach (var argument in arguments)
    {
        if (commandLine.Length > 0)
        {
            commandLine.Append(' ');
        }

        AppendQuotedArgument(commandLine, argument);
    }

    return commandLine;
}

static void AppendQuotedArgument(StringBuilder commandLine, string argument)
{
    if (argument.Length > 0 && argument.IndexOfAny([' ', '\t', '"']) < 0)
    {
        commandLine.Append(argument);
        return;
    }

    commandLine.Append('"');
    var backslashCount = 0;

    foreach (var character in argument)
    {
        if (character == '\\')
        {
            backslashCount++;
            continue;
        }

        if (character == '"')
        {
            commandLine.Append('\\', backslashCount * 2 + 1);
            commandLine.Append('"');
            backslashCount = 0;
            continue;
        }

        commandLine.Append('\\', backslashCount);
        commandLine.Append(character);
        backslashCount = 0;
    }

    commandLine.Append('\\', backslashCount * 2);
    commandLine.Append('"');
}

[DllImport
(
    "kernel32.dll", EntryPoint = "CreateProcessW", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Unicode
)]
[return: MarshalAs(UnmanagedType.Bool)]
static extern bool CreateProcess
(
    string? applicationName,
    StringBuilder commandLine,
    nint processAttributes,
    nint threadAttributes,
    [MarshalAs(UnmanagedType.Bool)] bool inheritHandles,
    uint creationFlags,
    nint environment,
    string? currentDirectory,
    in StartupInfo startupInfo,
    out ProcessInformation processInformation
);

[DllImport("kernel32.dll", SetLastError = true)]
[return: MarshalAs(UnmanagedType.Bool)]
static extern bool CheckRemoteDebuggerPresent
(
    nint processHandle,
    [MarshalAs(UnmanagedType.Bool)] out bool isDebuggerPresent
);

[DllImport("kernel32.dll", SetLastError = true)]
static extern uint ResumeThread(nint threadHandle);

[DllImport("kernel32.dll", SetLastError = true)]
[return: MarshalAs(UnmanagedType.Bool)]
static extern bool TerminateProcess(nint processHandle, uint exitCode);

[DllImport("kernel32.dll", SetLastError = true)]
[return: MarshalAs(UnmanagedType.Bool)]
static extern bool CloseHandle(nint handle);

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
struct StartupInfo
{
    public uint Size;
    public string? Reserved;
    public string? Desktop;
    public string? Title;
    public uint X;
    public uint Y;
    public uint XSize;
    public uint YSize;
    public uint XCountChars;
    public uint YCountChars;
    public uint FillAttribute;
    public uint Flags;
    public ushort ShowWindow;
    public ushort Reserved2Size;
    public nint Reserved2;
    public nint StandardInput;
    public nint StandardOutput;
    public nint StandardError;
}

[StructLayout(LayoutKind.Sequential)]
readonly struct ProcessInformation
{
    public readonly nint ProcessHandle;
    public readonly nint ThreadHandle;
    public readonly uint ProcessId;
    public readonly uint ThreadId;
}