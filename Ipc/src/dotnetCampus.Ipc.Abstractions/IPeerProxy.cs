using System;
using System.Threading.Tasks;
using dotnetCampus.Ipc.Abstractions.Context;

namespace dotnetCampus.Ipc.Abstractions
{
    /// <summary>
    /// 用于表示远程的对方
    /// </summary>
    public interface IPeerProxy
    {
        /// <summary>
        /// 对方的服务器名
        /// </summary>
        string PeerName { get; }

        /// <summary>
        /// 用于写入数据
        /// </summary>
        IpcMessageWriter IpcMessageWriter { get; }

        /// <summary>
        /// 当收到消息时触发
        /// </summary>
        event EventHandler<IPeerMessageArgs> MessageReceived;

        /// <summary>
        /// 发送请求给对方，请求对方的响应。这是客户端-服务器端模式
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<IpcBufferMessage> GetResponseAsync(IpcRequestMessage request);

        //IpcRequestMessage HandleIpcRequestMessage(IIpcRequestContext requestContext);

        /// <summary>
        /// 对方连接断开事件
        /// </summary>
        event EventHandler<IPeerConnectionBrokenArgs> PeerConnectionBroken;
    }
}
