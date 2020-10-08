using System;
using System.Threading.Tasks;
using dotnetCampus.Ipc.PipeCore.Context;

namespace dotnetCampus.Ipc.PipeCore
{
    public class IpcServerService
    {
        public IpcServerService(IpcContext ipcContext)
        {
            IpcContext = ipcContext;
        }

        public string PipeName => IpcContext.PipeName;
        public IpcContext IpcContext { get; }

        private ILogger Logger => IpcContext.Logger;

        public async Task Start()
        {
            while (true)
            {
                var pipeServerMessage = new PipeServerMessage(IpcContext, this);

                await pipeServerMessage.Start();
            }
        }

        public event EventHandler<PeerMessageArgs>? MessageReceived;

        public event EventHandler<PeerConnectedArgs>? PeerConnected;

        internal void OnMessageReceived(PeerMessageArgs e)
        {
            Logger.Debug($"[{nameof(IpcServerService)}] MessageReceived PeerName={e.PeerName} {e.Ack}");
            MessageReceived?.Invoke(this, e);
        }

        internal void OnPeerConnected(PeerConnectedArgs e)
        {
            Logger.Debug($"[{nameof(IpcServerService)}] PeerConnected PeerName={e.PeerName} {e.Ack}");

            PeerConnected?.Invoke(this, e);
        }
    }
}