using System;
using System.IO;

namespace dotnetCampus.Ipc.PipeCore.Context
{
    public class PeerMessageArgs : EventArgs
    {
        public PeerMessageArgs(string peerName, Stream stream, in Ack ack)
        {
            Stream = stream;
            Ack = ack;
            PeerName = peerName;
        }

        public Stream Stream { get; }
        public Ack Ack { get; }

        public string PeerName { get; }
    }
}