using System;
using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace Ipc
{
    public class IpcServerService
    {
        public IpcServerService(IpcContext ipcContext)
        {
            IpcContext = ipcContext;
        }

        public string PipeName => IpcContext.PipeName;
        public IpcContext IpcContext { get; }

        public async Task Start()
        {
            while (true)
            {
                var pipeServerMessage = new PipeServerMessage(PipeName, IpcContext, this);

                await pipeServerMessage.Start();
            }
        }

        public event EventHandler<ClientMessageArgs>? MessageReceived;

        public event EventHandler<ClientConnectedArgs>? ClientConnected;

        public event EventHandler<AckArgs>? AckReceived;

        internal void OnAckReceived(AckArgs e)
        {
            AckReceived?.Invoke(this, e);
        }

        internal void OnMessageReceived(ClientMessageArgs e)
        {
            MessageReceived?.Invoke(this, e);
        }

        internal void OnClientConnected(ClientConnectedArgs e)
        {
            ClientConnected?.Invoke(this, e);
        }
    }
}