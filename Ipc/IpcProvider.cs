using System;
using System.Collections.Concurrent;
using System.IO;
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

        private IpcContext IpcContext { get; } = new IpcContext();

        public async Task StartServer()
        {
            if (IpcServerService != null)
            {
                return;
            }

            var ipcServerService = new IpcServerService(ClientName);
            IpcServerService = ipcServerService;

            ipcServerService.NamedPipeServerStreamPool.ClientConnected += NamedPipeServerStreamPool_ClientConnected;
            ipcServerService.NamedPipeServerStreamPool.MessageReceived += NamedPipeServerStreamPool_MessageReceived;

            await ipcServerService.Start();
        }

        private void NamedPipeServerStreamPool_MessageReceived(object? sender, ClientMessageArgs e)
        {
            // 使用 e.ClientName 可以回复消息

            // 是否回复 ack 命令
            var ack = new BinaryReader(e.Stream).ReadUInt64();

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

        public IpcServerService IpcServerService { set; get; } = null!;

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
                else
                {
                    // 连接过
                    // 是否更新
                }

                await connectedServerManager.ConnectServer();

                await task;
            }

            return connectedServerManager.IpcClientService;
        }

        public string ClientName { get; }

        public ConcurrentDictionary<string, ConnectedServerManager> ConnectedServerManagerList { get; } = new ConcurrentDictionary<string, ConnectedServerManager>();
    }
}