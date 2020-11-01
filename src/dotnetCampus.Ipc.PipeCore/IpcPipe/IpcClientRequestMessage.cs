using System.Threading.Tasks;
using dotnetCampus.Ipc.PipeCore.Context;

namespace dotnetCampus.Ipc.PipeCore
{
    class IpcClientRequestMessage
    {
        public IpcClientRequestMessage(IpcBufferMessageContext ipcBufferMessageContext, Task<IpcBufferMessage> task, ulong messageId)
        {
            IpcBufferMessageContext = ipcBufferMessageContext;
            Task = task;
            MessageId = messageId;
        }

        public IpcBufferMessageContext IpcBufferMessageContext { get; }

        public Task<IpcBufferMessage> Task { get; }

        public ulong MessageId { get; }
    }
}
