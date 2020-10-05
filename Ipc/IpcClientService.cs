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
            await WriteStringAsync(IpcContext.PipeName);
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

        public async Task WriteMessageAsync(byte[] buffer, int offset, int count)
        {
            await AckManager.DoWillReceivedAck(async ack =>
            {
                await IpcMessageConverter.WriteAsync(NamedPipeClientStream, IpcConfiguration.MessageHeader, ack, buffer,
                    offset,
                    count);
                await NamedPipeClientStream.FlushAsync();
            }, PeerName, TimeSpan.FromSeconds(3), maxRetryCount: 10);
        }

        public async Task SendAck(Ack receivedAck)
        {
            var ackMessage = AckManager.BuildAckMessage(receivedAck);
            await WriteMessageAsync(ackMessage, 0, ackMessage.Length);
        }
    }
}