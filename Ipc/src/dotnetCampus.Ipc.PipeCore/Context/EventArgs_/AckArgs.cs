using System;

namespace dotnetCampus.Ipc.PipeCore.Context
{
    /// <summary>
    /// 回复消息的事件参数
    /// </summary>
    public class AckArgs : EventArgs
    {
        /// <summary>
        /// 创建回复消息的事件参数
        /// </summary>
        /// <param name="peerName"></param>
        /// <param name="ack"></param>
        public AckArgs(string peerName, in Ack ack)
        {
            Ack = ack;
            PeerName = peerName;
        }

        /// <summary>
        /// 消息编号
        /// </summary>
        public Ack Ack { get; }

        /// <summary>
        /// 发送方的名字
        /// </summary>
        public string PeerName { get; }
    }
}
