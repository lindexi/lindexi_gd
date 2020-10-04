using System;
using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace Ipc
{
    class NamedPipeServerStreamPool
    {
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

        public NamedPipeServerStreamPool(string pipeName, IpcContext ipcContext)
        {
            PipeName = pipeName;
            IpcContext = ipcContext;
        }

        public string PipeName { get; }
        public IpcContext IpcContext { get; }

        private void PipeServerMessage_MessageReceived(object? sender, ClientMessageArgs e)
        {
            MessageReceived?.Invoke(sender, e);
        }

        private void PipeServerMessage_ClientConnected(object? sender, ClientConnectedArgs e)
        {
            if (NamedPipeServerStreamList.TryAdd(e.ClientName, e.NamedPipeServerStream))
            {

            }
            else
            {
                // 有客户端重复连接，或这是服务器端连接
            }

            ClientConnected?.Invoke(sender, e);
        }

        public event EventHandler<ClientMessageArgs>? MessageReceived;

        public event EventHandler<ClientConnectedArgs>? ClientConnected;

        private ConcurrentDictionary<string, NamedPipeServerStream> NamedPipeServerStreamList { get; } = new ConcurrentDictionary<string, NamedPipeServerStream>();

        private IpcConfiguration IpcConfiguration { get; set; } = new IpcConfiguration();
    }
}