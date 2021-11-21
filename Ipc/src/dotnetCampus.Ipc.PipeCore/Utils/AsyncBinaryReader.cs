using System;
using System.IO;
using System.Threading.Tasks;

namespace dotnetCampus.Ipc.PipeCore.Utils
{
    class AsyncBinaryReader
    {
        public AsyncBinaryReader(Stream stream)
        {
            Stream = stream;
        }

        private Stream Stream { get; }

        public async Task<ushort> ReadUInt16Async()
        {
            var byteList = await InternalReadAsync(2);
            return BitConverter.ToUInt16(byteList, 0);
        }

        public async Task<UInt64> ReadReadUInt64Async()
        {
            var byteList = await InternalReadAsync(sizeof(UInt64));
            return BitConverter.ToUInt64(byteList, 0);
        }

        public async Task<UInt32> ReadUInt32Async()
        {
            var byteList = await InternalReadAsync(sizeof(UInt32));
            return BitConverter.ToUInt32(byteList, 0);
        }

        private async Task<byte[]> InternalReadAsync(int numBytes)
        {
            var byteList = new byte[numBytes];
            var bytesRead = 0;

            do
            {
                var n = await Stream.ReadAsync(byteList, bytesRead, numBytes - bytesRead).ConfigureAwait(false);
                if (n == 0)
                {
                    throw new EndOfStreamException();
                }

                bytesRead += n;
            } while (bytesRead < numBytes);

            return byteList;
        }
    }
}
