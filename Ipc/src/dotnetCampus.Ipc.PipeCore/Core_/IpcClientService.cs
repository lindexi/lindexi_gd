using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Threading.Tasks;
using dotnetCampus.Ipc.PipeCore.Context;
using dotnetCampus.Ipc.PipeCore.Utils;
using dotnetCampus.Ipc.PipeCore.Utils.Extensions;
using dotnetCampus.Threading;

namespace dotnetCampus.Ipc.PipeCore
{
    internal interface IClientMessageWriter : IMessageWriter
    {
        Task WriteMessageAsync(in IpcBufferMessageContext ipcBufferMessageContext);
    }

    /// <summary>
    /// 管道的客户端，用于发送消息
    /// </summary>
    /// 采用两个半工的管道做到双向通讯，这里的管道客户端用于发送
    public class IpcClientService : IMessageWriter, IDisposable, IClientMessageWriter
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

        private async Task DoTask(List<Func<Task>> list)
        {
            foreach (var func in list)
            {
                try
                {
                    await func();
                }
                catch (Exception e)
                {
                    IpcContext.Logger.Error($"[{nameof(IpcClientService)}.{nameof(DoTask)}] {e}");
                }
            }
        }

        private NamedPipeClientStream NamedPipeClientStream { set; get; } = null!;

        internal AckManager AckManager => IpcContext.AckManager;

        private IpcConfiguration IpcConfiguration => IpcContext.IpcConfiguration;

        /// <summary>
        /// 上下文
        /// </summary>
        public IpcContext IpcContext { get; }

        private PeerRegisterProvider PeerRegisterProvider => IpcContext.PeerRegisterProvider;

        /// <summary>
        /// 对方
        /// </summary>
        public string PeerName { get; }

        private ILogger Logger => IpcContext.Logger;

        /// <summary>
        /// 启动客户端，启动的时候将会去主动连接服务端，然后向服务端注册自身
        /// </summary>
        /// <param name="shouldRegisterToPeer">是否需要向对方注册</param>
        /// <returns></returns>
        public async Task Start(bool shouldRegisterToPeer = true)
        {
            var namedPipeClientStream = new NamedPipeClientStream(".", PeerName, PipeDirection.InOut,
                PipeOptions.None, TokenImpersonationLevel.Impersonation);
            await namedPipeClientStream.ConnectAsync();

            NamedPipeClientStream = namedPipeClientStream;

            if (shouldRegisterToPeer)
            {
                // 启动之后，向对方注册，此时对方是服务器
                await RegisterToPeer();
            }
        }

        private async Task RegisterToPeer()
        {
            Logger.Debug($"[{nameof(IpcClientService)}] StartRegisterToPeer PipeName={IpcContext.PipeName}");

            // 注册自己
            var peerRegisterMessage = PeerRegisterProvider.BuildPeerRegisterMessage(IpcContext.PipeName);
            await WriteMessageAsync(peerRegisterMessage);
        }

        /// <summary>
        /// 停止客户端
        /// </summary>
        public void Stop()
        {
            // 告诉服务器端不连接
        }

        internal async Task WriteMessageAsync(IpcBufferMessageContext ipcBufferMessageContext)
        {
            await QueueWriteAsync(async ack =>
            {
                await IpcMessageConverter.WriteAsync(NamedPipeClientStream, IpcConfiguration.MessageHeader, ack,
                    ipcBufferMessageContext, IpcContext.Logger);
                await NamedPipeClientStream.FlushAsync();
            }, ipcBufferMessageContext.Summary);
        }

        /// <summary>
        /// 向服务端发送消息
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="summary">这一次写入的是什么内容，用于调试</param>
        /// <returns></returns>
        /// 业务层使用的
        public async Task WriteMessageAsync(byte[] buffer, int offset, int count,
            [CallerMemberName] string summary = null!)
        {
            await QueueWriteAsync(async ack =>
            {
                await IpcMessageConverter.WriteAsync
                (
                    NamedPipeClientStream,
                    IpcConfiguration.MessageHeader,
                    ack,
                    // 表示这是业务层的消息
                    IpcMessageCommandType.Business,
                    buffer,
                    offset,
                    count,
                    summary,
                    Logger
                );
                await NamedPipeClientStream.FlushAsync();
            }, summary);
        }

        private async Task QueueWriteAsync(Func<Ack, Task> task, string summary)
        {
            await DoubleBufferTask.AddTaskAsync(async () =>
            {
                await AckManager.DoWillReceivedAck(task, PeerName, TimeSpan.FromSeconds(3), maxRetryCount: 10, summary,
                    IpcContext.Logger);
            });
        }

        private DoubleBufferTask<Func<Task>> DoubleBufferTask { get; }

        /// <summary>
        /// 向服务器端发送收到某条消息，或用于回复某条消息已收到
        /// </summary>
        /// <param name="receivedAck"></param>
        /// <returns></returns>
        public async Task SendAckAsync(Ack receivedAck)
        {
            var ackMessage = AckManager.BuildAckMessage(receivedAck);

            // 这里不能调用 WriteMessageAsync 方法，因为这些方法都使用了 QueueWriteAsync 方法，在这里面将会不断尝试发送信息，需要收到对方的 ack 才能完成。而作为回复 ack 消息的逻辑，如果还需要等待对方回复 ack 那么将会存在相互等待。本地回复对方的 ack 消息需要等待对方的 ack 消息，而对方的 ack 消息又需要等待本地的回复
            await DoubleBufferTask.AddTaskAsync(async () =>
            {
                await IpcMessageConverter.WriteAsync
                (
                    NamedPipeClientStream,
                    IpcConfiguration.MessageHeader,
                    ack: IpcContext.AckUsedForReply,
                    // 需要使用框架的命令
                    ipcMessageCommandType: IpcMessageCommandType.SendAck,
                    buffer: ackMessage,
                    offset: 0,
                    count: ackMessage.Length,
                    summary: nameof(SendAckAsync),
                    Logger
                );
                await NamedPipeClientStream.FlushAsync();
            });
        }

        /// <inheritdoc />
        public void Dispose()
        {
            NamedPipeClientStream.Dispose();
            DoubleBufferTask.Finish();
        }

        Task IClientMessageWriter.WriteMessageAsync(in IpcBufferMessageContext ipcBufferMessageContext) =>
            WriteMessageAsync(ipcBufferMessageContext);
    }
}
