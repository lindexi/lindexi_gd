using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;

namespace dotnetCampus.Ipc.PipeCore
{
    class PeerManager
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
    }
}
