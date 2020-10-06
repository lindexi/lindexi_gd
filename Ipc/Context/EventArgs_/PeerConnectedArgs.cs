using System;
using System.IO.Pipes;

namespace Ipc
{
    public class PeerConnectedArgs : EventArgs
    {
        public PeerConnectedArgs(string peerName, NamedPipeServerStream namedPipeServerStream, in Ack ack)
        {
            PeerName = peerName;
            NamedPipeServerStream = namedPipeServerStream;
            Ack = ack;
        }

        public string PeerName { get; }

        public NamedPipeServerStream NamedPipeServerStream { get; }
        public Ack Ack { get; }
    }
}