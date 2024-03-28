using System.IO;
using System.Text;

namespace UsingHardLinkToZipNtfsDiskSize;

public class LogFileStringLoggerWriter : IStringLoggerWriter
{
    public LogFileStringLoggerWriter(DirectoryInfo logFolder)
    {
        _logFolder = logFolder;
        var logFile = Path.Join(logFolder.FullName, "Log.txt");
        var fileStream = new FileStream(logFile, FileMode.Append, FileAccess.Write, FileShare.Read);
        _fileStream = fileStream;
        var streamWriter = new StreamWriter(fileStream, Encoding.UTF8);
        _streamWriter = streamWriter;
    }

    private readonly DirectoryInfo _logFolder;
    private readonly FileStream _fileStream;
    private readonly StreamWriter _streamWriter;

    public async ValueTask WriteAsync(string message)
    {
        await _streamWriter.WriteLineAsync(message);
    }

    public async ValueTask DisposeAsync()
    {
        await _streamWriter.DisposeAsync();
        await _fileStream.DisposeAsync();
    }
}