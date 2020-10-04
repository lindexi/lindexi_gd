using System;
using System.IO;

namespace Ipc
{
    public class ClientMessageArgs : EventArgs
    {
        public ClientMessageArgs(string clientName, Stream stream)
        {
            Stream = stream;
            ClientName = clientName;
        }

        public Stream Stream { get; }

        public string ClientName { get; }
    }
}