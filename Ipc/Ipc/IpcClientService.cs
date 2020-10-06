using System;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Ipc
{
    public class IpcClientService
    {
        /// <summary>
        /// 连接其他端，用来发送
        /// </summary>
        /// <param name="ipcContext"></param>
        /// <param name="peerName">对方</param>
        internal IpcClientService(IpcContext ipcContext, string peerName = IpcContext.DefaultPipeName)
        {
            IpcContext = ipcContext;
            PeerName = peerName;
        }

        private NamedPipeClientStream NamedPipeClientStream { set; get; } = null!;

        internal AckManager AckManager => IpcContext.AckManager;

        private IpcConfiguration IpcConfiguration => IpcContext.IpcConfiguration;

        public IpcContext IpcContext { get; }

        private PeerRegisterProvider PeerRegisterProvider => IpcContext.PeerRegisterProvider;

        /// <summary>
        /// 对方
        /// </summary>
        public string PeerName { get; }

        public async Task Start()
        {
            var namedPipeClientStream = new NamedPipeClientStream(".", PeerName, PipeDirection.InOut,
                PipeOptions.None, TokenImpersonationLevel.Impersonation);
            await namedPipeClientStream.ConnectAsync();

            NamedPipeClientStream = namedPipeClientStream;

            // 启动之后，向对方注册，此时对方是服务器
            await RegisterToPeer();
        }

        private async Task RegisterToPeer()
        {
            // 注册自己
            var peerRegisterMessage = PeerRegisterProvider.BuildPeerRegisterMessage(IpcContext.PipeName);
            await WriteMessageAsync(peerRegisterMessage);
        }

        public void Stop()
        {
            // 告诉服务器端不连接
        }

        public Task WriteStringAsync(string text)
        {
            var buffer = Encoding.UTF8.GetBytes(text);
            return WriteMessageAsync(buffer, 0, buffer.Length);
        }

        internal async Task WriteMessageAsync(IpcBufferMessageContext ipcBufferMessageContext)
        {
            await DoWillReceivedAck(async ack =>
            {
                await IpcMessageConverter.WriteAsync(NamedPipeClientStream, IpcConfiguration.MessageHeader, ack,
                    ipcBufferMessageContext);
                await NamedPipeClientStream.FlushAsync();
            });
        }

        internal async Task WriteMessageAsync(IpcBufferMessage ipcBufferMessage)
        {
            await DoWillReceivedAck(async ack =>
            {
                await IpcMessageConverter.WriteAsync(NamedPipeClientStream, IpcConfiguration.MessageHeader, ack,
                    ipcBufferMessage);
                await NamedPipeClientStream.FlushAsync();
            });
        }

        public async Task WriteMessageAsync(byte[] buffer, int offset, int count)
        {
            await DoWillReceivedAck(async ack =>
            {
                await IpcMessageConverter.WriteAsync(NamedPipeClientStream, IpcConfiguration.MessageHeader, ack, buffer,
                    offset,
                    count);
                await NamedPipeClientStream.FlushAsync();
            });
        }

        private async Task DoWillReceivedAck(Func<Ack, Task> task)
        {
            await AckManager.DoWillReceivedAck(task, PeerName, TimeSpan.FromSeconds(3), maxRetryCount: 10);
        }

        public async Task SendAck(Ack receivedAck)
        {
            var ackMessage = AckManager.BuildAckMessage(receivedAck);
            await WriteMessageAsync(ackMessage, 0, ackMessage.Length);
        }
    }

    readonly struct IpcBufferMessageContext
    {
        public IpcBufferMessageContext(params IpcBufferMessage[] ipcBufferMessageList)
        {
            IpcBufferMessageList = ipcBufferMessageList;
        }

        public IpcBufferMessage[] IpcBufferMessageList { get; }

        public int Length
        {
            get
            {
                var length = 0;
                foreach (var ipcBufferMessage in IpcBufferMessageList)
                {
                    length += ipcBufferMessage.Count;
                }

                return length;
            }
        }
    }

    readonly struct IpcBufferMessage
    {
        public IpcBufferMessage(byte[] buffer)
        {
            Buffer = buffer;
            Start = 0;
            Count = buffer.Length;
        }

        public IpcBufferMessage(byte[] buffer, int start, int count)
        {
            Buffer = buffer;
            Start = start;
            Count = count;
        }

        public byte[] Buffer { get; }
        public int Start { get; }
        public int Count { get; }
    }
}