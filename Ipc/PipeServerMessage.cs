using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace Ipc
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
                    if (IpcContext.PeerRegisterProvider.TryParsePeerRegisterMessage(stream,out var peerName))
                    {
                        // ReSharper disable once MethodHasAsyncOverload
                        PeerName = peerName;

                        IpcServerService.OnClientConnected(new ClientConnectedArgs(peerName, NamedPipeServerStream));

                        await SendAck(ipcMessageContext.Ack);

                        break;
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
                        IpcServerService.OnAckReceived(new AckArgs(PeerName, ack));
                    }
                    else
                    {
                        var task = SendAck(ack);
                        IpcServerService.OnMessageReceived(new ClientMessageArgs(PeerName, stream));
                        await task;
                    }
                }
            }
        }

        private async Task SendAck(Ack receivedAck)
        {
            var ipcProvider = IpcContext.IpcProvider;
            var ipcClient = await ipcProvider.ConnectPeer(PeerName);
            await ipcClient.SendAck(receivedAck);
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