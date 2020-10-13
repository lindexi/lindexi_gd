using System;
using System.Threading.Tasks;
using dotnetCampus.Ipc.PipeCore.Context;

namespace dotnetCampus.Ipc.PipeCore
{
    /// <summary>
    /// 管道服务端，用于接收消息
    /// </summary>
    /// 采用两个半工的管道做到双向通讯，这里的管道服务端用于接收
    public class IpcServerService : IDisposable
    {
        /// <summary>
        /// 管道服务端
        /// </summary>
        /// <param name="ipcContext"></param>
        public IpcServerService(IpcContext ipcContext)
        {
            IpcContext = ipcContext;
        }

        /// <summary>
        /// 上下文
        /// </summary>
        public IpcContext IpcContext { get; }

        private ILogger Logger => IpcContext.Logger;

        /// <summary>
        /// 启动服务
        /// </summary>
        /// <returns></returns>
        public async Task Start()
        {
            while (!_isDisposed)
            {
                var pipeServerMessage = new IpcPipeServerMessageProvider(IpcContext, this);

                await pipeServerMessage.Start();
            }
        }

        /// <summary>
        /// 当收到消息时触发
        /// </summary>
        public event EventHandler<PeerMessageArgs>? MessageReceived;

        /// <summary>
        /// 当有对方连接时触发
        /// </summary>
        public event EventHandler<PeerConnectedArgs>? PeerConnected;

        internal void OnMessageReceived(object? sender, PeerMessageArgs e)
        {
            Logger.Debug($"[{nameof(IpcServerService)}] MessageReceived PeerName={e.PeerName} {e.Ack}");
            MessageReceived?.Invoke(sender, e);
        }

        internal void OnPeerConnected(object? sender, PeerConnectedArgs e)
        {
            Logger.Debug($"[{nameof(IpcServerService)}] PeerConnected PeerName={e.PeerName} {e.Ack}");

            PeerConnected?.Invoke(sender, e);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
            }

            _isDisposed = true;
        }

        private bool _isDisposed;

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
