using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using dotnetCampus.Ipc.PipeCore.Context;
using dotnetCampus.Ipc.PipeCore.Utils;

namespace dotnetCampus.Ipc.PipeCore
{
    /// <summary>
    /// 基础的数据读取
    /// </summary>
    class ServerStreamMessageReader : IDisposable
    {
        public ServerStreamMessageReader(IpcContext ipcContext, Stream stream)
        {
            IpcContext = ipcContext;
            Stream = stream;
        }

        public IpcContext IpcContext { get; }

        /// <summary>
        /// 被对方连接的对方设备名
        /// </summary>
        public string PeerName { set; get; } = null!;

        private IpcConfiguration IpcConfiguration => IpcContext.IpcConfiguration;
        private Stream Stream { get; }

        /// <summary>
        /// 请求发送回复的 ack 消息
        /// </summary>
        internal event EventHandler<Ack>? AckRequested;

        /// <summary>
        /// 当收到对方确定收到消息时触发
        /// </summary>
        public event EventHandler<AckArgs>? AckReceived;

        /// <summary>
        /// 当收到消息时触发
        /// </summary>
        public event EventHandler<PeerMessageArgs>? MessageReceived;

        /// <summary>
        /// 当有对方连接时触发
        /// </summary>
        public event EventHandler<IpcInternalPeerConnectedArgs>? PeerConnected;

        public async void Run()
        {
            await WaitForConnectionAsync().ConfigureAwait(false);

            await ReadMessageAsync().ConfigureAwait(false);
        }

        private async Task WaitForConnectionAsync()
        {
            while (!_isDisposed)
            {
                try
                {
                    var ipcMessageResult = await IpcMessageConverter.ReadAsync(Stream,
                        IpcConfiguration.MessageHeader,
                        IpcConfiguration.SharedArrayPool).ConfigureAwait(false);
                    var success = ipcMessageResult.Success;
                    var ipcMessageContext = ipcMessageResult.IpcMessageContext;

                    // 这不是业务消息
                    Debug.Assert(!ipcMessageResult.IpcMessageCommandType.HasFlag(IpcMessageCommandType.Business));

                    if (success)
                    {
                        var stream = new ByteListMessageStream(ipcMessageContext);

                        var isPeerRegisterMessage =
                            IpcContext.PeerRegisterProvider.TryParsePeerRegisterMessage(stream, out var peerName);

                        if (isPeerRegisterMessage)
                        {
                            // ReSharper disable once MethodHasAsyncOverload
                            PeerName = peerName;

                            OnPeerConnected(new IpcInternalPeerConnectedArgs(peerName, Stream, ipcMessageContext.Ack,
                                this));

                            //SendAckAndRegisterToPeer(ipcMessageContext.Ack);
                            //SendAck(ipcMessageContext.Ack);
                            //// 不等待对方收到，因为对方也在等待
                            ////await SendAckAsync(ipcMessageContext.Ack);
                        }

                        // 如果是 对方的注册消息 同时也许是回应的消息，所以不能加上 else if 判断
                        if (IpcContext.AckManager.IsAckMessage(stream, out var ack))
                        {
                            // 只有作为去连接对方的时候，才会收到这个消息
                            IpcContext.Logger.Debug($"[{nameof(IpcServerService)}] AckReceived {ack} From {PeerName}");
                            OnAckReceived(new AckArgs(PeerName, ack));

                            if (isPeerRegisterMessage)
                            {
                                // 这是一条本地主动去连接对方，然后收到对方的反过来的连接的信息，此时需要回复对方
                                // 参阅 SendAckAndRegisterToPeer 方法的实现
                                //SendAck(ipcMessageContext.Ack);
                                OnAckRequested(ipcMessageContext.Ack);
                            }
                        }
                        else
                        {
                            // 后续需要要求重发设备名
                        }

                        if (isPeerRegisterMessage)
                        {
                            // 收到注册消息了
                            break;
                        }
                    }
                }
                catch (EndOfStreamException)
                {
                    // 对方关闭了
                    // [断开某个进程 使用大量CPU在读取 · Issue #15 · dotnet-campus/dotnetCampus.Ipc](https://github.com/dotnet-campus/dotnetCampus.Ipc/issues/15 )
                    IpcContext.Logger.Error($"对方已关闭");
                    return;
                }
                catch (Exception e)
                {
                    IpcContext.Logger.Error(e);
                }
            }
        }

        private async Task ReadMessageAsync()
        {
            while (!_isDisposed)
            {
                try
                {
                    var ipcMessageResult = await IpcMessageConverter.ReadAsync(Stream,
                        IpcConfiguration.MessageHeader,
                        IpcConfiguration.SharedArrayPool);
                    var success = ipcMessageResult.Success;
                    var ipcMessageContext = ipcMessageResult.IpcMessageContext;
                    var ipcMessageCommandType = ipcMessageResult.IpcMessageCommandType;

                    if (success)
                    {
                        var stream = new ByteListMessageStream(ipcMessageContext);

                        if (ipcMessageCommandType.HasFlag(IpcMessageCommandType.SendAck) && IpcContext.AckManager.IsAckMessage(stream, out var ack))
                        {
                            IpcContext.Logger.Debug($"[{nameof(IpcServerService)}] AckReceived {ack} From {PeerName}");
                            OnAckReceived(new AckArgs(PeerName, ack));
                            // 如果是收到 ack 回复了，那么只需要向 AckManager 注册
                            Debug.Assert(ipcMessageContext.Ack.Value == IpcContext.AckUsedForReply.Value);
                        }
                        // 只有业务的才能发给上层
                        else if (ipcMessageCommandType.HasFlag(IpcMessageCommandType.Business))
                        {
                            ack = ipcMessageContext.Ack;
                            OnAckRequested(ack);
                            OnMessageReceived(new PeerMessageArgs(PeerName, stream, ack,ipcMessageCommandType));
                        }
                        else
                        {
                            // 有不能解析的信息，后续需要告诉开发
                            // 依然回复一条 Ack 消息给对方，让对方不用重复发送
                            OnAckRequested(ipcMessageContext.Ack);
                        }
                    }
                }
                catch (EndOfStreamException)
                {
                    // 对方关闭了
                    // [断开某个进程 使用大量CPU在读取 · Issue #15 · dotnet-campus/dotnetCampus.Ipc](https://github.com/dotnet-campus/dotnetCampus.Ipc/issues/15 )
                    IpcContext.Logger.Error($"对方已关闭");
                    return;
                }
                catch (Exception e)
                {
                    IpcContext.Logger.Error(e);
                }
            }
        }

        private void OnAckReceived(AckArgs e)
        {
            AckReceived?.Invoke(this, e);
        }

        private void OnMessageReceived(PeerMessageArgs e)
        {
            MessageReceived?.Invoke(this, e);
        }

        private void OnPeerConnected(IpcInternalPeerConnectedArgs e)
        {
            PeerConnected?.Invoke(this, e);
        }

        private void OnAckRequested(in Ack e)
        {
            AckRequested?.Invoke(this, e);
        }

        ~ServerStreamMessageReader()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }

            _isDisposed = true;
            Stream.Dispose();
        }

        private bool _isDisposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
