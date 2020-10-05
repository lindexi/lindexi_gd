using System.Threading.Tasks;

namespace Ipc
{
    public class ConnectedServerManager
    {
        public ConnectedServerManager(string serverName, IpcContext ipcContext)
        {
            ServerName = serverName;
            IpcContext = ipcContext;
        }

        public string ServerName { get; }
        public string PipeName => IpcContext.PipeName;
        public IpcContext IpcContext { get; }

        public IpcClientService IpcClientService { get; set; } = null!;

        public async Task ConnectServer()
        {
            var ipcClientService = new IpcClientService(IpcContext, ServerName);
            IpcClientService = ipcClientService;
            await ipcClientService.Start();
            await ipcClientService.WriteStringAsync(PipeName);
        }
    }
}