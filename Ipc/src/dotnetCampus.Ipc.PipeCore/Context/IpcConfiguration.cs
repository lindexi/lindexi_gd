using dotnetCampus.Ipc.PipeCore.Utils;

namespace dotnetCampus.Ipc.PipeCore.Context
{
    /// <summary>
    /// 进程间通讯的配置
    /// </summary>
    public class IpcConfiguration
    {
        /// <summary>
        /// 用于内部使用的数组分配池
        /// </summary>
        public ISharedArrayPool SharedArrayPool { get; set; } = new SharedArrayPool();

        /// <summary>
        /// 每一条消息的头，用于处理消息的黏包和通讯损坏问题
        /// </summary>
        /// 在选用 Pipe 通讯，基本不存在通讯损坏等问题，也就是这个 Header 其实用途不大
        /// 这个 Header 的内容就是 dotnet campus 的 Ascii 数组
        /// dotnet campus 0x64, 0x6F, 0x74, 0x6E, 0x65, 0x74, 0x20, 0x63, 0x61, 0x6D, 0x70, 0x75, 0x73
        /// 大概的消息通讯方式如下，详细请看 <see cref="IpcMessageConverter"/> 的代码
        /*
         * Message:
         * Header
         * Length
         * Content
         */
        public byte[] MessageHeader { set; get; } =
            {0x64, 0x6F, 0x74, 0x6E, 0x65, 0x74, 0x20, 0x63, 0x61, 0x6D, 0x70, 0x75, 0x73};
    }
}
