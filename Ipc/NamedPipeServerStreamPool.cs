using System;
using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace Ipc
{
    internal class NamedPipeServerStreamPool
    {
        public NamedPipeServerStreamPool(string pipeName, IpcContext ipcContext)
        {
            PipeName = pipeName;
            IpcContext = ipcContext;
        }

        public string PipeName { get; }
        public IpcContext IpcContext { get; }

        private ConcurrentDictionary<string, NamedPipeServerStream> NamedPipeServerStreamList { get; } =
            new ConcurrentDictionary<string, NamedPipeServerStream>();

        private IpcConfiguration IpcConfiguration { get; set; } = new IpcConfiguration();

        public async Task Start()
        {
            while (true)
            {
                var pipeServerMessage = new PipeServerMessage(PipeName, IpcContext);

                pipeServerMessage.ClientConnected += PipeServerMessage_ClientConnected;
                pipeServerMessage.MessageReceived += PipeServerMessage_MessageReceived;

                await pipeServerMessage.Start();
            }
        }

        private void PipeServerMessage_MessageReceived(object? sender, ClientMessageArgs e)
        {
            MessageReceived?.Invoke(sender, e);
        }

        private void PipeServerMessage_ClientConnected(object? sender, ClientConnectedArgs e)
        {
            if (NamedPipeServerStreamList.TryAdd(e.ClientName, e.NamedPipeServerStream))
            {
            }

            ClientConnected?.Invoke(sender, e);
        }

        public event EventHandler<ClientMessageArgs>? MessageReceived;

        public event EventHandler<ClientConnectedArgs>? ClientConnected;
    }
}