using System;
using System.Diagnostics;
using System.Threading.Tasks;
using dotnetCampus.Ipc.PipeCore.Context;

namespace dotnetCampus.Ipc.PipeCore
{
    /// <summary>
    /// 用于表示远程的对方
    /// </summary>
    public class PeerProxy
    {
        internal PeerProxy(string peerName, IpcClientService ipcClientService)
        {
            PeerName = peerName;
            IpcClientService = ipcClientService;
        }

        internal PeerProxy(string peerName, IpcClientService ipcClientService, PeerConnectedArgs peerConnectedArgs) :
            this(peerName, ipcClientService)
        {
            Update(peerConnectedArgs);
        }

        /// <summary>
        /// 对方的服务器名
        /// </summary>
        public string PeerName { get; }

        internal TaskCompletionSource<bool> WaitForFinishedTaskCompletionSource { get; } =
            new TaskCompletionSource<bool>();

        /// <summary>
        /// 当收到消息时触发
        /// </summary>
        public event EventHandler<PeerMessageArgs>? MessageReceived;

        /// <summary>
        /// 用于写入数据
        /// </summary>
        public IpcClientService IpcClientService { get; }

        /// <summary>
        /// 获取是否连接完成，也就是可以读取，可以发送
        /// </summary>
        public bool IsConnectedFinished { get; private set; }

        internal void Update(PeerConnectedArgs peerConnectedArgs)
        {
            Debug.Assert(peerConnectedArgs.PeerName == PeerName);

            var serverStreamMessageReader = peerConnectedArgs.ServerStreamMessageReader;

            serverStreamMessageReader.MessageReceived -= ServerStreamMessageReader_MessageReceived;
            serverStreamMessageReader.MessageReceived += ServerStreamMessageReader_MessageReceived;

            IsConnectedFinished = true;

            if (WaitForFinishedTaskCompletionSource.TrySetResult(true))
            {
            }
            else
            {
                Debug.Assert(false, "重复调用");
            }
        }

        private void ServerStreamMessageReader_MessageReceived(object? sender, PeerMessageArgs e)
        {
            MessageReceived?.Invoke(sender, e);
        }
    }
}
