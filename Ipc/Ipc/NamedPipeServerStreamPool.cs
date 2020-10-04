using System;
using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace Ipc
{
    public class IpcServerService
    {
        public IpcServerService(string pipeName, IpcContext ipcContext)
        {
            PipeName = pipeName;
            IpcContext = ipcContext;
        }

        public string PipeName { get; }
        public IpcContext IpcContext { get; }

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
            ClientConnected?.Invoke(sender, e);
        }

        public event EventHandler<ClientMessageArgs>? MessageReceived;

        public event EventHandler<ClientConnectedArgs>? ClientConnected;
    }
}