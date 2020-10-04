namespace Ipc
{
    public readonly struct Ack
    {
        public Ack(ulong ack)
        {
            Value = ack;
        }

        public ulong Value { get; }

        public static implicit operator Ack(ulong ack) => new Ack(ack);
    }
}