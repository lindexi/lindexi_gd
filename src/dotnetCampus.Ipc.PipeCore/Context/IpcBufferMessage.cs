namespace dotnetCampus.Ipc.PipeCore.Context
{
    readonly struct IpcBufferMessage
    {
        public IpcBufferMessage(byte[] buffer)
        {
            Buffer = buffer;
            Start = 0;
            Count = buffer.Length;
        }

        public IpcBufferMessage(byte[] buffer, int start, int count)
        {
            Buffer = buffer;
            Start = start;
            Count = count;
        }

        public byte[] Buffer { get; }
        public int Start { get; }
        public int Count { get; }
    }
}
