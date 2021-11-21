using dotnetCampus.Ipc.PipeCore.Context;

namespace dotnetCampus.Ipc.PipeCore.Utils.Extensions
{
    static class IpcMessageContextExtension
    {
        public static ByteListMessageStream ToStream(this in IpcMessageContext ipcMessageContext)
        {
            return new ByteListMessageStream(ipcMessageContext);
        }
    }
}
