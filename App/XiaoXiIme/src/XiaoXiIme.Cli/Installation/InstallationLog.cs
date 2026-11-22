using System.Text;

namespace XiaoXiIme.Cli;

internal sealed class InstallationLog : IDisposable
{
    private readonly StreamWriter _writer;
    private readonly Lock _sync = new();
    private Exception? _writeError;

    internal InstallationLog(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        _writer = new StreamWriter(new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read), Encoding.UTF8);
    }

    internal static int Run(string operation, Func<InstallationLog, int> action)
    {
        var directory = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        if (!Path.IsPathFullyQualified(directory))
        {
            Console.Error.WriteLine("[InstallLog] Cannot locate ProgramData.");
            return 8;
        }

        var path = Path.Combine(directory, "XiaoXiIme", "Logs", "Install",
            $"{operation}-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}-{Guid.NewGuid():N}.log");
        try
        {
            using var log = new InstallationLog(path);
            log.Write($"[{operation}] Started; PID={Environment.ProcessId}");
            log.EnsureWritten();
            Console.Out.WriteLine($"[InstallLog] {path}");
            var exitCode = action(log);
            log.Write($"[{operation}] Finished; ExitCode={exitCode}");
            log.EnsureWritten();
            return exitCode;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"[InstallLog] {path}: {error.Message}");
            return 8;
        }
    }

    internal void Write(string message)
    {
        lock (_sync)
        {
            if (_writeError is not null)
            {
                return;
            }

            try
            {
                _writer.WriteLine($"{DateTimeOffset.UtcNow:O} {message}");
                _writer.Flush();
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                _writeError = error;
                Console.Error.WriteLine($"[InstallLog] {error.Message}");
            }
        }
    }

    internal void EnsureWritten()
    {
        lock (_sync)
        {
            if (_writeError is not null)
            {
                throw new IOException("Could not write the installation log.", _writeError);
            }
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            _writer.Dispose();
        }
    }
}
