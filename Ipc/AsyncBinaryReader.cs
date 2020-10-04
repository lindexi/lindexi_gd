using System;
using System.IO;
using System.Threading.Tasks;

namespace Ipc
{
    public class AsyncBinaryReader
    {
        public AsyncBinaryReader(Stream stream)
        {
            Stream = stream;
        }

        public async Task<ushort> ReadUInt16Async()
        {
            var byteList = await InternalReadAsync(2);
            return BitConverter.ToUInt16(byteList);
        }

        private async Task<byte[]> InternalReadAsync(int numBytes)
        {
            var byteList = new byte[numBytes];
            int bytesRead = 0;

            do
            {
                int n = await Stream.ReadAsync(byteList, bytesRead, numBytes - bytesRead);
                if (n == 0)
                {

                }
                bytesRead += n;
            } while (bytesRead < numBytes);

            return byteList;
        }

        private Stream Stream { get; }
    }
}