using System;

namespace dotnetCampus.Ipc.PipeCore.Context
{
    /// <summary>
    /// 对方连接的事件参数
    /// </summary>
    /// 这是给上层使用的事件参数
    public class PeerConnectedArgs : EventArgs
    {
        /// <summary>
        /// 创建对方连接的事件参数
        /// </summary>
        /// <param name="peer"></param>
        public PeerConnectedArgs(PeerProxy peer)
        {
            Peer = peer;
        }

        /// <summary>
        /// 获取用来代表对方的属性
        /// </summary>
        public PeerProxy Peer { get; }
    }
}
