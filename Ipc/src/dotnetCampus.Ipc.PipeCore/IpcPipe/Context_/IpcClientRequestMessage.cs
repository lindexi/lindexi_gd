using System.Threading.Tasks;
using dotnetCampus.Ipc.Abstractions.Context;
using dotnetCampus.Ipc.PipeCore.Context;

namespace dotnetCampus.Ipc.PipeCore.IpcPipe
{
    class IpcClientRequestMessage
    {
        public IpcClientRequestMessage(IpcBufferMessageContext ipcBufferMessageContext, Task<IpcBufferMessage> task, IpcClientRequestMessageId messageId)
        {
            IpcBufferMessageContext = ipcBufferMessageContext;
            Task = task;
            MessageId = messageId;
        }

        public IpcBufferMessageContext IpcBufferMessageContext { get; }

        /// <summary>
        /// 用于等待消息被对方回复完成
        /// </summary>
        public Task<IpcBufferMessage> Task { get; }

        public IpcClientRequestMessageId MessageId { get; }
    }
}
