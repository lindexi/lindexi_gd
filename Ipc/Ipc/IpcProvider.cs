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

        public IpcProvider(string pipeName)
        {
            IpcContext = new IpcContext(this, pipeName);
        }

        private IpcContext IpcContext { get; }

        public IpcServerService IpcServerService { private set; get; } = null!;

        public string PipeName => IpcContext.PipeName;

        public ConcurrentDictionary<string, IpcClientService> ConnectedServerManagerList { get; } =
            new ConcurrentDictionary<string, IpcClientService>();

        public async Task StartServer()
        {
            if (IpcServerService != null) return;

            var ipcServerService = new IpcServerService(IpcContext);
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
                await ConnectPeer(e.ClientName);
            }
        }

        /// <summary>
        /// 连接其他客户端
        /// </summary>
        /// <param name="peerName">对方</param>
        /// <returns></returns>
        public async Task<IpcClientService> ConnectPeer(string peerName)
        {
            if (ConnectedServerManagerList.TryGetValue(peerName, out var ipcClientService))
            {
            }
            else
            {
                // 这里无视多次加入，这里的多线程问题也可以忽略

                var task = StartServer();

                ipcClientService = new IpcClientService(IpcContext, peerName);

                if (ConnectedServerManagerList.TryAdd(peerName, ipcClientService))
                {
                }

                await ipcClientService.Start();

                await task;
            }

            return ipcClientService;
        }
    }
}