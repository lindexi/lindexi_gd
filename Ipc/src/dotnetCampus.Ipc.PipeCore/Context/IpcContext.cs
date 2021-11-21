using dotnetCampus.Ipc.PipeCore.IpcPipe;

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
        /// <param name="ipcConfiguration"></param>
        public IpcContext(IpcProvider ipcProvider, string pipeName, IpcConfiguration? ipcConfiguration = null)
        {
            IpcProvider = ipcProvider;
            PipeName = pipeName;

            AckManager = new AckManager();
            IpcRequestHandlerProvider = new IpcRequestHandlerProvider(this);

            IpcConfiguration = ipcConfiguration ?? new IpcConfiguration();

            Logger = new IpcLogger(this);
        }

        internal AckManager AckManager { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"[{PipeName}]";
        }

        internal IpcConfiguration IpcConfiguration { get; }

        internal IpcProvider IpcProvider { get; }

        internal IpcRequestHandlerProvider IpcRequestHandlerProvider { get; }

        internal IpcMessageResponseManager IpcMessageResponseManager { get; } = new IpcMessageResponseManager();

        /// <summary>
        /// 管道名，本地服务器名
        /// </summary>
        public string PipeName { get; }

        internal PeerRegisterProvider PeerRegisterProvider { get; } = new PeerRegisterProvider();

        internal ILogger Logger { get; }

        /// <summary>
        /// 规定回应 ack 的值使用的 ack 是最大值
        /// </summary>
        internal Ack AckUsedForReply { get; } = new Ack(ulong.MaxValue);
    }
}
