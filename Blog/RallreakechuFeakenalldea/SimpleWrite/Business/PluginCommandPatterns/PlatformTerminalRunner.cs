using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleWrite.Business.PluginCommandPatterns;

abstract class PlatformTerminalRunner
{
    public static PlatformTerminalRunner? CreateCurrent()
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsPlatformTerminalRunner();
        }

        if (OperatingSystem.IsLinux())
        {
            return new LinuxPlatformTerminalRunner();
        }

        if (OperatingSystem.IsMacOS())
        {
            return new MacOSPlatformTerminalRunner();
        }

        return null;
    }

    public abstract Task ExecuteAsync(string commandText);

    protected static Process StartProcess(ProcessStartInfo startInfo, string errorMessage)
    {
        var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true,
        };

        if (!process.Start())
        {
            process.Dispose();
            throw new InvalidOperationException(errorMessage);
        }

        return process;
    }

    protected static bool TryFindExecutableInPath(string fileName, out string executablePath)
    {
        executablePath = string.Empty;

        string? path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        foreach (string folder in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                continue;
            }

            string candidate = Path.Combine(folder, fileName);
            if (File.Exists(candidate))
            {
                executablePath = candidate;
                return true;
            }
        }

        return false;
    }
}

sealed class WindowsPlatformTerminalRunner : PlatformTerminalRunner
{
    private readonly Lock _runningProcessLock = new();
    private Process? _runningProcess;

    public override async Task ExecuteAsync(string commandText)
    {
        Process process = GetOrCreateRunningProcess();

        try
        {
            process.StandardInput.AutoFlush = true;
            await process.StandardInput.WriteLineAsync(commandText);
            await process.StandardInput.FlushAsync();
        }
        catch (Exception exception) when (exception is InvalidOperationException or ObjectDisposedException)
        {
            process = RestartRunningProcess(process);
            process.StandardInput.AutoFlush = true;
            await process.StandardInput.WriteLineAsync(commandText);
            await process.StandardInput.FlushAsync();
        }
    }

    private Process GetOrCreateRunningProcess()
    {
        lock (_runningProcessLock)
        {
            if (_runningProcess is { HasExited: false })
            {
                return _runningProcess;
            }

            _runningProcess?.Dispose();
            _runningProcess = CreateProcess();
            return _runningProcess;
        }
    }

    private Process RestartRunningProcess(Process process)
    {
        lock (_runningProcessLock)
        {
            if (_runningProcess is { HasExited: false } currentProcess && !ReferenceEquals(currentProcess, process))
            {
                return currentProcess;
            }

            if (ReferenceEquals(_runningProcess, process))
            {
                _runningProcess = null;
            }

            process.Dispose();
            _runningProcess = CreateProcess();
            return _runningProcess;
        }
    }

    private Process CreateProcess()
    {
        var process = StartProcess(new ProcessStartInfo("cmd.exe")
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            CreateNoWindow = false,
            WorkingDirectory = Environment.CurrentDirectory,
        }, "无法启动 cmd 终端进程。");

        process.Exited += RunningProcessOnExited;
        return process;
    }

    private void RunningProcessOnExited(object? sender, EventArgs e)
    {
        if (sender is not Process process)
        {
            return;
        }

        lock (_runningProcessLock)
        {
            if (ReferenceEquals(_runningProcess, process))
            {
                _runningProcess = null;
            }
        }

        process.Dispose();
    }
}

sealed class LinuxPlatformTerminalRunner : PlatformTerminalRunner
{
    public override Task ExecuteAsync(string commandText)
    {
        var startInfo = CreateStartInfo(commandText);
        Process process = StartProcess(startInfo, "无法启动 Linux 终端进程。");
        process.Dispose();
        return Task.CompletedTask;
    }

