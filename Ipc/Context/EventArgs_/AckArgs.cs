using System;

namespace Ipc
{
    public class AckArgs : EventArgs
    {
        public AckArgs(string peerName, in Ack ack)
        {
            Ack = ack;
            PeerName = peerName;
        }

        public Ack Ack { get; }
        public string PeerName { get; }
    }
}