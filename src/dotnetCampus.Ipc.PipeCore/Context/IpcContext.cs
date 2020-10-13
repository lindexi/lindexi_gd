namespace dotnetCampus.Ipc.PipeCore.Context
{
    /// <summary>
    /// 用于作为 Ipc 库的上下文，包括各个过程需要使用的工具和配置等
    /// </summary>
    public class IpcContext
    {
        /// <summary>
        /// 默认的管道名
        /// </summary>
        public const string DefaultPipeName = "dotnet campus";

        /// <summary>
        /// 创建上下文
        /// </summary>
        /// <param name="ipcProvider"></param>
        /// <param name="pipeName">管道名，也将被做来作为服务器名或当前服务名</param>
        public IpcContext(IpcProvider ipcProvider, string pipeName)
        {
            IpcProvider = ipcProvider;
            PipeName = pipeName;

            AckManager = new AckManager(this);
        }

        internal AckManager AckManager { get; }

        internal IpcConfiguration IpcConfiguration { get; set; } = new IpcConfiguration();

        internal IpcProvider IpcProvider { get; }

        /// <summary>
        /// 管道名，本地服务器名
        /// </summary>
        public string PipeName { get; }

        internal PeerRegisterProvider PeerRegisterProvider { get; } = new PeerRegisterProvider();

        internal ILogger Logger { get; } = null!;

        /// <summary>
        /// 规定回应 ack 的值使用的 ack 是最大值
        /// </summary>
        internal Ack AckUsedForReply { get; } = new Ack(ulong.MaxValue);
    }
}
