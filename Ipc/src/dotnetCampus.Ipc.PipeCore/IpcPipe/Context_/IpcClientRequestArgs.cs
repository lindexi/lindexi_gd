using System;
using dotnetCampus.Ipc.PipeCore.Context;

namespace dotnetCampus.Ipc.PipeCore
{
    public class IpcClientRequestArgs : EventArgs
    {
        public IpcClientRequestArgs(in IpcClientRequestMessageId messageId, in IpcBufferMessage ipcBufferMessage)
        {
            MessageId = messageId;
            IpcBufferMessage = ipcBufferMessage;
        }

        public IpcClientRequestMessageId MessageId { get; }
        public IpcBufferMessage IpcBufferMessage { get; }
    }
}
