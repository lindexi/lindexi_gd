using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using dotnetCampus.Ipc.Abstractions.Context;
using dotnetCampus.Ipc.PipeCore.Context;
using dotnetCampus.Ipc.PipeCore.Utils;

namespace dotnetCampus.Ipc.PipeCore
{
    /// <summary>
    ///     对等通讯，每个都是服务器端，每个都是客户端
    /// </summary>
    /// 这是这个程序集最顶层的类
    /// 这里有两个概念，一个是对方，另一个是本地
    /// 对方就是其他的开启的Ipc服务的端，可以在相同的进程内。而本地是指此Ipc服务
    public class IpcProvider : IDisposable
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
        /// <param name="ipcConfiguration"></param>
        public IpcProvider(string pipeName, IpcConfiguration? ipcConfiguration = null)
        {
            IpcContext = new IpcContext(this, pipeName, ipcConfiguration);
            IpcContext.Logger.Debug($"[IpcProvider] 本地服务名 {pipeName}");
        }

        private IpcContext IpcContext { get; }
        private PeerManager PeerManager { get; } = new PeerManager();

        /// <summary>
        /// 开启的管道服务端，用于接收消息
        /// </summary>
        public IpcServerService IpcServerService
        {
            get => _ipcServerService!;
        }

        /// <summary>
        /// 启动服务，启动之后将可以被对方连接
        /// </summary>
        /// <returns></returns>
        public async void StartServer()
        {
            if (_ipcServerService != null) return;

            var ipcServerService = new IpcServerService(IpcContext);
            _ipcServerService = ipcServerService;

            ipcServerService.PeerConnected += NamedPipeServerStreamPoolPeerConnected;

            // 以下的 Start 是一个循环，不会返回的
            await ipcServerService.Start();
        }

        /// <summary>
        /// 对方连接过来的时候，需要反过来连接对方的服务器端
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void NamedPipeServerStreamPoolPeerConnected(object? sender, IpcInternalPeerConnectedArgs e)
        {
            // 也许是对方反过来连接
            if (PeerManager.ConnectedServerManagerList.TryGetValue(e.PeerName, out var peerProxy))
            {
                if (peerProxy.IsBroken)
                {
                    // 理论上不会进入这个分支，因为如果 IsBroken 将会自动去清理，除非刚好一个断开，然后立刻连接
                    PeerManager.RemovePeerProxy(peerProxy);
                    await ConnectBackToPeer(e);
                }
                else
                {
                    peerProxy.Update(e);
                }
            }
            else
            {
                // 其他客户端连接，需要反过来连接对方的服务器端
                await ConnectBackToPeer(e);
            }
        }

        private async Task ConnectBackToPeer(IpcInternalPeerConnectedArgs e)
        {
            var peerName = e.PeerName;
            //var receivedAck = e.Ack;

            if (PeerManager.ConnectedServerManagerList.TryGetValue(peerName, out _))
            {
                // 预期不会进入此分支，也就是之前没有连接过才对
                Debug.Assert(false, "对方连接之前没有记录对方");
            }
            else
            {
                // 无须再次启动本地的服务器端，因为有对方连接过来，此时一定开启了本地的服务器端
                var ipcClientService = new IpcClientService(IpcContext, peerName);

                // 此时向对方注册，让对方重新触发逻辑
                var shouldRegisterToPeer = true;
                var task = ipcClientService.Start(shouldRegisterToPeer: shouldRegisterToPeer);

                // 此时就建立完成了链接
                CreatePeerProxy(ipcClientService);
                // 先建立链接再继续发送注册，解决多进程同时注册
                await task;

                /*
                SendAckAndRegisterToPeer();

                // 发送 ack 同时注册自身
                async void SendAckAndRegisterToPeer()
                {
                    IpcContext.Logger.Debug($"[{nameof(SendAckAndRegisterToPeer)}] Start SendAckAndRegisterToPeer");
                    var ackMessage = IpcContext.AckManager.BuildAckMessage(receivedAck);
                    var peerRegisterMessage =
                        IpcContext.PeerRegisterProvider.BuildPeerRegisterMessage(IpcContext.PipeName);
                    const string summary = nameof(SendAckAndRegisterToPeer);

                    // 消息的顺序是有要求的，先发送注册消息，然后加上回复 Ack 的消息
                    // 在收到对方的连接的时候，需要去连接对方，而在连接的时候需要有两个步骤
                    // 1. 回复对方的连接消息，需要发送 Ack 回复
                    // 2. 连接对方，需要发送注册消息
                    // 以下将上面两个步骤合并为一条消息，这一条消息包含了注册消息和 Ack 回复的消息
                    // 为什么注册消息在前面，而回复 Ack 在后面？原因是为了在解析的时候，可以先了解是哪个服务进行连接
                    // 而且回复 Ack 需要两个信息，一个是 Ack 的值，另一个是连接的设备名。因此让注册消息在前面就能
                    // 先读取设备名，用于后续回复 Ack 了解是哪个设备回复
                    var ackAndPeerRegisterMessage =
                        peerRegisterMessage.BuildWithCombine(summary, IpcMessageCommandType.SendAckAndRegisterToPeer,
                            mergeBefore: false,
                            new IpcBufferMessage(ackMessage));
                    await ipcClientService.WriteMessageAsync(ackAndPeerRegisterMessage);

                    // 此时就建立完成了链接
                    CreatePeerProxy(ipcClientService);
                }
                */

            }

            void CreatePeerProxy(IpcClientService ipcClientService)
            {
                var peerProxy = new PeerProxy(e.PeerName, ipcClientService, e, IpcContext);

                if (PeerManager.TryAdd(peerProxy))
                {
                    // 理论上会进入此分支，除非是此时收到了多次的发送
                }
                else
                {
                    // 后续需要处理，并发收到对方的多次连接
                    Debug.Assert(false, "对方的连接并发进入，此时也许会存在多次重复连接对方的服务器端");
                }

                // 通知有其他客户端连接过来
                PeerConnected?.Invoke(this, new PeerConnectedArgs(peerProxy));
            }
        }

        /// <summary>
        /// 本机作为服务端，有对方连接过来时触发
        /// </summary>
        public event EventHandler<PeerConnectedArgs>? PeerConnected;

        /// <summary>
        /// 获取一个连接到指定 <paramref name="peerName"/> 的客户端。如没有连接过，则需要等待连接。如已建立连接则不需要重新建立连接
        /// </summary>
        /// <param name="peerName">对方</param>
        /// <returns></returns>
        public async Task<PeerProxy> GetAndConnectToPeerAsync(string peerName)
        {
            var peerProxy = await GetOrCreatePeerProxyAsync(peerName);

            await PeerManager.WaitForPeerConnectFinishedAsync(peerProxy);

            return peerProxy;
        }

        /// <summary>
        /// 连接其他客户端
        /// </summary>
        /// <param name="peerName">对方</param>
        /// <returns></returns>
        internal async Task<PeerProxy> GetOrCreatePeerProxyAsync(string peerName)
        {
            if (PeerManager.ConnectedServerManagerList.TryGetValue(peerName, out var peerProxy))
            {
            }
            else
            {
                // 这里无视多次加入，这里的多线程问题也可以忽略
                StartServer();

                var ipcClientService = new IpcClientService(IpcContext, peerName);

                peerProxy = new PeerProxy(peerName, ipcClientService, IpcContext);
                PeerManager.TryAdd(peerProxy);

                await ipcClientService.Start();
            }

            return peerProxy;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            IpcServerService.Dispose();
            PeerManager.Dispose();
        }

        private IpcServerService? _ipcServerService;
    }
}
