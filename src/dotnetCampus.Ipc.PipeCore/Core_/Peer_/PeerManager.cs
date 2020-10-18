using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;

namespace dotnetCampus.Ipc.PipeCore
{
    class PeerManager : IDisposable
    {
        public ConcurrentDictionary<string, PeerProxy> ConnectedServerManagerList { get; } =
            new ConcurrentDictionary<string, PeerProxy>();

        public bool TryAdd(PeerProxy peerProxy) => ConnectedServerManagerList.TryAdd(peerProxy.PeerName, peerProxy);

        public async Task WaitForPeerConnectFinishedAsync(PeerProxy peerProxy)
        {
            await peerProxy.WaitForFinishedTaskCompletionSource.Task;

            // 更新或注册，用于解决之前注册的实际上是断开的连接
            ConnectedServerManagerList.AddOrUpdate(peerProxy.PeerName, peerProxy, (s, proxy) => proxy);
        }

        public void Dispose()
        {
            foreach (var (_, peer) in ConnectedServerManagerList)
            {
                // 为什么 PeerProxy 不加上 IDisposable 方法
                // 因为这个类在上层业务使用，被上层业务调释放就可以让框架不能使用
                peer.IpcClientService.Dispose();
            }
        }
    }
}