    private static ProcessStartInfo CreateStartInfo(string commandText)
    {
        string command = $"{commandText}; exec /bin/bash -i";

        if (TryFindExecutableInPath("x-terminal-emulator", out string xTerminalEmulator))
        {
            var startInfo = new ProcessStartInfo(xTerminalEmulator)
            {
                UseShellExecute = false,
                WorkingDirectory = Environment.CurrentDirectory,
            };
            startInfo.ArgumentList.Add("-e");
            startInfo.ArgumentList.Add("/bin/bash");
            startInfo.ArgumentList.Add("-ic");
            startInfo.ArgumentList.Add(command);
            return startInfo;
        }

        if (TryFindExecutableInPath("gnome-terminal", out string gnomeTerminal))
        {
            var startInfo = new ProcessStartInfo(gnomeTerminal)
            {
                UseShellExecute = false,
                WorkingDirectory = Environment.CurrentDirectory,
            };
            startInfo.ArgumentList.Add("--");
            startInfo.ArgumentList.Add("/bin/bash");
            startInfo.ArgumentList.Add("-ic");
            startInfo.ArgumentList.Add(command);
            return startInfo;
        }

        if (TryFindExecutableInPath("kgx", out string kgx))
        {
            var startInfo = new ProcessStartInfo(kgx)
            {
                UseShellExecute = false,
                WorkingDirectory = Environment.CurrentDirectory,
            };
            startInfo.ArgumentList.Add("--");
            startInfo.ArgumentList.Add("/bin/bash");
            startInfo.ArgumentList.Add("-ic");
            startInfo.ArgumentList.Add(command);
            return startInfo;
        }

        if (TryFindExecutableInPath("konsole", out string konsole))
        {
            var startInfo = new ProcessStartInfo(konsole)
            {
                UseShellExecute = false,
                WorkingDirectory = Environment.CurrentDirectory,
            };
            startInfo.ArgumentList.Add("--noclose");
            startInfo.ArgumentList.Add("-e");
            startInfo.ArgumentList.Add("/bin/bash");
            startInfo.ArgumentList.Add("-ic");
            startInfo.ArgumentList.Add(command);
            return startInfo;
        }

        if (TryFindExecutableInPath("xfce4-terminal", out string xfceTerminal))
        {
            var startInfo = new ProcessStartInfo(xfceTerminal)
            {
                UseShellExecute = false,
                WorkingDirectory = Environment.CurrentDirectory,
            };
            startInfo.ArgumentList.Add("--hold");
            startInfo.ArgumentList.Add("--command");
            startInfo.ArgumentList.Add($"/bin/bash -ic \"{EscapeForDoubleQuotes(command)}\"");
            return startInfo;
        }

        if (TryFindExecutableInPath("xterm", out string xterm))
        {
            var startInfo = new ProcessStartInfo(xterm)
            {
                UseShellExecute = false,
                WorkingDirectory = Environment.CurrentDirectory,
            };
            startInfo.ArgumentList.Add("-hold");
            startInfo.ArgumentList.Add("-e");
            startInfo.ArgumentList.Add("/bin/bash");
            startInfo.ArgumentList.Add("-ic");
            startInfo.ArgumentList.Add(command);
            return startInfo;
        }

        throw new InvalidOperationException("未找到可用的 Linux 终端程序。请安装 gnome-terminal、konsole、xfce4-terminal、xterm 或 x-terminal-emulator。 ");
    }

    private static string EscapeForDoubleQuotes(string text)
    {
        return text.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
    }
}

sealed class MacOSPlatformTerminalRunner : PlatformTerminalRunner
{
    public override Task ExecuteAsync(string commandText)
    {
        var startInfo = new ProcessStartInfo("osascript")
        {
            UseShellExecute = false,
            WorkingDirectory = Environment.CurrentDirectory,
        };
        startInfo.ArgumentList.Add("-e");
        startInfo.ArgumentList.Add("tell application \"Terminal\" to activate");
        startInfo.ArgumentList.Add("-e");
        startInfo.ArgumentList.Add($"tell application \"Terminal\" to do script \"{EscapeForAppleScript(commandText)}\"");

        Process process = StartProcess(startInfo, "无法启动 macOS Terminal 进程。");
        process.Dispose();
        return Task.CompletedTask;
    }

    private static string EscapeForAppleScript(string text)
    {
        return text.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
    }
}
