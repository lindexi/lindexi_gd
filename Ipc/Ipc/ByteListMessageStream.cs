using System.IO;

namespace Ipc
{
    class ByteListMessageStream : MemoryStream
    {
        private readonly ISharedArrayPool _sharedArrayPool;
        private byte[] Buffer { get; }

        public ByteListMessageStream(byte[] buffer, int count, ISharedArrayPool sharedArrayPool) : base(buffer, 0, count, false)
        {
            _sharedArrayPool = sharedArrayPool;
            Buffer = buffer;
        }

        ~ByteListMessageStream()
        {
            _sharedArrayPool.Return(Buffer);
        }
    }
}