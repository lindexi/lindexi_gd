using System.Diagnostics;

namespace WinRemoteShell.Server;

internal static class WindowsServiceInstaller
{
    internal const string ServiceName = "WinRemoteShell";

    public static async Task InstallAsync(int port, CancellationToken cancellationToken = default)
    {
        EnsureWindows();
        var executablePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new InvalidOperationException("The current executable path could not be determined.");
        }

        var binaryPath = $"\"{executablePath}\" server --port {port}";
        await RunServiceControlAsync(
            ["create", ServiceName, "binPath=", binaryPath, "start=", "auto", "DisplayName=", ServiceName],
            cancellationToken);
        await RunServiceControlAsync(["start", ServiceName], cancellationToken);
    }

    public static async Task UninstallAsync(CancellationToken cancellationToken = default)
    {
        EnsureWindows();
        await RunServiceControlAsync(["stop", ServiceName], cancellationToken, 1062);
        await RunServiceControlAsync(["delete", ServiceName], cancellationToken);
    }

    private static async Task RunServiceControlAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken,
        params int[] allowedExitCodes)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "sc.exe",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        if (!process.Start())
        {
            throw new InvalidOperationException("Windows Service Control Manager could not be started.");
        }

        var standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardError = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        var output = await standardOutput;
        var error = await standardError;
        if (process.ExitCode != 0 && !allowedExitCodes.Contains(process.ExitCode))
        {
            var details = string.IsNullOrWhiteSpace(error) ? output : error;
            throw new InvalidOperationException(
                $"Windows service operation failed with exit code {process.ExitCode}: {details.Trim()}");
        }
    }

    private static void EnsureWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Windows service management is only supported on Windows.");
        }
    }
}
