using System;
using dotnetCampus.Ipc.PipeCore.Context;

namespace dotnetCampus.Ipc.PipeCore
{
    public class IpcClientRequestArgs : EventArgs
    {
        public IpcClientRequestArgs(in ulong messageId, in IpcBufferMessage ipcBufferMessage)
        {
            MessageId = messageId;
            IpcBufferMessage = ipcBufferMessage;
        }

        public ulong MessageId { get; }
        public IpcBufferMessage IpcBufferMessage { get; }
    }
}
