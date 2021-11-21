using System;
using dotnetCampus.Ipc.Abstractions;
using dotnetCampus.Ipc.PipeCore.Context;

namespace dotnetCampus.Ipc.PipeCore.IpcPipe
{
    class IpcMessageResponseManager : IpcMessageManagerBase
    {
        public IpcBufferMessageContext CreateResponseMessage(IpcClientRequestMessageId messageId,
            IpcRequestMessage response)
            => CreateResponseMessageInner(messageId, response);
    }
}
