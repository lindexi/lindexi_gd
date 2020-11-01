using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using dotnetCampus.Ipc.Abstractions;
using dotnetCampus.Ipc.PipeCore.Context;

namespace dotnetCampus.Ipc.PipeCore
{
    /// <summary>
    /// 请求管理
    /// </summary>
    class ResponseManager
    {
        public ResponseManager(IClientMessageWriter messageWriter)
        {
            _messageWriter = messageWriter;
        }

        public async Task<IpcBufferMessage> GetResponseAsync(IpcRequestMessage request)
        {
            ulong currentMessageId;
            var task = new TaskCompletionSource<IpcBufferMessage>();
            lock (Locker)
            {
                currentMessageId = CurrentMessageId;
                // 在超过 ulong.MaxValue 之后，将会是 0 这个值
                CurrentMessageId++;

                TaskList[currentMessageId] = task;
            }

            var requestMessage = CreateRequestMessage(request, currentMessageId);
            await _messageWriter.WriteMessageAsync(requestMessage);
            return await task.Task;
        }

        private Dictionary<ulong, TaskCompletionSource<IpcBufferMessage>> TaskList { get; } = new Dictionary<ulong, TaskCompletionSource<IpcBufferMessage>>();

        public void ReceiveMessage(PeerMessageArgs args)
        {
            Stream message = args.Message;
            if (message.Length < RequestMessageHeader.Length + sizeof(ulong))
            {
                return;
            }

            var currentPosition = message.Position;
            try
            {
                if (CheckHeader(message))
                {
                    // 标记在这一级消费
                    args.Handle = true;

                    var binaryReader = new BinaryReader(message);
                    var messageId = binaryReader.ReadUInt64();
                    TaskCompletionSource<IpcBufferMessage>? task=null;
                    lock (Locker)
                    {
                        if (TaskList.TryGetValue(messageId,out task))
                        {
                        }
                        else
                        {
                            return;
                        }
                    }

                    if (task == null)
                    {
                        return;
                    }

                    var requestMessageLength = binaryReader.ReadInt32();
                    var responseMessageByteList = binaryReader.ReadBytes(requestMessageLength);
                    task.SetResult(new IpcBufferMessage(responseMessageByteList));
                }
            }
            finally
            {
                message.Position = currentPosition;
            }
        }

        private bool CheckHeader(Stream stream)
        {
            for (var i = 0; i < RequestMessageHeader.Length; i++)
            {
                if (stream.ReadByte()== RequestMessageHeader[i])
                {
                    
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private readonly IClientMessageWriter _messageWriter;

        private IpcBufferMessageContext CreateRequestMessage(in IpcRequestMessage request, ulong currentMessageId)
        {
            /*
            * MessageHeader
            * MessageId
             * Request Message Length
            * Request Message
            */
            var currentMessageIdByteList = BitConverter.GetBytes(currentMessageId);

            var requestMessageLengthByteList = BitConverter.GetBytes(request.RequestMessage.Count);

            return new IpcBufferMessageContext
            (
                request.Summary,
                IpcMessageCommandType.Business,
                new IpcBufferMessage(RequestMessageHeader),
                new IpcBufferMessage(currentMessageIdByteList),
                new IpcBufferMessage(requestMessageLengthByteList),
                request.RequestMessage
            );
        }

        private object Locker => RequestMessageHeader;

        /// <summary>
        /// 用于标识请求消息
        /// </summary>
        /// 0x52, 0x65, 0x71, 0x75, 0x65, 0x73, 0x74 0x00 就是 Request 字符
        private byte[] RequestMessageHeader { get; } = { 0x52, 0x65, 0x71, 0x75, 0x65, 0x73, 0x74, 0x00 };

        private ulong CurrentMessageId { set; get; }
    }
}
