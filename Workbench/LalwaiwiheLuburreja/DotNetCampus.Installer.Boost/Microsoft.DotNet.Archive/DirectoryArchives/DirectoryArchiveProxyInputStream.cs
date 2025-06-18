namespace DotNetCampus.Installer.Boost.Microsoft.DotNet.Archive.DirectoryArchives;

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