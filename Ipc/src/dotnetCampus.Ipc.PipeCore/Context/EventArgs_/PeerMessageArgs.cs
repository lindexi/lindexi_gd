using System;
using System.Diagnostics;
using System.IO;
using dotnetCampus.Ipc.Abstractions;

namespace dotnetCampus.Ipc.PipeCore.Context
{
    /// <summary>
    /// 对方通讯的消息事件参数
    /// </summary>
    public class PeerMessageArgs : EventArgs, IPeerMessageArgs
    {
        /// <summary>
        /// 创建对方通讯的消息事件参数
        /// </summary>
        /// <param name="peerName"></param>
        /// <param name="message"></param>
        /// <param name="ack"></param>
        /// <param name="messageCommandType"></param>
        [DebuggerStepThrough]
        public PeerMessageArgs(string peerName, Stream message, in Ack ack, IpcMessageCommandType messageCommandType)
        {
            Message = message;
            Ack = ack;
            MessageCommandType = messageCommandType;
            PeerName = peerName;
        }

        /// <summary>
        /// 用于读取消息的内容
        /// </summary>
        public Stream Message { get; }

        /// <summary>
        /// 消息编号
        /// </summary>
        public Ack Ack { get; }

        /// <summary>
        /// 对方的名字，此名字是对方的服务器名字，可以用来连接
        /// </summary>
        public string PeerName { get; }

        internal IpcMessageCommandType MessageCommandType { get; }

        /// <summary>
        /// 表示是否被上一级处理了，可以通过 <see cref="HandlerMessage"/> 了解处理者的信息
        /// </summary>
        public bool Handle { private set; get; }

        /// <summary>
        /// 处理者的消息
        /// </summary>
        /// 框架大了，不能只有 <see cref="Handle"/> 一个属性，还需要能做到调试，调试是谁处理了，因此加添加了这个属性
        public string? HandlerMessage { private set; get; }

        /// <summary>
        /// 设置被处理，同时添加 <paramref name="message"/> 用于调试的信息
        /// </summary>
        /// <param name="message">用于调试的信息，请记录是谁设置的，原因是什么</param>
        public void SetHandle(string message)
        {
            Handle = true;
            HandlerMessage = message;
        }
    }
}
