using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Threading;
using dotnetCampus.Ipc.Abstractions;
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

        private void Peer_MessageReceived(object? sender, IPeerMessageArgs e)
        {
            var streamReader = new StreamReader(e.Message);
            var message = streamReader.ReadToEnd();

            Dispatcher.InvokeAsync(() =>
            {
                AddMessage(PeerName, message);
            });
        }

        public void AddMessage(string name, string message)
        {
            MessageList.Add($"{name} {DateTime.Now:yyyy/MM/dd hh:mm:ss.fff}:\r\n{message}");
        }

        public ObservableCollection<string> MessageList { get; } = new ObservableCollection<string>();

        public PeerProxy Peer { get; }

        public string PeerName => Peer.PeerName;

        private Dispatcher Dispatcher { get; } = Dispatcher.CurrentDispatcher;
    }
}
