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
            using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(ip, 0));
            socket.Listen(1);
            var ipEndPoint = (IPEndPoint)socket.LocalEndPoint;
            var port = ipEndPoint.Port;
            return port;
        }
    }
}
