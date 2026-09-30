using System.Diagnostics;
using XiaoXiIme.ImeIpc;

namespace XiaoXiIme.Cli;

internal sealed record ImeHostProcessStartResult(
    bool Succeeded,
    string Message,
    string ExecutablePath,
    int? ProcessId = null,
    ImeHostStatus? HostStatus = null);

internal sealed class ImeHostProcessManager : IDisposable
{
    private Process? _process;

    public static ImeHostProcessStartResult StartDetached(string executablePath, string? serverName = null)
    {
        using var manager = new ImeHostProcessManager();
        var result = manager.Start(executablePath, keepRunning: true, serverName: serverName);
        return result;
    }

    public static void StopExisting(string? serverName = null)
    {
        _ = serverName;
        foreach (var process in Process.GetProcessesByName("XiaoXiIme.ImeHost"))
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(5_000);
                }
            }
            catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
            {
            }
            finally
            {
                process.Dispose();
            }
        }
    }

    public ImeHostProcessStartResult Start(string executablePath, bool keepRunning = false, string? serverName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);

        var fullPath = Path.GetFullPath(executablePath);
        if (!File.Exists(fullPath))
        {
            return new ImeHostProcessStartResult(false, $"IME host executable was not found: {fullPath}", fullPath);
        }

        Stop();

        var ipcOptions = string.IsNullOrWhiteSpace(serverName)
            ? XiaoXiImeIpcOptions.Default
            : new XiaoXiImeIpcOptions(serverName);
        var startInfo = new ProcessStartInfo(fullPath)
        {
            WorkingDirectory = Path.GetDirectoryName(fullPath)!,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add(ipcOptions.ServerName);

        try
        {
            _process = Process.Start(startInfo);
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return new ImeHostProcessStartResult(
                false,
                $"Unable to start the IME host: {exception.GetType().Name}: {exception.Message}",
                fullPath);
        }

        if (_process is null)
        {
            return new ImeHostProcessStartResult(false, $"Unable to start the IME host: {fullPath}", fullPath);
        }

        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
        if (_process.HasExited)
        {
            var exitCode = _process.ExitCode;
            _process.Dispose();
            _process = null;
            return new ImeHostProcessStartResult(
                false,
                $"IME host exited immediately with code {exitCode}: {fullPath}",
                fullPath);
        }

        var processId = _process.Id;
        var ready = WaitUntilReady(ipcOptions);
        if (!ready.Succeeded)
        {
            Stop();
            StopExisting(ipcOptions.ServerName);
            return new ImeHostProcessStartResult(
                false,
                ready.Message,
                fullPath,
                processId);
        }

        if (keepRunning)
        {
            _process.Dispose();
            _process = null;
        }

        return new ImeHostProcessStartResult(
            true,
            $"IME host started and is ready: {fullPath}",
            fullPath,
            processId,
            ready.HostStatus);
    }

    internal static ImeHostProcessStartResult WaitUntilReady(XiaoXiImeIpcOptions options, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        using var client = new XiaoXiImeIpcClient(options);
        try
        {
            var status = client.WaitUntilReadyAsync(timeout).ConfigureAwait(false).GetAwaiter().GetResult();
            var fallbackNote = status.IsUsingFallbackDictionary
                ? " Host is using the minimal fallback dictionary."
                : string.Empty;
            return new ImeHostProcessStartResult(
                true,
                $"IME host is ready.{fallbackNote}",
                string.Empty,
                HostStatus: status);
        }
        catch (Exception exception) when (exception is TimeoutException
            or IOException
            or InvalidOperationException
            or System.ComponentModel.Win32Exception)
        {
            return new ImeHostProcessStartResult(
                false,
                $"IME host did not become ready: {exception.Message}",
                string.Empty);
        }
    }

    public void Stop()
    {
        if (_process is null)
        {
            return;
        }

        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(5_000);
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
        }
        finally
        {
            _process.Dispose();
            _process = null;
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
