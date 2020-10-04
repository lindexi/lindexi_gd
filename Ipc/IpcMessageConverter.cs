using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Ipc
{
    internal static class IpcMessageConverter
    {
        public static async Task WriteAsync(Stream stream, byte[] messageHeader, ulong ack, byte[] buffer, int offset, int count)
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
            UInt16 messageHeaderLength = (ushort)messageHeader.Length;
            binaryWriter.Write(messageHeaderLength);

            await stream.WriteAsync(messageHeader);
            binaryWriter.Write(version);
            binaryWriter.Write(ack);
            // Empty
            binaryWriter.Write((UInt32.MinValue));
            binaryWriter.Write(count);
            await stream.WriteAsync(buffer, offset, count);
        }

        public static async Task<(bool success, IpcMessageContext ipcMessageContext)> ReadAsync(Stream stream, byte[] messageHeader, ISharedArrayPool sharedArrayPool, int maxMessageLength = ushort.MaxValue * byte.MaxValue)
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
            {
                // 消息不对，忽略
                return (false, default)!;
            }

            var binaryReader = new BinaryReader(stream);
            var version = binaryReader.ReadUInt32();
            Debug.Assert(version == 0);

            var ack = binaryReader.ReadUInt64();

            var empty = binaryReader.ReadUInt32();
            Debug.Assert(empty == 0);

            var messageLength = binaryReader.ReadUInt32();

            if (messageLength > maxMessageLength)
            {
                // 太长了
                return (false, default)!;
            }

            var messageBuffer = sharedArrayPool.Rent((int)messageLength);
            var readCount = await stream.ReadAsync(messageBuffer, 0, (int)messageLength).ConfigureAwait(false);

            Debug.Assert(readCount == messageLength);

            var ipcMessageContext = new IpcMessageContext(ack, messageBuffer, messageLength, sharedArrayPool);
            return (true, ipcMessageContext);
        }

        private static async Task<bool> GetHeader(Stream stream, byte[] messageHeader, ISharedArrayPool sharedArrayPool)
        {
            var binaryReader = new BinaryReader(stream);
            var messageHeaderLength = binaryReader.ReadUInt16();
            Debug.Assert(messageHeaderLength == messageHeader.Length);
            if (messageHeaderLength != messageHeader.Length)
            {
                // 消息不对，忽略
                return false;
            }

            var messageHeaderBuffer = sharedArrayPool.Rent(messageHeader.Length);

            try
            {
                var readCount = await stream.ReadAsync(messageHeaderBuffer, 0, messageHeader.Length).ConfigureAwait(false);
                Debug.Assert(readCount == messageHeader.Length);
                if (ByteListExtension.Equals(messageHeaderBuffer, messageHeader, readCount))
                {
                    // 读对了
                    return true;
                }
                else
                {
                    // 发过来的消息是出错的
                    return false;
                }
            }
            finally
            {
                sharedArrayPool.Return(messageHeaderBuffer);
            }
        }
    }
}