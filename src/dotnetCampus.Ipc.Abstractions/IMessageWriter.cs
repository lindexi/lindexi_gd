using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace dotnetCampus.Ipc.PipeCore.Context
{
    /// <summary>
    /// 用于表示发送消息
    /// </summary>
    public interface IMessageWriter
    {
        /// <summary>
        /// 向服务端发送消息
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="summary">这一次写入的是什么内容，用于调试</param>
        /// <returns></returns>
        Task WriteMessageAsync(byte[] buffer, int offset, int count,
            [CallerMemberName] string summary = null!);
    }
}
