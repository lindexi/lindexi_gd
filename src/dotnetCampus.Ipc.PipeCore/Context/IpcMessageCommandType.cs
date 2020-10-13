namespace dotnetCampus.Ipc.PipeCore.Context
{
    /// <summary>
    /// 用于作为命令类型，用于框架的命令和业务的命令
    /// </summary>
    internal enum IpcMessageCommandType : ushort
    {
        /// <summary>
        /// 业务层的消息
        /// </summary>
        Business = 0,

        /// <summary>
        /// 向对方服务器注册
        /// </summary>
        PeerRegister = 1,

        /// <summary>
        /// 发送回复信息
        /// </summary>
        SendAck = 2,

        /// <summary>
        /// 发送回复信息，同时向对方服务器注册
        /// </summary>
        SendAckAndRegisterToPeer = PeerRegister | SendAck,

        /// <summary>
        /// 其他信息
        /// </summary>
        Unknown = ushort.MaxValue,
    }
}
