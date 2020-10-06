using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Ipc
{
    internal static class IpcMessageConverter
    {
        public static async Task WriteAsync(Stream stream, byte[] messageHeader, Ack ack,
            IpcBufferMessageContext ipcBufferMessageContext)
        {
            var binaryWriter = await WriteHeaderAsync(stream, messageHeader, ack);

            binaryWriter.Write(ipcBufferMessageContext.Length);
            foreach (var ipcBufferMessage in ipcBufferMessageContext.IpcBufferMessageList)
            {
                await stream.WriteAsync(ipcBufferMessage.Buffer, ipcBufferMessage.Start, ipcBufferMessage.Count);
            }
        }

        public static async Task WriteAsync(Stream stream, byte[] messageHeader, Ack ack,
            IpcBufferMessage ipcBufferMessage)
        {
            var binaryWriter = await WriteHeaderAsync(stream, messageHeader, ack);

            binaryWriter.Write(ipcBufferMessage.Count);

            await stream.WriteAsync(ipcBufferMessage.Buffer, ipcBufferMessage.Start, ipcBufferMessage.Count);
        }

        public static async Task WriteAsync(Stream stream, byte[] messageHeader, Ack ack, byte[] buffer, int offset,
            int count)
        {
            var binaryWriter = await WriteHeaderAsync(stream, messageHeader, ack);

            binaryWriter.Write(count);
            await stream.WriteAsync(buffer, offset, count);
        }

        public static async Task<BinaryWriter> WriteHeaderAsync(Stream stream, byte[] messageHeader, Ack ack)
        {
            /*
             * UInt16 Message Header Length
             * byte[] Message Header
             * UInt32 Version
             * UInt64 Ack
             * UInt32 Empty
             * UInt32 Content Length
             * byte[] Content
             */

            uint version = 0;

            var binaryWriter = new BinaryWriter(stream);
            var messageHeaderLength = (ushort)messageHeader.Length;
            binaryWriter.Write(messageHeaderLength);

            await stream.WriteAsync(messageHeader);
            binaryWriter.Write(version);
            binaryWriter.Write(ack.Value);
            // Empty
            binaryWriter.Write(uint.MinValue);
            return binaryWriter;
        }

        public static async Task<(bool success, IpcMessageContext ipcMessageContext)> ReadAsync(Stream stream,
            byte[] messageHeader, ISharedArrayPool sharedArrayPool,
            int maxMessageLength = ushort.MaxValue * byte.MaxValue)
        {
            /*
            * UInt16 Message Header Length
            * byte[] Message Header
            * UInt32 Version
            * UInt64 Ack
            * UInt32 Empty
            * UInt32 Content Length
            * byte[] Content
            */

            if (!await GetHeader(stream, messageHeader, sharedArrayPool))
                // 消息不对，忽略
                return (false, default)!;

            var binaryReader = new BinaryReader(stream);
            var version = binaryReader.ReadUInt32();
            Debug.Assert(version == 0);

            var ack = binaryReader.ReadUInt64();

            var empty = binaryReader.ReadUInt32();
            Debug.Assert(empty == 0);

            var messageLength = binaryReader.ReadUInt32();

            if (messageLength > maxMessageLength)
                // 太长了
                return (false, default)!;

            var messageBuffer = sharedArrayPool.Rent((int)messageLength);
            var readCount = await stream.ReadAsync(messageBuffer, 0, (int)messageLength).ConfigureAwait(false);

            Debug.Assert(readCount == messageLength);

            var ipcMessageContext = new IpcMessageContext(ack, messageBuffer, messageLength, sharedArrayPool);
            return (true, ipcMessageContext);
        }

        private static async Task<bool> GetHeader(Stream stream, byte[] messageHeader, ISharedArrayPool sharedArrayPool)
        {
            var binaryReader = new AsyncBinaryReader(stream);
            var messageHeaderLength = await binaryReader.ReadUInt16Async();
            Debug.Assert(messageHeaderLength == messageHeader.Length);
            if (messageHeaderLength != messageHeader.Length)
                // 消息不对，忽略
                return false;

            var messageHeaderBuffer = sharedArrayPool.Rent(messageHeader.Length);

            try
            {
                var readCount = await stream.ReadAsync(messageHeaderBuffer, 0, messageHeader.Length)
                    .ConfigureAwait(false);
                Debug.Assert(readCount == messageHeader.Length);
                if (ByteListExtension.Equals(messageHeaderBuffer, messageHeader, readCount))
                    // 读对了
                    return true;
                else
                    // 发过来的消息是出错的
                    return false;
            }
            finally
            {
                sharedArrayPool.Return(messageHeaderBuffer);
            }
        }
    }
}