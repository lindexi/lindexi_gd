namespace dotnetCampus.Ipc.PipeCore.Context
{
    public readonly struct Ack
    {
        public Ack(ulong ack)
        {
            Value = ack;
        }

        public ulong Value { get; }

        public static implicit operator Ack(ulong ack)
        {
            return new Ack(ack);
        }

        public override string ToString()
        {
            return $"Ack={Value}";
        }
    }
}