using System;
using System.IO;
using OpenMcdf;

namespace KaldaygeduWalaineejaw
{
    class DataStream : Stream
    {
        private readonly CFStream _stream;

        public DataStream(CFStream stream)
        {
            _stream = stream;
        }

        public override void Flush()
        {

        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (offset != 0)
            {
                throw new ArgumentException($"Offset must be 0");
            }

            if (count + Position > Length)
            {
                count = (int)(Length - Position);
            }

            var n = _stream.Read(buffer, Position, count);
            Position += n;
            return n;
        }



        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin != SeekOrigin.Begin)
            {
                throw new ArgumentException($"{nameof(origin)} must be SeekOrigin.Begin");
            }

            return Position = offset;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (offset != 0)
            {
                throw new ArgumentException($"Offset must be 0");
            }

            if (count != buffer.Length)
            {
                throw new ArgumentException($"{nameof(count)} must equal {nameof(buffer)}.Length");
            }

            _stream.Write(buffer, Position);
        }

        public override bool CanRead { get; } = true;
        public override bool CanSeek { get; } = true;
        public override bool CanWrite { get; } = true;
        public override long Length => _stream.Size;
        public override long Position { get; set; }
    }
}