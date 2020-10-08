using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using dotnetCampus.Ipc.PipeCore.Context;
using dotnetCampus.Ipc.PipeCore.Utils;

namespace dotnetCampus.Ipc.PipeCore
{
    /// <summary>
    /// 消息的封包和解包代码，用于将传入的内容包装为 Ipc 通讯使用的二进制内容，或将 Ipc 通讯使用的二进制内容读取为业务端使用的内容
    /// </summary>
    internal static class IpcMessageConverter
    {
        public static async Task WriteAsync(Stream stream, byte[] messageHeader, Ack ack,
            IpcBufferMessageContext ipcBufferMessageContext, ILogger logger)
        {
            logger.Debug($"[{nameof(IpcMessageConverter)}] Start Write {ipcBufferMessageContext.Summary}");

            var binaryWriter = await WriteHeaderAsync(stream, messageHeader, ack);

            await binaryWriter.WriteAsync(ipcBufferMessageContext.Length);
            foreach (var ipcBufferMessage in ipcBufferMessageContext.IpcBufferMessageList)
            {
                await stream.WriteAsync(ipcBufferMessage.Buffer, ipcBufferMessage.Start, ipcBufferMessage.Count);
            }

            logger.Debug($"[{nameof(IpcMessageConverter)}] Finished Write {ipcBufferMessageContext.Summary}");
        }

        public static async Task WriteAsync(Stream stream, byte[] messageHeader, Ack ack,
            IpcBufferMessage ipcBufferMessage, string? summary, ILogger logger)
        {
            logger.Debug($"[{nameof(IpcMessageConverter)}] Start Write {summary}");

            var binaryWriter = await WriteHeaderAsync(stream, messageHeader, ack);

            await binaryWriter.WriteAsync(ipcBufferMessage.Count);

            await stream.WriteAsync(ipcBufferMessage.Buffer, ipcBufferMessage.Start, ipcBufferMessage.Count);

            logger.Debug($"[{nameof(IpcMessageConverter)}] Finished Write {summary}");
        }

        public static async Task WriteAsync(Stream stream, byte[] messageHeader, Ack ack, byte[] buffer, int offset,
            int count, string? summary, ILogger logger)
        {
            logger.Debug($"[{nameof(IpcMessageConverter)}] Start Write {summary}");

            var binaryWriter = await WriteHeaderAsync(stream, messageHeader, ack);

            await binaryWriter.WriteAsync(count);
            await stream.WriteAsync(buffer, offset, count);

            logger.Debug($"[{nameof(IpcMessageConverter)}] Finished Write {summary}");
        }

        public static async Task<AsyncBinaryWriter> WriteHeaderAsync(Stream stream, byte[] messageHeader, Ack ack)
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

            var asyncBinaryWriter = new AsyncBinaryWriter(stream);
            var messageHeaderLength = (ushort)messageHeader.Length;
            await asyncBinaryWriter.WriteAsync(messageHeaderLength);

            await stream.WriteAsync(messageHeader);
            await asyncBinaryWriter.WriteAsync(version);
            await asyncBinaryWriter.WriteAsync(ack.Value);
            // Empty
            await asyncBinaryWriter.WriteAsync(uint.MinValue);
            return asyncBinaryWriter;
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

            var readCount = await ReadBufferAsync(stream, messageBuffer, (int)messageLength);

            Debug.Assert(readCount == messageLength);

            var ipcMessageContext = new IpcMessageContext(ack, messageBuffer, messageLength, sharedArrayPool);
            return (true, ipcMessageContext);
        }

        private static async Task<int> ReadBufferAsync(Stream stream, byte[] messageBuffer, int messageLength)
        {
            var readCount = 0;

            do
            {
                var n = await stream.ReadAsync(messageBuffer, readCount, (int)messageLength - readCount)
                    .ConfigureAwait(false);
                readCount += n;
            } while (readCount < messageLength);

            return readCount;
        }


        private static async Task<bool> GetHeader(Stream stream, byte[] messageHeader, ISharedArrayPool sharedArrayPool)
        {
            var binaryReader = new AsyncBinaryReader(stream);
            var messageHeaderLength = await binaryReader.ReadUInt16Async();
            Debug.Assert(messageHeaderLength == messageHeader.Length);
            if (messageHeaderLength != messageHeader.Length)
            {
                // 消息不对，忽略
                return false;
            }

            var messageHeaderBuffer = sharedArrayPool.Rent(messageHeader.Length);

            try
            {
                var readCount = await ReadBufferAsync(stream, messageHeaderBuffer, messageHeader.Length);
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