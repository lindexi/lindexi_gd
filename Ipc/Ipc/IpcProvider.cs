using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace Ipc
{
    /// <summary>
    /// 对等通讯，每个都是服务器端，每个都是客户端
    /// </summary>
    public class IpcProvider
    {
        public IpcProvider() : this(Guid.NewGuid().ToString("N"))
        {

        }

        public IpcProvider(string clientName)
        {
            ClientName = clientName;
        }

        public async Task StartServer()
        {
            if (IpcServerService != null)
            {
                return;
            }

            var ipcServerService = new IpcServerService(ClientName);
            IpcServerService = ipcServerService;

            ipcServerService.NamedPipeServerStreamPool.ClientConnected += NamedPipeServerStreamPool_ClientConnected;

            await ipcServerService.Start();
        }

        private async void NamedPipeServerStreamPool_ClientConnected(object? sender, ClientConnectedArgs e)
        {
            // 也许是服务器连接
            if (ConnectedServerManagerList.Any(temp => temp.ServerName == e.ClientName))
            {

            }
            else
            {
                // 其他客户端连接
                await ConnectServer(e.ClientName);
            }
        }

        public IpcServerService IpcServerService { set; get; } = null!;

        public async Task ConnectServer(string serverName)
        {
            var task = StartServer();

            var connectedServerManager = new ConnectedServerManager(serverName, ClientName);

            ConnectedServerManagerList.Add(connectedServerManager);

            await connectedServerManager.ConnectServer();

            await task;
        }

        public string ClientName { get; }

        public ConcurrentBag<ConnectedServerManager> ConnectedServerManagerList { get; } = new ConcurrentBag<ConnectedServerManager>();
    }
}