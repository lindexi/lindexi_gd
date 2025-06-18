using System.Diagnostics;

using Microsoft.DotNet.Archive;

namespace DotNetCampus.Installer.Boost.Microsoft.DotNet.Archive.DirectoryArchives;

internal static class DirectoryArchive
{
    public static void Compress(DirectoryInfo inputDirectoryInfo, FileInfo outputFileInfo)
    {
        using var outputFileStream = new FileStream(outputFileInfo.FullName, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);

        FileInfo[] fileArray = inputDirectoryInfo.GetFiles("*", SearchOption.AllDirectories);

        var headStream = new MemoryStream();
        long totalFileLength = 0;

        for (int i = 0; i < 8; i++)
        {
            // 预留的内容，用来后续填充长度信息
            headStream.WriteByte(0xFF);
        }

        var streamWriter = new StreamWriter(headStream);
        foreach (var fileInfo in fileArray)
        {
            totalFileLength += fileInfo.Length;
            var relativePath = Path.GetRelativePath(inputDirectoryInfo.FullName, fileInfo.FullName);
            streamWriter.WriteLine($"{relativePath}");
            streamWriter.WriteLine($"Length={fileInfo.Length}");
            streamWriter.WriteLine();
        }
        streamWriter.Flush();
        var headLength = headStream.Position;
        Debug.Assert(headStream.Position == headStream.Length);
        totalFileLength += headLength;

        headStream.Position = 0;
        for (int i = 0; i < 8; i++)
        {
            headStream.WriteByte((byte) (headLength >> (8 * i)));
        }
        headStream.Position = 0;

        var currentIndex = 0;
        FileStream? currentFileStream = null;

        var directoryArchiveProxyInputStream = new DirectoryArchiveProxyInputStream(headStream, totalFileLength);
        directoryArchiveProxyInputStream.ReadNext += (_, args) =>
        {
            // ReSharper disable AccessToDisposedClosure
            if (currentFileStream is not null && !ReferenceEquals(currentFileStream, args.CurrentInputStream))
            {
                throw new Exception();
            }

            while (currentIndex < fileArray.Length)
            {
                FileInfo fileInfo = fileArray[currentIndex];
                if (fileInfo.Length == 0)
                {
                    // 跳过空文件
                    currentIndex++;
                    continue;
                }
                else
                {
                    break;
                }
            }

            if (currentIndex < fileArray.Length)
            {
                args.CurrentInputStream.Dispose();

                FileInfo fileInfo = fileArray[currentIndex];
                var fileStream = fileInfo.OpenRead();
                currentFileStream = fileStream;
                args.UpdateInputStream(fileStream);

                currentIndex++;
            }
        };

        var stopwatch = Stopwatch.StartNew();

        CompressionUtility.Compress(directoryArchiveProxyInputStream, outputFileStream, new Progress<ProgressReport>());

        stopwatch.Stop();
        Console.WriteLine($"TotalLength={totalFileLength};Elapsed={stopwatch.Elapsed.Minutes}m,{stopwatch.Elapsed.Seconds}s,{stopwatch.Elapsed.Milliseconds}ms");

        if (currentFileStream is not null)
        {
            currentFileStream.Dispose();
        }
    }

    public static async Task DecompressAsync()
    {
        await Task.CompletedTask;
    }
}

class DirectoryArchiveProxyInputStream : Stream
{
    public DirectoryArchiveProxyInputStream(Stream inputStream, long totalLength)
    {
        _inputStream = inputStream;
        Length = totalLength;
    }

    private Stream _inputStream;

    public event EventHandler<ReadNextEventArgs>? ReadNext;

    public readonly record struct ReadNextEventArgs(DirectoryArchiveProxyInputStream Stream)
    {
        public Stream CurrentInputStream => Stream._inputStream;
        public void UpdateInputStream(Stream stream) => Stream._inputStream = stream;
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var readCount = _inputStream.Read(buffer, offset, count);
        if (readCount == 0)
        {
            // 读取不到了，继续抛出事件
            ReadNext?.Invoke(this, new ReadNextEventArgs(this));

            readCount = _inputStream.Read(buffer, offset, count);
        }

        if (readCount == 0)
        {

        }

        Position += readCount;
        return readCount;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length { get; }
    public override long Position { get; set; }
}
