using dotnetCampus.Ipc.PipeCore.Utils;

namespace dotnetCampus.Ipc.PipeCore.Context
{
    internal readonly struct IpcMessageContext
    {
        public IpcMessageContext(in ulong ack, byte[] messageBuffer, in uint messageLength,
            ISharedArrayPool sharedArrayPool)
        {
            Ack = ack;
            MessageBuffer = messageBuffer;
            MessageLength = messageLength;
            SharedArrayPool = sharedArrayPool;
        }

        public Ack Ack { get; }
        public byte[] MessageBuffer { get; }
        public uint MessageLength { get; }
        public ISharedArrayPool SharedArrayPool { get; }
    }
}
