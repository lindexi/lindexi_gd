using dotnetCampus.Ipc.PipeCore.Context;

namespace dotnetCampus.Ipc.Abstractions
{
    public readonly struct IpcRequestMessage
    {
        public IpcRequestMessage(string summary, IpcBufferMessage requestMessage)
        {
            Summary = summary;
            RequestMessage = requestMessage;
        }

        public string Summary { get; }

        public IpcBufferMessage RequestMessage { get; }
    }
}
