using System;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Ipc
{
    public class IpcClientService
    {
        public IpcClientService(string serverName = IpcContext.PipeName)
        {
            ServerName = serverName;
        }

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

        private NamedPipeClientStream NamedPipeClientStream { set; get; } = null!;

        public Task WriteStringAsync(string text)
        {
            var buffer = Encoding.UTF8.GetBytes(text);
            return WriteMessageAsync(buffer, 0, buffer.Length);
        }

        public async Task WriteMessageAsync(byte[] buffer, int offset, int count)
        {
            await NamedPipeClientStream.WriteAsync(IpcConfiguration.MessageHeader);
            var byteList = BitConverter.GetBytes(count);
            await NamedPipeClientStream.WriteAsync(byteList, 0, byteList.Length);
            await NamedPipeClientStream.WriteAsync(buffer, offset, count);
            await NamedPipeClientStream.FlushAsync();
        }

        private IpcConfiguration IpcConfiguration { get; set; } = new IpcConfiguration();

        public string ServerName { get; }
    }
}