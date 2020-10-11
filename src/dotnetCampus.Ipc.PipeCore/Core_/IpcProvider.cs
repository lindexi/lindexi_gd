using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using dotnetCampus.Ipc.PipeCore.Context;

namespace dotnetCampus.Ipc.PipeCore
{
    /// <summary>
    ///     对等通讯，每个都是服务器端，每个都是客户端
    /// </summary>
    /// 这是这个程序集最顶层的类
    /// 这里有两个概念，一个是对方，另一个是本地
    /// 对方就是其他的开启的Ipc服务的端，可以在相同的进程内。而本地是指此Ipc服务
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
        /// 启动服务，启动之后将可以被对方连接
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

        /// <summary>
        /// 对方连接过来的时候，需要反过来连接对方的服务器端
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void NamedPipeServerStreamPoolPeerConnected(object? sender, PeerConnectedArgs e)
        {
            // 也许是服务器连接
            if (ConnectedServerManagerList.TryGetValue(e.PeerName, out _))
            {
            }
            else
            {
                // 其他客户端连接，需要反过来连接对方的服务器端
                await ConnectBackToPeer(e.PeerName, e.Ack);
            }
        }

        private async Task ConnectBackToPeer(string peerName, Ack ack)
        {
            if (ConnectedServerManagerList.TryGetValue(peerName, out _))
            {
                // 预期不会进入此分支，也就是之前没有连接过才对
                Debug.Assert(false, "对方连接之前没有记录对方");
            }
            else
            {
                // 无须再次启动本地的服务器端，因为有对方连接过来，此时一定开启了本地的服务器端
                var ipcClientService = new IpcClientService(IpcContext, peerName);
                if (ConnectedServerManagerList.TryAdd(peerName, ipcClientService))
                {
                    // 理论上会进入此分支，除非是此时收到了多次的发送
                }
                else
                {
                    // 后续需要处理，并发收到对方的多次连接
                    Debug.Assert(false, "对方的连接并发进入，此时也许会存在多次重复连接对方的服务器端");
                }

                // 此时不需要向对方注册，因为对方知道本地的存在，是对方主动连接本地
                var shouldRegisterToPeer = false;
                await ipcClientService.Start(shouldRegisterToPeer: shouldRegisterToPeer);
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