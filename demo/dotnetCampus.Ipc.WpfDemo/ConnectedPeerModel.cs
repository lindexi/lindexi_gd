using dotnetCampus.Ipc.PipeCore;

namespace dotnetCampus.Ipc.WpfDemo
{
    public class ConnectedPeerModel
    {
        public ConnectedPeerModel(PeerProxy peer)
        {
            Peer = peer;
        }

        public PeerProxy Peer { get; }

        public string PeerName => Peer.PeerName;
    }
}
