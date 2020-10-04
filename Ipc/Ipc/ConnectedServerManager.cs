using System.Threading.Tasks;

namespace Ipc
{
    public class ConnectedServerManager
    {
        public ConnectedServerManager(string serverName, string clientName, IpcContext ipcContext)
        {
            ServerName = serverName;
            ClientName = clientName;
            IpcContext = ipcContext;
        }

        public string ServerName { get; }
        public string ClientName { get; }
        public IpcContext IpcContext { get; }

        public IpcClientService IpcClientService { get; set; } = null!;

        public async Task ConnectServer()
        {
            var ipcClientService = new IpcClientService(IpcContext, ServerName);
            IpcClientService = ipcClientService;
            await ipcClientService.Start();
            await ipcClientService.WriteStringAsync(ClientName);
        }
    }
}