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

            var binaryWriter = await WriteHeaderAsync(stream, messageHeader, ack, ipcBufferMessageContext.IpcMessageCommandType);

            await binaryWriter.WriteAsync(ipcBufferMessageContext.Length);
            foreach (var ipcBufferMessage in ipcBufferMessageContext.IpcBufferMessageList)
            {
                await stream.WriteAsync(ipcBufferMessage.Buffer, ipcBufferMessage.Start, ipcBufferMessage.Count);
            }

            logger.Debug($"[{nameof(IpcMessageConverter)}] Finished Write {ipcBufferMessageContext.Summary}");
        }

        public static async Task WriteAsync(Stream stream, byte[] messageHeader, Ack ack,
            IpcMessageCommandType ipcMessageCommandType,
            IpcBufferMessage ipcBufferMessage, string? summary, ILogger logger)
        {
            logger.Debug($"[{nameof(IpcMessageConverter)}] Start Write {summary}");

            var binaryWriter = await WriteHeaderAsync(stream, messageHeader, ack, ipcMessageCommandType);

            await binaryWriter.WriteAsync(ipcBufferMessage.Count);

            await stream.WriteAsync(ipcBufferMessage.Buffer, ipcBufferMessage.Start, ipcBufferMessage.Count);

            logger.Debug($"[{nameof(IpcMessageConverter)}] Finished Write {summary}");
        }

        public static async Task WriteAsync(Stream stream, byte[] messageHeader, Ack ack,
            IpcMessageCommandType ipcMessageCommandType, byte[] buffer, int offset,
            int count, string? summary, ILogger logger)
        {
            logger.Debug($"[{nameof(IpcMessageConverter)}] Start Write {summary}");

            var binaryWriter = await WriteHeaderAsync(stream, messageHeader, ack, ipcMessageCommandType);

            await binaryWriter.WriteAsync(count);
            await stream.WriteAsync(buffer, offset, count);

            logger.Debug($"[{nameof(IpcMessageConverter)}] Finished Write {summary}");
        }

        public static async Task<AsyncBinaryWriter> WriteHeaderAsync(Stream stream, byte[] messageHeader, Ack ack,
            IpcMessageCommandType ipcMessageCommandType)
        {
            /*
             * UInt16 Message Header Length 消息头的长度
             * byte[] Message Header        消息头的内容
             * UInt32 Version        当前IPC服务的版本
             * UInt64 Ack            用于给对方确认收到消息使用
             * UInt32 Empty          给以后版本使用的值
             * UInt16 Command Type   命令类型，业务端的值将会是 0 而框架层采用其他值
             * UInt32 Content Length 这条消息的内容长度
             * byte[] Content        实际的内容
             */

            // 当前版本默认是 0 版本，这个值用来后续如果有协议上的更改时，兼容旧版本使用
            const uint version = 0;

            var asyncBinaryWriter = new AsyncBinaryWriter(stream);
            var messageHeaderLength = (ushort) messageHeader.Length;
            await asyncBinaryWriter.WriteAsync(messageHeaderLength);

            await stream.WriteAsync(messageHeader);
            // UInt32 Version
            await asyncBinaryWriter.WriteAsync(version);
            // UInt64 Ack
            await asyncBinaryWriter.WriteAsync(ack.Value);
            // UInt32 Empty
            await asyncBinaryWriter.WriteAsync(uint.MinValue);
            // UInt16 Command Type   命令类型，业务端的值将会是 0 而框架层采用其他值
            ushort commandType = (ushort) ipcMessageCommandType;
            await asyncBinaryWriter.WriteAsync(commandType);

            return asyncBinaryWriter;
        }

        public static async Task<IpcMessageResult> ReadAsync(Stream stream,
            byte[] messageHeader, ISharedArrayPool sharedArrayPool,
            int maxMessageLength = ushort.MaxValue * byte.MaxValue)
        {
            /*
             * UInt16 Message Header Length 消息头的长度
             * byte[] Message Header        消息头的内容
             * UInt32 Version        当前IPC服务的版本
             * UInt64 Ack            用于给对方确认收到消息使用
             * UInt32 Empty          给以后版本使用的值
             * UInt16 Command Type   命令类型，业务端的值将会是 0 而框架层采用其他值
             * UInt32 Content Length 这条消息的内容长度
             * byte[] Content        实际的内容
             */

            if (!await GetHeader(stream, messageHeader, sharedArrayPool))
            {
                // 消息不对，忽略
                return new IpcMessageResult("Message Header no match");
            }

            var binaryReader = new BinaryReader(stream);
            // UInt32 Version        当前IPC服务的版本
            var version = binaryReader.ReadUInt32();
            Debug.Assert(version == 0);

            // UInt64 Ack            用于给对方确认收到消息使用
            var ack = binaryReader.ReadUInt64();

            // UInt32 Empty          给以后版本使用的值
            var empty = binaryReader.ReadUInt32();
            Debug.Assert(empty == 0);

            // UInt16 Command Type   命令类型，业务端的值将会是 0 而框架层采用其他值
            var commandType = (IpcMessageCommandType) binaryReader.ReadUInt16();

            // UInt32 Content Length 这条消息的内容长度
            var messageLength = binaryReader.ReadUInt32();

            if (messageLength > maxMessageLength)
            {
                // 太长了
                return new IpcMessageResult(
                    $"Message Length too long  MessageLength={messageLength} MaxMessageLength={maxMessageLength}");
            }

            var messageBuffer = sharedArrayPool.Rent((int) messageLength);
            // byte[] Content        实际的内容
            var readCount = await ReadBufferAsync(stream, messageBuffer, (int) messageLength);

            Debug.Assert(readCount == messageLength);

            var ipcMessageContext = new IpcMessageContext(ack, messageBuffer, messageLength, sharedArrayPool);
            return new IpcMessageResult(success: true, ipcMessageContext, commandType);
        }

        private static async Task<int> ReadBufferAsync(Stream stream, byte[] messageBuffer, int messageLength)
        {
            var readCount = 0;

            do
            {
                var n = await stream.ReadAsync(messageBuffer, readCount, (int) messageLength - readCount);
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
