namespace dotnetCampus.Ipc.Abstractions
{
    /// <summary>
    /// 处理客户端请求的结果
    /// </summary>
    public interface IIpcHandleRequestMessageResult
    {
        /// <summary>
        /// 返回给对方的信息
        /// </summary>
        IpcRequestMessage ReturnMessage { get; }
    }
}
