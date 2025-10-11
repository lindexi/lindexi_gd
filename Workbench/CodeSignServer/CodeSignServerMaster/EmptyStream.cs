namespace CodeSignServerMaster;

/// <summary>
/// 表示一个空白的流，所有写入操作都会被忽略，读取操作总是返回 0 字节
/// </summary>
class EmptyStream : Stream
{
    public override void Flush()
    {
        
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return 0;
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
        // 空实现
    }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => 0;
    public override long Position { set; get; }
}