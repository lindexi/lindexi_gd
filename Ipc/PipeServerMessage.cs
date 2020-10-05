using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace Ipc
{
    // 提供一个客户端连接
    internal class PipeServerMessage
    {
        public PipeServerMessage(string pipeName, IpcContext ipcContext, IpcServerService ipcServerService)
        {
            PipeName = pipeName;
            IpcContext = ipcContext;
            IpcServerService = ipcServerService;
        }

        private NamedPipeServerStream NamedPipeServerStream { set; get; } = null!;

        private string ClientName { set; get; } = null!;

        public string PipeName { get; }
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

                    var streamReader = new StreamReader(stream);
                    // ReSharper disable once MethodHasAsyncOverload
                    var clientName = streamReader.ReadToEnd();
                    ClientName = clientName;

                    IpcServerService.OnClientConnected(new ClientConnectedArgs(clientName, NamedPipeServerStream));
                    await SendAck(ipcMessageContext.Ack);

                    break;
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
                        IpcServerService.OnAckReceived(new AckArgs(ClientName, ack));
                    }
                    else
                    {
                        var task = SendAck(ack);
                        IpcServerService.OnMessageReceived(new ClientMessageArgs(ClientName, stream));
                        await task;
                    }
                }
            }
        }

        private async Task SendAck(Ack receivedAck)
        {
            var ipcProvider = IpcContext.IpcProvider;
            var ipcClient = await ipcProvider.ConnectServer(ClientName);
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