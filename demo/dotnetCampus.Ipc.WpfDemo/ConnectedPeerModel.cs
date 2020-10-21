using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Threading;
using dotnetCampus.Ipc.PipeCore;

namespace dotnetCampus.Ipc.WpfDemo
{
    public class ConnectedPeerModel
    {
        public ConnectedPeerModel()
        {
            Peer = null!;
        }

        public ConnectedPeerModel(PeerProxy peer)
        {
            Peer = peer;
            peer.MessageReceived += Peer_MessageReceived;
        }

        private void Peer_MessageReceived(object? sender, PipeCore.Context.PeerMessageArgs e)
        {
            var streamReader = new StreamReader(e.Message);
            var message = streamReader.ReadToEnd();

            Dispatcher.InvokeAsync(() =>
            {
                MessageList.Add($"{PeerName}:\r\n{message}");
            });
        }

        public ObservableCollection<string> MessageList { get; } = new ObservableCollection<string>();

        public PeerProxy Peer { get; }

        public string PeerName => Peer.PeerName;

        private Dispatcher Dispatcher { get; } = Dispatcher.CurrentDispatcher;
    }
}
