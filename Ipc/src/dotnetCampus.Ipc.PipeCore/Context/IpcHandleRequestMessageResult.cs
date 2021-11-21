using System.Diagnostics;
using dotnetCampus.Ipc.Abstractions;

namespace dotnetCampus.Ipc.PipeCore.Context
{
    class IpcHandleRequestMessageResult : IIpcHandleRequestMessageResult
    {
        [DebuggerStepThrough]
        public IpcHandleRequestMessageResult(IpcRequestMessage returnMessage)
        {
            ReturnMessage = returnMessage;
        }

        public IpcRequestMessage ReturnMessage { get; }
    }
}
