using System.IO.Pipes;
using System.Threading.Tasks;
using dotnetCampus.Ipc.PipeCore.Context;

namespace dotnetCampus.Ipc.PipeCore
{
    // 提供一个客户端连接
    internal class IpcPipeServerMessageProvider
    {
        public IpcPipeServerMessageProvider(IpcContext ipcContext, IpcServerService ipcServerService)
        {
            IpcContext = ipcContext;
            IpcServerService = ipcServerService;
        }

        private NamedPipeServerStream NamedPipeServerStream { set; get; } = null!;

        /// <summary>
        /// 被对方连接
        /// </summary>
        private string PeerName => ServerStreamMessageReader.PeerName;

        /// <summary>
        /// 自身的名字
        /// </summary>
        public string PipeName => IpcContext.PipeName;
        public IpcContext IpcContext { get; }
        public IpcServerService IpcServerService { get; }


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

            var serverStreamMessageConverter = new ServerStreamMessageReader(IpcContext, NamedPipeServerStream);
            ServerStreamMessageReader = serverStreamMessageConverter;

            serverStreamMessageConverter.AckRequested += ServerStreamMessageConverter_AckRequested;
            serverStreamMessageConverter.AckReceived += IpcContext.AckManager.OnAckReceived;
            serverStreamMessageConverter.PeerConnected += IpcServerService.OnPeerConnected;
            serverStreamMessageConverter.MessageReceived += IpcServerService.OnMessageReceived;

            serverStreamMessageConverter.Run();
        }

        private void ServerStreamMessageConverter_AckRequested(object? sender, Ack e)
        {
            SendAck(e);
        }

        private ServerStreamMessageReader ServerStreamMessageReader { set; get; } = null!;

        private async void SendAck(Ack receivedAck) => await SendAckAsync(receivedAck);

        private async Task SendAckAsync(Ack receivedAck)
        {
            IpcContext.Logger.Debug($"[{nameof(IpcServerService)}] SendAck {receivedAck} to {PeerName}");
            var ipcProvider = IpcContext.IpcProvider;
            var ipcClient = await ipcProvider.ConnectPeer(PeerName);
            await ipcClient.SendAckAsync(receivedAck);
        }
    }
}
