namespace dotnetCampus.Ipc.PipeCore.Context
{
    /// <summary>
    /// 用于作为命令类型，用于框架的命令和业务的命令
    /// </summary>
    public enum IpcMessageCommandType : short
    {
        /// <summary>
        /// 向对方服务器注册
        /// </summary>
        PeerRegister = -1,

        /*
        /// <summary>
        /// 发送回复信息
        /// </summary>
        SendAck = 0B0010,

        /// <summary>
        /// 发送回复信息，同时向对方服务器注册
        /// </summary>
        SendAckAndRegisterToPeer = PeerRegister | SendAck,
        */

        /// <summary>
        /// 业务层的消息
        /// </summary>
        /// 所有大于 0 的都是业务层消息
        Business = 1,

        /// <summary>
        /// 请求信息，这也是业务层消息
        /// </summary>
        RequestMessage = 1 << 1 | Business,

        /// <summary>
        /// 回应信息，这也是业务层消息
        /// </summary>
        ResponseMessage = 1 << 2 | Business,

        /// <summary>
        /// 其他信息
        /// </summary>
        Unknown = short.MaxValue,
    }
}
