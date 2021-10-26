using dotnetCampus.Ipc.Abstractions.Context;

namespace dotnetCampus.Ipc.Abstractions
{
    /// <summary>
    /// 客户端请求的上下文
    /// </summary>
    public interface IIpcRequestContext
    {
        /// <summary>
        /// 是否已处理
        /// </summary>
        bool Handle { get; set; }

        /// <summary>
        /// 收到客户端发生过来的消息
        /// </summary>
        IpcBufferMessage IpcBufferMessage { get; }

        /// <summary>
        /// 发送请求的对方
        /// </summary>
        IPeerProxy Peer { get; }
    }
}
