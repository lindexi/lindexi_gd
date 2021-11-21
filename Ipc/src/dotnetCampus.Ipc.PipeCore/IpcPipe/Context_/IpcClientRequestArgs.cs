using System;
using dotnetCampus.Ipc.Abstractions.Context;
using dotnetCampus.Ipc.PipeCore.Context;

namespace dotnetCampus.Ipc.PipeCore.IpcPipe
{
    /// <summary>
    /// 来自客户端的请求事件参数
    /// </summary>
    public class IpcClientRequestArgs : EventArgs
    {
        /// <summary>
        /// 创建来自客户端的请求事件参数
        /// </summary>
        /// <param name="messageId"></param>
        /// <param name="ipcBufferMessage"></param>
        internal IpcClientRequestArgs(in IpcClientRequestMessageId messageId, in IpcBufferMessage ipcBufferMessage)
        {
            MessageId = messageId;
            IpcBufferMessage = ipcBufferMessage;
        }

        /// <summary>
        /// 消息号
        /// </summary>
        public IpcClientRequestMessageId MessageId { get; }

        /// <summary>
        /// 收到客户端发生过来的消息
        /// </summary>
        public IpcBufferMessage IpcBufferMessage { get; }
    }
}
