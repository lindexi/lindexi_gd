using System;
using System.IO;
using System.Threading.Tasks;

namespace dotnetCampus.Ipc.PipeCore.Utils
{
    class AsyncBinaryWriter
    {
        public AsyncBinaryWriter(Stream stream)
        {
            Stream = stream;
        }

        private Stream Stream { get; }

        public async Task WriteAsync(ushort value)
        {
            await Stream.WriteAsync(BitConverter.GetBytes(value));
        }

        public async Task WriteAsync(uint value)
        {
            await Stream.WriteAsync(BitConverter.GetBytes(value));
        }

        public async Task WriteAsync(ulong value)
        {
            await Stream.WriteAsync(BitConverter.GetBytes(value));
        }

        public async Task WriteAsync(int value)
        {
            await Stream.WriteAsync(BitConverter.GetBytes(value));
        }
    }
}
