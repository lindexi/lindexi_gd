using System.Threading.Tasks;
using dotnetCampus.Ipc.Abstractions;
using dotnetCampus.Ipc.PipeCore.Context;

namespace dotnetCampus.Ipc.PipeCore
{
    internal interface IClientMessageWriter : IMessageWriter
    {
        Task WriteMessageAsync(in IpcBufferMessageContext ipcBufferMessageContext);
    }
}
