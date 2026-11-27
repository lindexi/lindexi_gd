using System.ClientModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace PptxGenerator.Chat.Services;

internal static class ChatDiagnostics
{
    private static TextWriterTraceListener? _fileListener;

    internal static string? StartFileLog(string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, $"Chat-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}-{Environment.ProcessId}.log");
            var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            _fileListener = new TextWriterTraceListener(new StreamWriter(stream) { AutoFlush = true });
            Info("Startup", "FileLogging=Enabled");
            return path;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Failure("InitializeLog", exception, "LogDirectory", directory);
            return null;
        }
    }

    internal static void StopFileLog()
    {
        var listener = Interlocked.Exchange(ref _fileListener, null);
        if (listener is null) return;
        lock (listener)
        {
            try { listener.Dispose(); }
            catch (IOException exception) { Failure("CloseLog", exception, "Target", "File"); }
        }
    }

    internal static void Info(string operation, string fields) => Write("Information", operation, fields);

    internal static void Failure(string operation, Exception exception, string field, string value)
    {
        // Remote exception messages can embed response bodies or credentials.
        string detail = exception switch
        {
            ClientResultException client => $" StatusCode={client.Status}",
            HttpRequestException http => $" StatusCode={http.StatusCode}",
            JsonException json => $" LineNumber={json.LineNumber} BytePosition={json.BytePositionInLine}",
            _ => string.Empty,
        };
        Write("Warning", operation, $"{field}={value} ExceptionType={exception.GetType().Name} HResult=0x{exception.HResult:X8} CauseType={exception.InnerException?.GetType().Name}{detail}");
    }

    private static void Write(string level, string operation, string fields)
    {
        string entry = $"{DateTimeOffset.Now:O} [{level}] [Chat][{operation}] {fields}";
        Trace.WriteLine(entry);
        var listener = Volatile.Read(ref _fileListener);
        if (listener is null) return;
        lock (listener)
        {
            if (!ReferenceEquals(listener, Volatile.Read(ref _fileListener))) return;
            try
            {
                listener.WriteLine(entry);
            }
            catch (IOException exception)
            {
                Trace.TraceWarning($"[Chat][WriteLog] HResult=0x{exception.HResult:X8}");
            }
        }
    }
}
