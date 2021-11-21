using System.Diagnostics;

namespace dotnetCampus.Ipc.Abstractions.Context
{
    /// <summary>
    /// 表示一段 Ipc 消息内容
    /// </summary>
    public readonly struct IpcBufferMessage
    {
        /// <summary>
        /// 创建一段 Ipc 消息内容
        /// </summary>
        /// <param name="buffer"></param>
        [DebuggerStepThrough]
        public IpcBufferMessage(byte[] buffer)
        {
            Buffer = buffer;
            Start = 0;
            Count = buffer.Length;
        }

        /// <summary>
        /// 创建一段 Ipc 消息内容
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="start"></param>
        /// <param name="count"></param>
        [DebuggerStepThrough]
        public IpcBufferMessage(byte[] buffer, int start, int count)
        {
            Buffer = buffer;
            Start = start;
            Count = count;
        }

        /// <summary>
        /// 缓存数据
        /// </summary>
        public byte[] Buffer { get; }

        /// <summary>
        /// 缓存数据的起始点
        /// </summary>
        public int Start { get; }

        /// <summary>
        /// 数据长度
        /// </summary>
        public int Count { get; }
    }
}
