using System;
using System.Threading.Tasks;
using dotnetCampus.Ipc.PipeCore.Context;
using dotnetCampus.Ipc.PipeCore.Utils;

namespace dotnetCampus.Ipc.Abstractions
{
    public interface IPeerProxy
    {
        string PeerName { get; }

        IpcMessageWriter IpcMessageWriter { get; }

        /// <summary>
        /// 当收到消息时触发
        /// </summary>
        event EventHandler<IPeerMessageArgs> MessageReceived;

        Task<IpcBufferMessage> GetResponseAsync(IpcRequestMessage request);
    }
}
