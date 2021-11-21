using System.Diagnostics;
using dotnetCampus.Ipc.Abstractions;
using dotnetCampus.Ipc.Abstractions.Context;
using dotnetCampus.Ipc.PipeCore.Context;

namespace dotnetCampus.Ipc.PipeCore.IpcPipe
{
    class IpcRequestMessageContext : IIpcRequestContext
    {
        [DebuggerStepThrough]
        public IpcRequestMessageContext(IpcBufferMessage ipcBufferMessage, IPeerProxy peer)
        {
            IpcBufferMessage = ipcBufferMessage;
            Peer = peer;
        }

        /// <inheritdoc />
        public bool Handle { get; set; }

        /// <inheritdoc />
        public IpcBufferMessage IpcBufferMessage { get; }

        public IPeerProxy Peer { get; }
    }
}
