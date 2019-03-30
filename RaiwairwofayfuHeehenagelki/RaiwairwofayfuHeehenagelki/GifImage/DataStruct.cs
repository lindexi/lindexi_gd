

using System;
using System.IO;

namespace RaiwairwofayfuHeehenagelki.GifImage
{
    internal class DataStruct
    {

        internal DataStruct(int blockSize, Stream stream)
        {
            var streamHelper = new StreamHelper(stream);
            BlockSize = (byte) blockSize;
            if (BlockSize > 0)
            {
                Data = streamHelper.ReadByte(BlockSize);
            }
        }

        /// <summary>
        ///     块大小
        /// </summary>
        internal byte BlockSize { get; set; }

        internal byte[] Data { get; set; } = new byte[0];

        internal byte[] GetBuffer()
        {
            var buffer = new byte[BlockSize + 1];
            buffer[0] = BlockSize;
            Array.Copy(Data, 0, buffer, 1, BlockSize);
            return buffer;
        }
    }
}