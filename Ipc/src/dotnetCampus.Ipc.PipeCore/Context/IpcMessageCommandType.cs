namespace dotnetCampus.Ipc.PipeCore.Context
{
    /// <summary>
    /// 用于作为命令类型，用于框架的命令和业务的命令
    /// </summary>
    public enum IpcMessageCommandType : ushort
    {
        /// <summary>
        /// 向对方服务器注册
        /// </summary>
        PeerRegister = 0B0001,

        /// <summary>
        /// 发送回复信息
        /// </summary>
        SendAck = 0B0010,

        /// <summary>
        /// 发送回复信息，同时向对方服务器注册
        /// </summary>
        SendAckAndRegisterToPeer = PeerRegister | SendAck,

        /// <summary>
        /// 业务层的消息
        /// </summary>
        Business = 0B0000_1000_0000,

        /// <summary>
        /// 请求信息，这也是业务层消息
        /// </summary>
        RequestMessage = 0B0001_0000_0000 | Business,

        /// <summary>
        /// 回应信息，这也是业务层消息
        /// </summary>
        ResponseMessage = 0B0010_0000_0000 | Business,

        /// <summary>
        /// 其他信息
        /// </summary>
        Unknown = ushort.MaxValue,
    }
}
