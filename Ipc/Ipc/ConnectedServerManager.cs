using System.Threading.Tasks;

namespace Ipc
{
    public class ConnectedServerManager
    {
        /// <summary>
        /// 连接管理
        /// </summary>
        /// <param name="peerName"></param>
        /// <param name="ipcContext"></param>
        public ConnectedServerManager(string peerName, IpcContext ipcContext)
        {
            PeerName = peerName;
            IpcContext = ipcContext;
        }

        public string PeerName { get; }
        public string PipeName => IpcContext.PipeName;
        public IpcContext IpcContext { get; }

        public IpcClientService IpcClientService { get; set; } = null!;

        public async Task ConnectServer()
        {
            var ipcClientService = new IpcClientService(IpcContext, PeerName);
            IpcClientService = ipcClientService;
            await ipcClientService.Start();
            await ipcClientService.WriteStringAsync(PipeName);
        }
    }
}