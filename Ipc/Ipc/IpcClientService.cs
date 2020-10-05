using System;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Ipc
{
    public class IpcClientService
    {
        internal IpcClientService(IpcContext ipcContext, string serverName = IpcContext.DefaultPipeName)
        {
            IpcContext = ipcContext;
            ServerName = serverName;
        }

        private NamedPipeClientStream NamedPipeClientStream { set; get; } = null!;

        internal AckManager AckManager => IpcContext.AckManager;

        private IpcConfiguration IpcConfiguration => IpcContext.IpcConfiguration;

        public IpcContext IpcContext { get; }
        public string ServerName { get; }

        public async Task Start()
        {
            var namedPipeClientStream = new NamedPipeClientStream(".", ServerName, PipeDirection.InOut,
                PipeOptions.None, TokenImpersonationLevel.Impersonation);
            await namedPipeClientStream.ConnectAsync();

            NamedPipeClientStream = namedPipeClientStream;
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
            }, ServerName, TimeSpan.FromSeconds(3), maxRetryCount: 10);
        }

        public async Task SendAck(Ack receivedAck)
        {
            var ackMessage = AckManager.BuildAckMessage(receivedAck);
            await WriteMessageAsync(ackMessage, 0, ackMessage.Length);
        }
    }
}