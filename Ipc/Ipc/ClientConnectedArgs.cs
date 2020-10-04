using System;
using System.IO.Pipes;

namespace Ipc
{
    public class ClientConnectedArgs : EventArgs
    {
        public ClientConnectedArgs(string clientName, NamedPipeServerStream namedPipeServerStream)
        {
            ClientName = clientName;
            NamedPipeServerStream = namedPipeServerStream;
        }

        public string ClientName { get; }

        public NamedPipeServerStream NamedPipeServerStream { get; }
    }
}