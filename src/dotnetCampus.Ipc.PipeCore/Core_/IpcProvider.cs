using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using dotnetCampus.Ipc.PipeCore.Context;

namespace dotnetCampus.Ipc.PipeCore
{
    /// <summary>
    ///     对等通讯，每个都是服务器端，每个都是客户端
    /// </summary>
    /// 这是这个程序集最顶层的类
    public class IpcProvider
    {
        /// <summary>
        /// 创建对等通讯
        /// </summary>
        public IpcProvider() : this(Guid.NewGuid().ToString("N"))
        {
        }

        /// <summary>
        /// 创建对等通讯
        /// </summary>
        /// <param name="pipeName">本地服务名，将作为管道名，管道服务端名</param>
        public IpcProvider(string pipeName)
        {
            IpcContext = new IpcContext(this, pipeName);
        }

        private IpcContext IpcContext { get; }

        /// <summary>
        /// 开启的管道服务端，用于接收消息
        /// </summary>
        public IpcServerService IpcServerService { private set; get; } = null!;

        internal ConcurrentDictionary<string, IpcClientService> ConnectedServerManagerList { get; } =
            new ConcurrentDictionary<string, IpcClientService>();

        /// <summary>
        /// 启动服务，启动之后将可以被连接
        /// </summary>
        /// <returns></returns>
        public async Task StartServer()
        {
            if (IpcServerService != null) return;

            var ipcServerService = new IpcServerService(IpcContext);
            IpcServerService = ipcServerService;

            ipcServerService.PeerConnected += NamedPipeServerStreamPoolPeerConnected;
            ipcServerService.MessageReceived += NamedPipeServerStreamPool_MessageReceived;

            await ipcServerService.Start();
        }

        private void NamedPipeServerStreamPool_MessageReceived(object? sender, PeerMessageArgs e)
        {

        }


        private async void NamedPipeServerStreamPoolPeerConnected(object? sender, PeerConnectedArgs e)
        {
            // 也许是服务器连接
            if (ConnectedServerManagerList.TryGetValue(e.PeerName, out _))
            {
            }
            else
            {
                // 其他客户端连接
                await ConnectPeer(e.PeerName);
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