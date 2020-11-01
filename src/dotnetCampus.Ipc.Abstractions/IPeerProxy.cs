using System;
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
    }
}