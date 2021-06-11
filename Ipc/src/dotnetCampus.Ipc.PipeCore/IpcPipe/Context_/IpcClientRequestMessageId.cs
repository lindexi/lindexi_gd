namespace dotnetCampus.Ipc.PipeCore
{
    public readonly struct IpcClientRequestMessageId
    {
        public IpcClientRequestMessageId(ulong messageIdValue)
        {
            MessageIdValue = messageIdValue;
        }

        public ulong MessageIdValue { get; }
    }
}
