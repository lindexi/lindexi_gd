using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using dotnetCampus.Threading;

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

            DoubleBufferTask = new DoubleBufferTask<Func<Task>>(DoTask);
        }

        private static async Task DoTask(List<Func<Task>> list)
        {
            foreach (var func in list)
            {
                try
                {
                    await func();
                }
                catch (Exception)
                {

                }
            }
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

        private ILogger Logger => IpcContext.Logger;

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
            Logger.Debug($"[{nameof(IpcClientService)}] StartRegisterToPeer PipeName={IpcContext.PipeName}");

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
            await QueueWriteAsync(async ack =>
            {
                await IpcMessageConverter.WriteAsync(NamedPipeClientStream, IpcConfiguration.MessageHeader, ack,
                    ipcBufferMessageContext);
                await NamedPipeClientStream.FlushAsync();
            });
        }

        internal async Task WriteMessageAsync(IpcBufferMessage ipcBufferMessage)
        {
            await QueueWriteAsync(async ack =>
            {
                await IpcMessageConverter.WriteAsync(NamedPipeClientStream, IpcConfiguration.MessageHeader, ack,
                    ipcBufferMessage);
                await NamedPipeClientStream.FlushAsync();
            });
        }

        public async Task WriteMessageAsync(byte[] buffer, int offset, int count)
        {
            await QueueWriteAsync(async ack =>
            {
                await IpcMessageConverter.WriteAsync(NamedPipeClientStream, IpcConfiguration.MessageHeader, ack, buffer,
                    offset,
                    count);
                await NamedPipeClientStream.FlushAsync();
            });
        }

        private async Task QueueWriteAsync(Func<Ack, Task> task)
        {
            await DoubleBufferTask.AddTaskAsync(async () =>
            {
                await AckManager.DoWillReceivedAck(task, PeerName, TimeSpan.FromSeconds(3), maxRetryCount: 10);
            });
        }

        private DoubleBufferTask<Func<Task>> DoubleBufferTask { get; }

        public async Task SendAck(Ack receivedAck)
        {
            var ackMessage = AckManager.BuildAckMessage(receivedAck);
            await WriteMessageAsync(ackMessage, 0, ackMessage.Length);
        }
    }
}