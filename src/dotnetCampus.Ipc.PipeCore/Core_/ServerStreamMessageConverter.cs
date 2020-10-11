using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using dotnetCampus.Ipc.PipeCore.Context;
using dotnetCampus.Ipc.PipeCore.Utils;

namespace dotnetCampus.Ipc.PipeCore
{
    /// <summary>
    /// 基础的数据读取
    /// </summary>
    class ServerStreamMessageConverter
    {
        public ServerStreamMessageConverter(IpcContext ipcContext, Stream stream)
        {
            IpcContext = ipcContext;
            Stream = stream;
        }

        private IpcConfiguration IpcConfiguration => IpcContext.IpcConfiguration;
        public IpcContext IpcContext { get; }

        /// <summary>
        /// 被对方连接
        /// </summary>
        public string PeerName { set; get; } = null!;

        public async void Run()
        {
            await WaitForConnectionAsync();

            await ReadMessageAsync();
        }

        private async Task ReadMessageAsync()
        {
            while (true)
            {
                var (success, ipcMessageContext) = await IpcMessageConverter.ReadAsync(Stream,
                    IpcConfiguration.MessageHeader,
                    IpcConfiguration.SharedArrayPool);

                if (success)
                {
                    var stream = new ByteListMessageStream(ipcMessageContext);

                    if (IpcContext.AckManager.IsAckMessage(stream, out var ack))
                    {
                        IpcContext.Logger.Debug($"[{nameof(IpcServerService)}] AckReceived {ack} From {PeerName}");
                        IpcContext.AckManager.OnAckReceived(this, new AckArgs(PeerName, ack));
                        // 如果是收到 ack 回复了，那么只需要向 AckManager 注册
                        Debug.Assert(ipcMessageContext.Ack.Value == IpcContext.AckUsedForReply.Value);
                    }
                    else
                    {
                        ack = ipcMessageContext.Ack;
                        OnAckRequested(ack);
                        OnMessageReceived(new PeerMessageArgs(PeerName, stream, ack));
                    }
                }
            }
        }

        private async Task WaitForConnectionAsync()
        {
            while (true)
            {
                var (success, ipcMessageContext) = await IpcMessageConverter.ReadAsync(Stream,
                    IpcConfiguration.MessageHeader,
                    IpcConfiguration.SharedArrayPool);
                if (success)
                {
                    var stream = new ByteListMessageStream(ipcMessageContext);

                    var isPeerRegisterMessage =
                        IpcContext.PeerRegisterProvider.TryParsePeerRegisterMessage(stream, out var peerName);

                    if (isPeerRegisterMessage)
                    {
                        // ReSharper disable once MethodHasAsyncOverload
                        PeerName = peerName;

                        OnPeerConnected(new PeerConnectedArgs(peerName, Stream, ipcMessageContext.Ack));

                        //SendAckAndRegisterToPeer(ipcMessageContext.Ack);
                        //SendAck(ipcMessageContext.Ack);
                        //// 不等待对方收到，因为对方也在等待
                        ////await SendAckAsync(ipcMessageContext.Ack);
                    }

                    // 如果是 对方的注册消息 同时也许是回应的消息，所以不能加上 else if 判断
                    if (IpcContext.AckManager.IsAckMessage(stream, out var ack))
                    {
                        // 只有作为去连接对方的时候，才会收到这个消息
                        IpcContext.Logger.Debug($"[{nameof(IpcServerService)}] AckReceived {ack} From {PeerName}");
                        IpcContext.AckManager.OnAckReceived(this, new AckArgs(PeerName, ack));

                        if (isPeerRegisterMessage)
                        {
                            // 这是一条本地主动去连接对方，然后收到对方的反过来的连接的信息，此时需要回复对方
                            // 参阅 SendAckAndRegisterToPeer 方法的实现
                            //SendAck(ipcMessageContext.Ack);
                            OnAckRequested(ipcMessageContext.Ack);
                        }
                    }
                    else
                    {
                        // 后续需要要求重发设备名
                    }

                    if (isPeerRegisterMessage)
                    {
                        // 收到注册消息了
                        break;
                    }
                }
            }
        }

        private Stream Stream { get; }

        /// <summary>
        /// 请求发送回复的 ack 消息
        /// </summary>
        internal event EventHandler<Ack>? AckRequested;

        /// <summary>
        /// 当收到对方确定收到消息时触发
        /// </summary>
        public event EventHandler<AckArgs>? AckReceived;

        /// <summary>
        /// 当收到消息时触发
        /// </summary>
        public event EventHandler<PeerMessageArgs>? MessageReceived;

        /// <summary>
        /// 当有对方连接时触发
        /// </summary>
        public event EventHandler<PeerConnectedArgs>? PeerConnected;

        private void OnAckReceived(AckArgs e)
        {
            AckReceived?.Invoke(this, e);
        }

        private void OnMessageReceived(PeerMessageArgs e)
        {
            MessageReceived?.Invoke(this, e);
        }

        private void OnPeerConnected(PeerConnectedArgs e)
        {
            PeerConnected?.Invoke(this, e);
        }

        private void OnAckRequested(in Ack e)
        {
            AckRequested?.Invoke(this, e);
        }
    }
}