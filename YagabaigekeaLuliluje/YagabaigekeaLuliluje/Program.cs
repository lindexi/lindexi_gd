using System;
using System.Net;
using System.Net.Sockets;

namespace YagabaigekeaLuliluje
{
    class Program
    {
        static void Main(string[] args)
        {
            var availablePort = GetAvailablePort(IPAddress.Any);
            Console.WriteLine(availablePort);
            TcpListener l = new TcpListener(IPAddress.Any, availablePort);
            l.Start();
        }

        public static int GetAvailablePort(IPAddress ip)
        {
            TcpListener l = new TcpListener(ip, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }
    }
}
