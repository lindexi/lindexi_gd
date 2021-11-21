using System;
using System.Diagnostics;
using System.Threading.Tasks;
using dotnetCampus.Ipc.Abstractions;
using dotnetCampus.Ipc.Abstractions.Context;
using dotnetCampus.Ipc.PipeCore.Context;
using dotnetCampus.Ipc.PipeCore.IpcPipe;
using dotnetCampus.Ipc.PipeCore.Utils;

namespace dotnetCampus.Ipc.PipeCore
{
    /// <summary>
    /// 用于表示远程的对方
    /// </summary>
    public class PeerProxy : IPeerProxy
    {
        internal PeerProxy(string peerName, IpcClientService ipcClientService, IpcContext ipcContext)
        {
            PeerName = peerName;
            IpcClientService = ipcClientService;
            IpcMessageWriter = new IpcMessageWriter(ipcClientService);

            IpcContext = ipcContext;

            IpcMessageRequestManager = new IpcMessageRequestManager();
            IpcMessageRequestManager.OnIpcClientRequestReceived += ResponseManager_OnIpcClientRequestReceived;
        }

        internal PeerProxy(string peerName, IpcClientService ipcClientService, IpcInternalPeerConnectedArgs ipcInternalPeerConnectedArgs, IpcContext ipcContext) :
            this(peerName, ipcClientService, ipcContext)
        {
            Update(ipcInternalPeerConnectedArgs);
        }

        /// <summary>
        /// 对方的服务器名
        /// </summary>
        public string PeerName { get; }

        internal TaskCompletionSource<bool> WaitForFinishedTaskCompletionSource { get; } =
            new TaskCompletionSource<bool>();

        private IpcContext IpcContext { get; }

        /// <summary>
        /// 当收到消息时触发
        /// </summary>
        public event EventHandler<IPeerMessageArgs>? MessageReceived;

        internal IpcMessageRequestManager IpcMessageRequestManager { get; }

        /// <inheritdoc />
        public async Task<IpcBufferMessage> GetResponseAsync(IpcRequestMessage request)
        {
            var ipcClientRequestMessage = IpcMessageRequestManager.CreateRequestMessage(request);
            await IpcClientService.WriteMessageAsync(ipcClientRequestMessage.IpcBufferMessageContext);
            return await ipcClientRequestMessage.Task;
        }

        /// <inheritdoc />
        public event EventHandler<IPeerConnectionBrokenArgs>? PeerConnectionBroken;

        /// <summary>
        /// 用于写入数据
        /// </summary>
        public IpcMessageWriter IpcMessageWriter { get; }

        /// <summary>
        /// 表示作为客户端和对方连接
        /// </summary>
        /// 框架内使用
        internal IpcClientService IpcClientService { get; }

        internal bool IsBroken { get; private set; }

        /// <summary>
        /// 获取是否连接完成，也就是可以读取，可以发送
        /// </summary>
        public bool IsConnectedFinished { get; private set; }

        ///// <summary>
        ///// 当断开连接的时候触发
        ///// </summary>
        //public event EventHandler<PeerProxy>? Disconnected;

        internal void Update(IpcInternalPeerConnectedArgs ipcInternalPeerConnectedArgs)
        {
            Debug.Assert(ipcInternalPeerConnectedArgs.PeerName == PeerName);

            var serverStreamMessageReader = ipcInternalPeerConnectedArgs.ServerStreamMessageReader;

            serverStreamMessageReader.MessageReceived -= ServerStreamMessageReader_MessageReceived;
            serverStreamMessageReader.MessageReceived += ServerStreamMessageReader_MessageReceived;

            // 连接断开
            serverStreamMessageReader.PeerConnectBroke -= ServerStreamMessageReader_PeerConnectBroke;
            serverStreamMessageReader.PeerConnectBroke += ServerStreamMessageReader_PeerConnectBroke;

            IsConnectedFinished = true;

            if (WaitForFinishedTaskCompletionSource.TrySetResult(true))
            {
            }
            else
            {
                Debug.Assert(false, "重复调用");
            }
        }

        private void ServerStreamMessageReader_PeerConnectBroke(object? sender, PeerConnectionBrokenArgs e)
        {
            OnPeerConnectionBroken(e);
        }

        private void ServerStreamMessageReader_MessageReceived(object? sender, PeerMessageArgs e)
        {
            IpcMessageRequestManager.OnReceiveMessage(e);

            MessageReceived?.Invoke(sender, e);
        }

        private void ResponseManager_OnIpcClientRequestReceived(object? sender, IpcClientRequestArgs e)
        {
            var ipcRequestHandlerProvider = IpcContext.IpcRequestHandlerProvider;
            ipcRequestHandlerProvider.HandleRequest(this, e);
        }

        private void OnPeerConnectionBroken(IPeerConnectionBrokenArgs e)
        {
            IsBroken = true;
            IpcClientService.Dispose();

            PeerConnectionBroken?.Invoke(this, e);
        }
    }
}
