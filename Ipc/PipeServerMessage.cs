using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace Ipc
{
    // 提供一个客户端连接
    class PipeServerMessage
    {
        public PipeServerMessage(string pipeName, IpcContext ipcContext)
        {
            PipeName = pipeName;
            IpcContext = ipcContext;
        }

        private NamedPipeServerStream NamedPipeServerStream { set; get; } = null!;

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
                var (success, ipcMessageContext) = await IpcMessageConverter.ReadAsync(NamedPipeServerStream, IpcConfiguration.MessageHeader,
                    IpcConfiguration.SharedArrayPool);
                if (success)
                {
                    var stream = new ByteListMessageStream(ipcMessageContext);

                    var streamReader = new StreamReader(stream);
                    // ReSharper disable once MethodHasAsyncOverload
                    var clientName = streamReader.ReadToEnd();
                    ClientName = clientName;

                    OnClientConnected(new ClientConnectedArgs(clientName, NamedPipeServerStream));
                    await SendAck(ipcMessageContext.Ack);

                    break;
                }
            }

            while (true)
            {
                var (success, ipcMessageContext) = await IpcMessageConverter.ReadAsync(NamedPipeServerStream, IpcConfiguration.MessageHeader,
                    IpcConfiguration.SharedArrayPool);

                if (success)
                {
                    var stream = new ByteListMessageStream(ipcMessageContext);

                    if (IpcContext.AckManager.IsAckMessage(stream, out var ack))
                    {
                        OnAckReceived(new AckArgs(ClientName, ack));
                    }
                    else
                    {
                        var task = SendAck(ack);
                        OnMessageReceived(new ClientMessageArgs(ClientName, stream));
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

        public event EventHandler<ClientMessageArgs>? MessageReceived;

        public event EventHandler<ClientConnectedArgs>? ClientConnected;

        public event EventHandler<AckArgs>? AckReceived;

        private string ClientName { set; get; } = null!;

        public string PipeName { get; }
        public IpcContext IpcContext { get; }

        private IpcConfiguration IpcConfiguration => IpcContext.IpcConfiguration;

        protected virtual void OnClientConnected(ClientConnectedArgs e)
        {
            ClientConnected?.Invoke(this, e);
        }

        protected virtual void OnMessageReceived(ClientMessageArgs e)
        {
            MessageReceived?.Invoke(this, e);
        }

        protected virtual void OnAckReceived(AckArgs e)
        {
            AckReceived?.Invoke(this, e);
        }
    }
}