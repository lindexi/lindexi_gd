using System.Diagnostics;
using XiaoXiIme.ImeIpc;

namespace XiaoXiIme.Cli;

internal sealed class ImeHostProcessManager : IDisposable
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(30);
    private Process? _process;

    public static ImeHostProcessStartResult StartDetached(string executablePath, string? serverName = null, Action<string>? startupLog = null)
    {
        using var manager = new ImeHostProcessManager();
        var result = manager.Start(executablePath, keepRunning: true, serverName: serverName, startupLog: startupLog);
        return result;
    }

    public ImeHostProcessStartResult Start(string executablePath, bool keepRunning = false, string? serverName = null, Action<string>? startupLog = null)
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
        if (string.Equals(Path.GetFileName(fullPath), "XiaoXiIme.exe", StringComparison.OrdinalIgnoreCase))
        {
            startInfo.ArgumentList.Add("ui");
        }
        startInfo.ArgumentList.Add(ipcOptions.ServerName);
        startInfo.Environment["XIAOXIIME_STARTUP_TRACE"] = "1";

        try
        {
            _process = Process.Start(startInfo);
        }
        catch (Exception exception) when
            (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return new ImeHostProcessStartResult
            (
                false,
                $"Unable to start the IME host: {exception.GetType().Name}: {exception.Message}",
                fullPath
            );
        }

        if (_process is null)
        {
            return new ImeHostProcessStartResult(false, $"Unable to start the IME host: {fullPath}", fullPath);
        }

        var process = _process;
        var processId = process.Id;
        startupLog?.Invoke($"[Install][WaitForUi] PID={processId}; Server={ipcOptions.ServerName}; Timeout={StartupTimeout.TotalSeconds}s");
        var stdout = new StartupOutput(startupLog, "stdout");
        var stderr = new StartupOutput(startupLog, "stderr");
        process.OutputDataReceived += (_, line) => stdout.Append(line.Data);
        process.ErrorDataReceived += (_, line) => stderr.Append(line.Data);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        using var exited = new CancellationTokenSource();
        void OnExit(object? sender, EventArgs args) => exited.Cancel();
        process.Exited += OnExit;
        process.EnableRaisingEvents = true;
        var ready = WaitUntilReady(ipcOptions, StartupTimeout, exited.Token);
        process.Exited -= OnExit;
        if (!ready.Succeeded || process.HasExited)
        {
            int? childExitCode = process.HasExited ? process.ExitCode : null;
            Stop();
            if (childExitCode is { } exitCode)
            {
                ready = ready with { Message = $"IME host exited during startup with code {exitCode}: {fullPath}" };
            }

            stdout.StopCollecting();
            stderr.StopCollecting();
            return new ImeHostProcessStartResult(false, ready.Message, fullPath, processId,
                ChildExitCode: childExitCode, StandardOutput: stdout.Text, StandardError: stderr.Text);
        }

        stdout.StopCollecting();
        stderr.StopCollecting();

        if (keepRunning)
        {
            _process.Dispose();
            _process = null;
        }

        return new ImeHostProcessStartResult
        (
            true,
            $"IME host started and is ready: {fullPath}",
            fullPath,
            processId,
            ready.HostStatus,
            StandardOutput: stdout.Text,
            StandardError: stderr.Text
        );
    }

    internal static ImeHostProcessStartResult WaitUntilReady
        (XiaoXiImeIpcOptions options, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        using var client = new XiaoXiImeIpcClient(options);
        try
        {
            // IPC connection initialization must not capture a blocked caller synchronization context.
            var status = Task.Run(() => client.WaitUntilReadyAsync(timeout, cancellationToken)).GetAwaiter().GetResult();
            var fallbackNote = status.IsUsingFallbackDictionary
                ? " Host is using the minimal fallback dictionary."
                : string.Empty;
            return new ImeHostProcessStartResult
            (
                true,
                $"IME host is ready.{fallbackNote}",
                string.Empty,
                HostStatus: status
            );
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new ImeHostProcessStartResult(false, "IME host startup was canceled.", string.Empty);
        }
        catch (Exception exception) when (exception is TimeoutException
                                              or IOException
                                              or InvalidOperationException
                                              or System.ComponentModel.Win32Exception)
        {
            return new ImeHostProcessStartResult
            (
                false,
                $"IME host did not become ready: {exception.Message}",
                string.Empty
            );
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
            }
            _process.WaitForExit(5_000);
        }
        catch (Exception exception) when
            (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
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

    private sealed class StartupOutput(Action<string>? log, string channel)
    {
        private const int Limit = 16_384;
        private readonly System.Text.StringBuilder _text = new();
        private bool _collect = true;

        internal string Text
        {
            get { lock (_text) { return _text.ToString(); } }
        }

        internal void Append(string? line)
        {
            lock (_text)
            {
                if (_collect && line is not null && _text.Length < Limit)
                {
                    var captured = line[..Math.Min(line.Length, Limit - _text.Length)];
                    _text.AppendLine(captured);
                    log?.Invoke($"[ImeUI][{channel}] {captured}");
                }
            }
        }

        internal void StopCollecting()
        {
            lock (_text) { _collect = false; }
        }
    }
}