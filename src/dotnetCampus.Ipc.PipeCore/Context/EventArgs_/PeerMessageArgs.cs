using System;
using System.IO;

namespace dotnetCampus.Ipc.PipeCore.Context
{
    /// <summary>
    /// 对方通讯的消息事件参数
    /// </summary>
    public class PeerMessageArgs : EventArgs
    {
        /// <summary>
        /// 创建对方通讯的消息事件参数
        /// </summary>
        /// <param name="peerName"></param>
        /// <param name="message"></param>
        /// <param name="ack"></param>
        public PeerMessageArgs(string peerName, Stream message, in Ack ack)
        {
            Message = message;
            Ack = ack;
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
    }
}
