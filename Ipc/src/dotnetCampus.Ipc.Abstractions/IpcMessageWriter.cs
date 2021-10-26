using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace dotnetCampus.Ipc.Abstractions
{
    /// <summary>
    /// 提供消息的写入方法
    /// </summary>
    public class IpcMessageWriter : IMessageWriter
    {
        /// <summary>
        /// 创建提供消息的写入方法
        /// </summary>
        /// <param name="messageWriter">实际用来写入的方法</param>
        public IpcMessageWriter(IMessageWriter messageWriter)
        {
            MessageWriter = messageWriter;
        }

        /// <inheritdoc />
        public Task WriteMessageAsync(byte[] buffer, int offset, int count, [CallerMemberName] string summary = null!)
        {
            return MessageWriter.WriteMessageAsync(buffer, offset, count, summary);
        }

        /// <summary>
        /// 向对方发送消息
        /// </summary>
        /// <param name="message">字符串消息，将会被使用Utf-8编码转换然后发送</param>
        /// <param name="summary"></param>
        /// <returns></returns>
        public Task WriteMessageAsync(string message, string? summary = null)
        {
            if (summary is null)
            {
                summary = message;
            }

            var messageBuffer = Encoding.UTF8.GetBytes(message);
            return WriteMessageAsync(messageBuffer, 0, messageBuffer.Length, summary);
        }

        private IMessageWriter MessageWriter { get; }
    }
}
