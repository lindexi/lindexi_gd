using System.Threading.Tasks;

namespace Ipc
{
    public class ConnectedServerManager
    {
        public ConnectedServerManager(string serverName, string clientName)
        {
            ServerName = serverName;
            ClientName = clientName;
        }

        public async Task ConnectServer()
        {
            var ipcClientService = new IpcClientService(ServerName);
            IpcClientService = ipcClientService;
            await ipcClientService.Start();
            await ipcClientService.WriteStringAsync(ClientName);
        }

        public string ServerName { get; }
        public string ClientName { get; }

        public IpcClientService IpcClientService { get; set; } = null!;

    }
}