using System;

namespace Ipc
{
    public class AckArgs : EventArgs
    {
        public AckArgs(string clientName, ulong ack)
        {
            Ack = ack;
            ClientName = clientName;
        }

        public ulong Ack { get; }
        public string ClientName { get; }
    }
}