using System;
using dotnetCampus.Ipc.Abstractions.Context;

namespace dotnetCampus.Ipc.PipeCore.Utils.Extensions
{
    static class IpcBufferMessageExtension
    {
#if NETCOREAPP3_1_OR_GREATER
        public static Span<byte> AsSpan(this in IpcBufferMessage message)
        {
            return message.Buffer.AsSpan(message.Start, message.Count);
        }
#endif
    }
}
