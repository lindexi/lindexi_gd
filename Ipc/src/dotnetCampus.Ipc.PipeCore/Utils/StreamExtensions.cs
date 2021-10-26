using System.IO;
using System.Threading.Tasks;

namespace dotnetCampus.Ipc.PipeCore.Utils
{
#if NETFRAMEWORK
    static class StreamExtensions
    {
        /// <summary>
        /// 将字节序列异步写入
        /// </summary>
        /// <remarks>
        /// 在 .NET Framework 4.5 不支持 WriteAsync 写入 byte[] 而是需要传入开始和长度
        /// </remarks>
        /// <param name="stream"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Task WriteAsync(this Stream stream, byte[] data)
        {
            return stream.WriteAsync(data, 0, data.Length);
        }
    }
#endif
}
