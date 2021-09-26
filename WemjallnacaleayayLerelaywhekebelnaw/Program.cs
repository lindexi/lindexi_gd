using System;
using System.Buffers;

namespace WemjallnacaleayayLerelaywhekebelnaw
{
    class Program
    {
        static void Main(string[] args)
        {
            var newSize = 200;
            var _buffer = new byte[100];
            for (int i = 0; i < _buffer.Length; i++)
            {
                _buffer[i] = (byte)i;
            }

            byte[] newBuffer = ArrayPool<byte>.Shared.Rent(newSize);

            unsafe
            {
                fixed (byte* pBuffer = _buffer)
                fixed (byte* pNewBuffer = newBuffer)
                {
                    Buffer.MemoryCopy(pBuffer, pNewBuffer, _buffer.Length, _buffer.Length);
                }
            }
        }
    }
}
