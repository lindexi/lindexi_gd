using System;
using System.Diagnostics;
using System.IO;

namespace OpenMcdf
{
    public class ForwardSeekStream : Stream
    {
        public ForwardSeekStream(Stream sourceStream, IByteArrayPool byteArrayPool)
        {
            _sourceStream = sourceStream;
            _byteArrayPool = byteArrayPool;
        }

        public override void Flush()
        {
            _sourceStream.Flush();
        }

        public long CurrentPosition { private set; get; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var n = _sourceStream.Read(buffer, offset, count);
            CurrentPosition += n;
            return n;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (offset == 0 && origin == SeekOrigin.Begin)
            {
                return CurrentPosition;
            }

            if (offset == CurrentPosition) return CurrentPosition;
            if (offset > CurrentPosition)
            {
                int length = (int)(offset - CurrentPosition);
                var byteList = _byteArrayPool.Rent(length);
                Read(byteList, 0, length);
                _byteArrayPool.Return(byteList);

                Debug.Assert(offset == CurrentPosition);
                return CurrentPosition;
            }

            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            _sourceStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _sourceStream.Write(buffer, offset, count);
        }

        public override bool CanRead => _sourceStream.CanRead;

        public override bool CanSeek => true;

        public override bool CanWrite => _sourceStream.CanWrite;

        public override long Length => _sourceStream.Length;

        public override long Position
        {
            get => _sourceStream.Position;
            set => _sourceStream.Position = value;
        }

        private readonly Stream _sourceStream;
        private readonly IByteArrayPool _byteArrayPool;
    }
}