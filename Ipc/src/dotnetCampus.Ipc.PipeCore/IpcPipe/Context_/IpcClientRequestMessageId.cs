namespace dotnetCampus.Ipc.PipeCore.IpcPipe
{
    /// <summary>
    /// 客户端请求的信息号
    /// </summary>
    /// 通过信息号可以用来在服务器端返回，让客户端知道服务器端返回的响应是对应那个信息
    public readonly struct IpcClientRequestMessageId
    {
        /// <summary>
        /// 创建信息号
        /// </summary>
        /// <param name="messageIdValue"></param>
        public IpcClientRequestMessageId(ulong messageIdValue)
        {
            MessageIdValue = messageIdValue;
        }

        /// <summary>
        /// 信息号的值
        /// </summary>
        public ulong MessageIdValue { get; }
    }
}
