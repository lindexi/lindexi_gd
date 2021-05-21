namespace dotnetCampus.Ipc.PipeCore.Context
{
    /// <summary>
    /// 用于作为消息的回复编号
    /// <para>
    /// 仅为了不直接使用 <see cref="ulong"/> 而定义一个类型，解决使用 <see cref="ulong"/> 和业务上存在混乱
    /// </para>
    /// </summary>
    public readonly struct Ack
    {
        /// <summary>
        /// 创建消息的回复号
        /// </summary>
        /// <param name="ack"></param>
        public Ack(ulong ack)
        {
            Value = ack;
        }

        /// <summary>
        /// 具体的编号值
        /// </summary>
        public ulong Value { get; }

        /// <summary>
        /// 转换逻辑
        /// </summary>
        /// <param name="ack"></param>
        public static implicit operator Ack(ulong ack)
        {
            return new Ack(ack);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Ack={Value}";
        }
    }
}
