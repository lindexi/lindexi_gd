using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace Ipc
{
    /// <summary>
    ///     对等通讯，每个都是服务器端，每个都是客户端
    /// </summary>
    public class IpcProvider
    {
        public IpcProvider() : this(Guid.NewGuid().ToString("N"))
        {
        }

        public IpcProvider(string clientName)
        {
            ClientName = clientName;
            IpcContext = new IpcContext(this, clientName);
        }

        private IpcContext IpcContext { get; }

        public IpcServerService IpcServerService { private set; get; } = null!;

        public string ClientName { get; }

        public ConcurrentDictionary<string, ConnectedServerManager> ConnectedServerManagerList { get; } =
            new ConcurrentDictionary<string, ConnectedServerManager>();

        public async Task StartServer()
        {
            if (IpcServerService != null) return;

            var ipcServerService = new IpcServerService( ClientName,IpcContext);
            IpcServerService = ipcServerService;

            ipcServerService.ClientConnected += NamedPipeServerStreamPool_ClientConnected;
            ipcServerService.MessageReceived += NamedPipeServerStreamPool_MessageReceived;
            ipcServerService.AckReceived += IpcContext.AckManager.OnAckReceived;

            await ipcServerService.Start();
        }

        private void NamedPipeServerStreamPool_MessageReceived(object? sender, ClientMessageArgs e)
        {
            
        }


        private async void NamedPipeServerStreamPool_ClientConnected(object? sender, ClientConnectedArgs e)
        {
            // 也许是服务器连接
            if (ConnectedServerManagerList.TryGetValue(e.ClientName, out _))
            {
            }
            else
            {
                // 其他客户端连接
                await ConnectServer(e.ClientName); 
            }
        }

        public async Task<IpcClientService> ConnectServer(string serverName)
        {
            if (ConnectedServerManagerList.TryGetValue(serverName, out var connectedServerManager))
            {
            }
            else
            {
                // 这里无视多次加入，这里的多线程问题也可以忽略

                var task = StartServer();

                connectedServerManager = new ConnectedServerManager(serverName, ClientName, IpcContext);

                if (ConnectedServerManagerList.TryAdd(serverName, connectedServerManager))
                {
                }

                await connectedServerManager.ConnectServer();

                await task;
            }

            return connectedServerManager.IpcClientService;
        }
    }
}