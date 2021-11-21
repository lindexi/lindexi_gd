using System.Diagnostics;
using dotnetCampus.Ipc.Abstractions.Context;

namespace dotnetCampus.Ipc.Abstractions
{
    /// <summary>
    /// 表示一条 IPC 请求消息
    /// </summary>
    public readonly struct IpcRequestMessage
    {
        /// <summary>
        /// 创建 IPC 请求消息
        /// </summary>
        /// <param name="summary"></param>
        /// <param name="requestMessage"></param>
        [DebuggerStepThrough]
        public IpcRequestMessage(string summary, IpcBufferMessage requestMessage)
        {
            Summary = summary;
            RequestMessage = requestMessage;
        }

        /// <summary>
        /// 创建 IPC 请求消息
        /// </summary>
        [DebuggerStepThrough]
        public IpcRequestMessage(string summary, byte[] buffer) : this(summary, new IpcBufferMessage(buffer))
        {
        }

        /// <summary>
        /// 消息的调试友好文本，用来描述这条消息是什么
        /// </summary>
        public string Summary { get; }

        /// <summary>
        /// 请求的信息
        /// </summary>
        public IpcBufferMessage RequestMessage { get; }
    }
}
