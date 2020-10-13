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
            return BitConverter.ToUInt16(byteList);
        }

        private async Task<byte[]> InternalReadAsync(int numBytes)
        {
            var byteList = new byte[numBytes];
            var bytesRead = 0;

            do
            {
                var n = await Stream.ReadAsync(byteList, bytesRead, numBytes - bytesRead);
                if (n == 0)
                {
                }

                bytesRead += n;
            } while (bytesRead < numBytes);

            return byteList;
        }
    }
}
