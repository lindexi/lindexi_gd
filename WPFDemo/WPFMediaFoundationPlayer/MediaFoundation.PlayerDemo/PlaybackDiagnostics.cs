using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace MediaFoundation.PlayerDemo;

internal static class PlaybackDiagnostics
{
    private static readonly ConcurrentDictionary<string, byte> secrets = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, byte> registeredSources = new(StringComparer.Ordinal);
    private static readonly BlockingCollection<string> buffer = new(4096);
    private static readonly SessionListener listener = new();
    private static Task? writer;
    private static string? failure;
    internal static string LogPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WPFMediaFoundationPlayer", "Logs", $"session-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}-{Environment.ProcessId}-{Guid.NewGuid():N}.log");
    internal static string? Failure => Volatile.Read(ref failure);
    internal static event Action? StorageFailed;

    internal static void Start()
    {
        Trace.Listeners.Add(listener);
        writer = Task.Run(WriteLoop);
        Trace.WriteLine($"Startup process={Environment.ProcessId} architecture={RuntimeInformation.ProcessArchitecture} OS={RuntimeInformation.OSDescription} runtime={RuntimeInformation.FrameworkDescription} log={LogPath}", "Demo.Diagnostics");
    }

    internal static void RegisterSource(Uri? source)
    {
        if (source == null) return;
        string key = source.IsFile ? source.AbsoluteUri.ToUpperInvariant() : source.AbsoluteUri;
        if (!registeredSources.TryAdd(key, 0)) return;
        if (!source.IsFile)
        {
            secrets.TryAdd(source.OriginalString, 0);
            secrets.TryAdd(source.AbsoluteUri, 0);
            secrets.TryAdd(source.ToString(), 0);
        }
        Trace.WriteLine($"RegisterSource {MediaFoundation.Core.SourceDiagnostics.Describe(source)}{(source.IsFile ? " sourcePath=" + source.LocalPath : "")}", "Demo.Diagnostics");
    }

    internal static string Sanitize(string text)
    {
        foreach (string secret in secrets.Keys.OrderByDescending(s => s.Length)) text = text.Replace(secret, "[media]", StringComparison.OrdinalIgnoreCase);
        return Regex.Replace(text, @"\b[a-zA-Z][a-zA-Z0-9+.-]*://[^\s""<>]+", match =>
            match.Value.StartsWith("file:", StringComparison.OrdinalIgnoreCase) ? match.Value : "[uri]");
    }

    internal static string Write(Exception error, Uri? source)
    {
        RegisterSource(source);
        var chain = new List<string>();
        for (Exception? current = error; current != null; current = current.InnerException)
            chain.Add($"{current.GetType().FullName} HRESULT=0x{current.HResult:X8}");
        Trace.WriteLine($"Failure {string.Join(" -> ", chain)}{Environment.NewLine}{Sanitize(error.ToString())}", "Demo.Diagnostics");
        return LogPath;
    }

    internal static string Summary => $"Log: {LogPath}{Environment.NewLine}Storage: {Failure ?? "OK"}{Environment.NewLine}{RuntimeInformation.ProcessArchitecture} {RuntimeInformation.OSDescription} {RuntimeInformation.FrameworkDescription}";

    internal static void Stop()
    {
        Trace.WriteLine("Shutdown flush/dispose", "Demo.Diagnostics");
        Trace.Listeners.Remove(listener);
        lock (listener) buffer.CompleteAdding();
        if (writer != null && !writer.Wait(TimeSpan.FromSeconds(3))) ReportFailure("Log shutdown flush timed out: " + LogPath);
    }

    private static void ReportFailure(string message)
    {
        if (Interlocked.CompareExchange(ref failure, message, null) != null) return;
        Console.Error.WriteLine($"Diagnostics storage failure: {LogPath}{Environment.NewLine}{message}");
        StorageFailed?.Invoke();
    }

    private static void WriteLoop()
    {
        try
        {
            string directory = Path.GetDirectoryName(LogPath)!;
            Directory.CreateDirectory(directory);
            foreach (var old in new DirectoryInfo(directory).GetFiles("session-*.log").OrderByDescending(f => f.Name).Skip(19))
            {
                try { old.Delete(); }
                catch (IOException error) { Console.Error.WriteLine($"Log retention: {error}"); }
                catch (UnauthorizedAccessException error) { Console.Error.WriteLine($"Log retention: {error}"); }
            }
            using var output = new StreamWriter(new FileStream(LogPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read), System.Text.Encoding.UTF8);
            output.WriteLine($"{DateTime.UtcNow:O} LogPath={LogPath}");
            while (!buffer.IsCompleted)
            {
                var interval = Stopwatch.StartNew();
                while (!buffer.IsCompleted && interval.ElapsedMilliseconds < 500)
                    if (buffer.TryTake(out string? line, Math.Max(1, 500 - (int)interval.ElapsedMilliseconds))) output.WriteLine(line);
                output.Flush();
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            ReportFailure(error.ToString());
        }
    }

    private sealed class SessionListener : TraceListener
    {
        public override bool IsThreadSafe => true;
        public override void Write(string? message) => WriteLine(message);
        public override void WriteLine(string? message)
        {
            // The log location is intentionally retained; media locations are never recorded.
            string line = $"{DateTime.UtcNow:O} thread={Environment.CurrentManagedThreadId} {Sanitize(message ?? "")}";
            lock (this)
            {
                if (!buffer.IsAddingCompleted && !buffer.TryAdd(line)) ReportFailure("Diagnostic buffer full; records were lost.");
            }
        }
    }
}
