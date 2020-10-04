using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace Ipc
{
    class PipeServerMessage
    {
        public PipeServerMessage(string pipeName)
        {
            PipeName = pipeName;
        }

        private NamedPipeServerStream NamedPipeServerStream { set; get; } = null!;

        public async Task Start()
        {
            var namedPipeServerStream = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 250);
            NamedPipeServerStream = namedPipeServerStream;

            await namedPipeServerStream.WaitForConnectionAsync();

            var streamMessageConverter = new StreamMessageConverter(namedPipeServerStream,
                IpcConfiguration.MessageHeader, IpcConfiguration.SharedArrayPool);
            streamMessageConverter.MessageReceived += OnClientConnectReceived;
            StreamMessageConverter = streamMessageConverter;
            streamMessageConverter.Start();
        }

        private StreamMessageConverter StreamMessageConverter { set; get; } = null!;

        private void OnClientConnectReceived(object? sender, ByteListMessageStream stream)
        {
            var streamReader = new StreamReader(stream);
            var clientName = streamReader.ReadToEnd();
            ClientName = clientName;

            OnClientConnected(new ClientConnectedArgs(clientName, NamedPipeServerStream));

            StreamMessageConverter.MessageReceived -= OnClientConnectReceived;
            StreamMessageConverter.MessageReceived += StreamMessageConverter_MessageReceived;
        }

        private void StreamMessageConverter_MessageReceived(object? sender, ByteListMessageStream e)
        {
            OnMessageReceived(new ClientMessageArgs(ClientName, e));
        }

        public event EventHandler<ClientMessageArgs>? MessageReceived;

        public event EventHandler<ClientConnectedArgs>? ClientConnected;

        private string ClientName { set; get; } = null!;

        public string PipeName { get; }

        private IpcConfiguration IpcConfiguration { get; set; } = new IpcConfiguration();

        protected virtual void OnClientConnected(ClientConnectedArgs e)
        {
            ClientConnected?.Invoke(this, e);
        }

        protected virtual void OnMessageReceived(ClientMessageArgs e)
        {
            MessageReceived?.Invoke(this, e);
        }
    }
}