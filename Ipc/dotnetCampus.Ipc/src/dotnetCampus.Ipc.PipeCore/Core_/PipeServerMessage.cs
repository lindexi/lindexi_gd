using System.IO.Pipes;
using System.Threading.Tasks;
using dotnetCampus.Ipc.PipeCore.Context;
using dotnetCampus.Ipc.PipeCore.Utils;

namespace dotnetCampus.Ipc.PipeCore
{
    // 提供一个客户端连接
    internal class PipeServerMessage
    {
        public PipeServerMessage(IpcContext ipcContext, IpcServerService ipcServerService)
        {
            IpcContext = ipcContext;
            IpcServerService = ipcServerService;
        }

        private NamedPipeServerStream NamedPipeServerStream { set; get; } = null!;

        /// <summary>
        /// 被对方连接
        /// </summary>
        private string PeerName { set; get; } = null!;

        /// <summary>
        /// 自身的名字
        /// </summary>
        public string PipeName => IpcContext.PipeName;
        public IpcContext IpcContext { get; }
        public IpcServerService IpcServerService { get; }

        private IpcConfiguration IpcConfiguration => IpcContext.IpcConfiguration;

        public async Task Start()
        {
            var namedPipeServerStream = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 250);
            NamedPipeServerStream = namedPipeServerStream;

            await namedPipeServerStream.WaitForConnectionAsync();

            //var streamMessageConverter = new StreamMessageConverter(namedPipeServerStream,
            //    IpcConfiguration.MessageHeader, IpcConfiguration.SharedArrayPool);
            //streamMessageConverter.MessageReceived += OnClientConnectReceived;
            //StreamMessageConverter = streamMessageConverter;
            //streamMessageConverter.Start();

            Read();
        }

        private async void Read()
        {
            while (true)
            {
                var (success, ipcMessageContext) = await IpcMessageConverter.ReadAsync(NamedPipeServerStream,
                    IpcConfiguration.MessageHeader,
                    IpcConfiguration.SharedArrayPool);
                if (success)
                {
                    var stream = new ByteListMessageStream(ipcMessageContext);
                    if (IpcContext.PeerRegisterProvider.TryParsePeerRegisterMessage(stream, out var peerName))
                    {
                        // ReSharper disable once MethodHasAsyncOverload
                        PeerName = peerName;

                        IpcServerService.OnPeerConnected(new PeerConnectedArgs(peerName, NamedPipeServerStream, ipcMessageContext.Ack));

                        SendAck(ipcMessageContext.Ack);
                        // 不等待对方收到，因为对方也在等待
                        //await SendAckAsync(ipcMessageContext.Ack);

                        break;
                    }
                    else if (IpcContext.AckManager.IsAckMessage(stream, out var ack))
                    {
                        // 只有作为去连接对方的时候，才会收到这个消息
                        IpcContext.Logger.Debug($"[{nameof(IpcServerService)}] AckReceived {ack} From {PeerName}");
                        IpcContext.AckManager.OnAckReceived(this, new AckArgs(PeerName, ack));
                    }
                    else
                    {
                        // 后续需要要求重发设备名
                    }
                }
            }

            while (true)
            {
                var (success, ipcMessageContext) = await IpcMessageConverter.ReadAsync(NamedPipeServerStream,
                    IpcConfiguration.MessageHeader,
                    IpcConfiguration.SharedArrayPool);

                if (success)
                {
                    var stream = new ByteListMessageStream(ipcMessageContext);

                    if (IpcContext.AckManager.IsAckMessage(stream, out var ack))
                    {
                        IpcContext.Logger.Debug($"[{nameof(IpcServerService)}] AckReceived {ack} From {PeerName}");
                        IpcContext.AckManager.OnAckReceived(this, new AckArgs(PeerName, ack));
                    }
                    else
                    {
                        SendAck(ack);
                        IpcServerService.OnMessageReceived(new PeerMessageArgs(PeerName, stream, ack));
                    }
                }
            }
        }

        private async void SendAck(Ack receivedAck) => await SendAckAsync(receivedAck);

        private async Task SendAckAsync(Ack receivedAck)
        {
            IpcContext.Logger.Debug($"[{nameof(IpcServerService)}] SendAck {receivedAck} to {PeerName}");
            var ipcProvider = IpcContext.IpcProvider;
            var ipcClient = await ipcProvider.ConnectPeer(PeerName);
            await ipcClient.SendAckAsync(receivedAck);
        }

        //private StreamMessageConverter StreamMessageConverter { set; get; } = null!;

        //private void OnClientConnectReceived(object? sender, ByteListMessageStream stream)
        //{


        //    StreamMessageConverter.MessageReceived -= OnClientConnectReceived;
        //    StreamMessageConverter.MessageReceived += StreamMessageConverter_MessageReceived;
        //}

        //private void StreamMessageConverter_MessageReceived(object? sender, ByteListMessageStream e)
        //{
        //}

    }
}