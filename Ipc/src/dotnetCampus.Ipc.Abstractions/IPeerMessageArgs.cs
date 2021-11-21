using System.IO;

namespace dotnetCampus.Ipc.Abstractions
{
    /// <summary>
    /// 收到的对方的信息事件参数
    /// </summary>
    public interface IPeerMessageArgs
    {
        /// <summary>
        /// 用于读取消息的内容
        /// </summary>
        public Stream Message { get; }

        /// <summary>
        /// 对方的名字，此名字是对方的服务器名字，可以用来连接
        /// </summary>
        public string PeerName { get; }
    }
}
