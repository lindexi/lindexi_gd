using System;

namespace Ipc
{
    public class AckArgs : EventArgs
    {
        public AckArgs(string clientName, in Ack ack)
        {
            Ack = ack;
            ClientName = clientName;
        }

        public Ack Ack { get; }
        public string ClientName { get; }
    }
}