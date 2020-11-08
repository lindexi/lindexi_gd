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
    /// <para/>
    /// 这是用来期待发送一条消息之后，在对方的业务层能有回复的消息 <para/>
    /// 这是服务器-客户端模型 <para/>
    /// 客户端向服务器发起请求，服务器端业务层处理之后返回响应信息 <para/>
    /// 通过调用 <see cref="CreateRequestMessage"/> 方法创建出请求消息 <para/>
    /// 然后将此消息的 <see cref="IpcClientRequestMessage.IpcBufferMessageContext"/> 通过现有 <see cref="PeerProxy"/> 发送到服务器端。同时客户端可以使用 <see cref="IpcClientRequestMessage.Task"/> 进行等待 <para/>
    /// 服务器端接收到 <see cref="IpcClientRequestMessage"/> 的内容，将会在 <see cref="OnIpcClientRequestReceived"/> 事件触发，这个事件将会带上 <see cref="IpcClientRequestArgs"/> 参数 <para/>
    /// 在服务器端处理完成之后，底层的方法是通过调用 <see cref="CreateResponseMessage"/> 方法创建响应消息，通过 <see cref="PeerProxy"/> 发送给客户端 <para/>
    /// 客户端收到了服务器端的响应信息，将会释放 <see cref="IpcClientRequestMessage.Task"/> 任务，客户端从 <see cref="IpcClientRequestMessage.Task"/> 可以拿到服务器端的返回值
    /// </summary>
    class ResponseManager
    {
        public IpcClientRequestMessage CreateRequestMessage(IpcRequestMessage request)
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

            var requestMessage = CreateRequestMessageInner(request, currentMessageId);
            return new IpcClientRequestMessage(requestMessage, task.Task, new IpcClientRequestMessageId(currentMessageId));
        }

        private IpcBufferMessageContext CreateRequestMessageInner(in IpcRequestMessage request, ulong currentMessageId)
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
                IpcMessageCommandType.RequestMessage,
                new IpcBufferMessage(RequestMessageHeader),
                new IpcBufferMessage(currentMessageIdByteList),
                new IpcBufferMessage(requestMessageLengthByteList),
                request.RequestMessage
            );
        }

        public IpcBufferMessageContext CreateResponseMessage(IpcClientRequestMessageId messageId, IpcBufferMessage response, string summary)
        {
            /*
           * MessageHeader
           * MessageId
            * Response Message Length
           * Response Message
           */
            var currentMessageIdByteList = BitConverter.GetBytes(messageId.MessageIdValue);

            var responseMessageLengthByteList = BitConverter.GetBytes(response.Count);
            return new IpcBufferMessageContext
            (
                summary,
                IpcMessageCommandType.ResponseMessage,
                new IpcBufferMessage(ResponseMessageHeader),
                new IpcBufferMessage(currentMessageIdByteList),
                new IpcBufferMessage(responseMessageLengthByteList),
                response
            );
        }

        private Dictionary<ulong, TaskCompletionSource<IpcBufferMessage>> TaskList { get; } =
            new Dictionary<ulong, TaskCompletionSource<IpcBufferMessage>>();

        public void OnReceiveMessage(PeerMessageArgs args)
        {
            HandleResponse(args);
            if (args.Handle)
            {
                return;
            }

            HandleRequest(args);
        }

        private void HandleRequest(PeerMessageArgs args)
        {
            Stream message = args.Message;
            if (message.Length < RequestMessageHeader.Length + sizeof(ulong))
            {
                return;
            }

            var currentPosition = message.Position;
            try
            {
                if (CheckRequestHeader(message))
                {
                    // 标记在这一级消费
                    args.SetHandle(message: nameof(HandleRequest));

                    var binaryReader = new BinaryReader(message);
                    var messageId = binaryReader.ReadUInt64();
                    var requestMessageLength = binaryReader.ReadInt32();
                    var requestMessageByteList = binaryReader.ReadBytes(requestMessageLength);
                    var ipcClientRequestArgs =
                        new IpcClientRequestArgs(new IpcClientRequestMessageId(messageId), new IpcBufferMessage(requestMessageByteList));
                    OnIpcClientRequestReceived?.Invoke(this, ipcClientRequestArgs);
                }
            }
            finally
            {
                message.Position = currentPosition;
            }
        }

        public event EventHandler<IpcClientRequestArgs>? OnIpcClientRequestReceived;

        private void HandleResponse(PeerMessageArgs args)
        {
            Stream message = args.Message;

            if (message.Length < ResponseMessageHeader.Length + sizeof(ulong))
            {
                return;
            }

            var currentPosition = message.Position;
            try
            {
                if (CheckResponseHeader(message))
                {
                    // 标记在这一级消费
                    args.SetHandle(message: nameof(HandleResponse));

                    var binaryReader = new BinaryReader(message);
                    var messageId = binaryReader.ReadUInt64();
                    TaskCompletionSource<IpcBufferMessage>? task = null;
                    lock (Locker)
                    {
                        if (TaskList.TryGetValue(messageId, out task))
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

                    var responseMessageLength = binaryReader.ReadInt32();
                    var responseMessageByteList = binaryReader.ReadBytes(responseMessageLength);
                    task.SetResult(new IpcBufferMessage(responseMessageByteList));
                }
            }
            finally
            {
                message.Position = currentPosition;
            }
        }

        private static bool CheckHeader(Stream stream, byte[] header)
        {
            for (var i = 0; i < header.Length; i++)
            {
                if (stream.ReadByte() == header[i])
                {
                }
                else
                {
                    return false;
                }
            }

            return true;
        }
        private bool CheckResponseHeader(Stream stream)
        {
            var header = ResponseMessageHeader;

            return CheckHeader(stream, header);
        }
        private bool CheckRequestHeader(Stream stream)
        {
            var header = RequestMessageHeader;
            return CheckHeader(stream, header);
        }

        private object Locker => RequestMessageHeader;

        /// <summary>
        /// 用于标识请求消息
        /// </summary>
        /// 0x52, 0x65, 0x71, 0x75, 0x65, 0x73, 0x74 0x00 就是 Request 字符
        private byte[] RequestMessageHeader { get; } = { 0x52, 0x65, 0x71, 0x75, 0x65, 0x73, 0x74, 0x00 };

        private byte[] ResponseMessageHeader { get; } = { 0x52, 0x65, 0x73, 0x70, 0x6F, 0x6E, 0x73, 0x65 };

        private ulong CurrentMessageId { set; get; }
    }
}
