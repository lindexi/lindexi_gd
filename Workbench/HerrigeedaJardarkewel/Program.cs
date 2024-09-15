// See https://aka.ms/new-console-template for more information
var fooStream = new FooStream();
var streamReader = new StreamReader(fooStream);

while (!streamReader.EndOfStream)
{
    var line = await streamReader.ReadLineAsync();
    if (line is null)
    {
        break;
    }
}

Console.WriteLine("Hello, World!");

class FooStream : Stream
{
    public FooStream()
    {
        _buffer = "123\r\n"u8.ToArray();
    }

    private readonly byte[] _buffer;

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        // 模拟卡顿
        Thread.Sleep(10000);

        if (count >= _buffer.Length)
        {
            count = _buffer.Length;

            Array.Copy(_buffer, 0, buffer, offset, count);
        }

        return count;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return offset;
    }

    public override void SetLength(long value)
    {
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => long.MaxValue;
    public override long Position { get; set; }
}