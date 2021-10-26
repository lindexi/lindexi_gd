
namespace dotnetCampus.Ipc.PipeCore
{
    class DebugContext
    {
        public const string DoNotUseAck = "当前不再需要回复ACK信息，因为管道通讯形式是不需要获取对方是否收到";

        public const string OverMaxMessageLength = "消息内容允许最大的长度。超过这个长度，咋不上天。如果真有那么大的内容准备传的，自己开共享内存或写文件等方式传输，然后通过 IPC 告知对方如何获取即可";
    }
}
