using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace DurarwoqeCerjaibibafo
{
    class Program
    {
        static void Main(string[] args)
        {
            UdpClient client = new UdpClient(59091);
            client.JoinMulticastGroup(IPAddress.Parse("234.5.6.7"));
            IPEndPoint multicast = new IPEndPoint(IPAddress.Parse("234.5.6.7"), 7788);
            byte[] buf = Encoding.Default.GetBytes("Hello from multicast");
            Thread t = new Thread(new ThreadStart(RecvThread));
            t.IsBackground = true;
            t.Start();

            while (true)
            {
                client.Send(buf, buf.Length, multicast);
                Thread.Sleep(1000);
            }
        }

        static void RecvThread()
        {
            UdpClient client = new UdpClient(7788);
            client.JoinMulticastGroup(IPAddress.Parse("234.5.6.7"));
            IPEndPoint multicast = new IPEndPoint(IPAddress.Parse("234.5.6.7"), 5566);
            while (true)
            {
                byte[] buf = client.Receive(ref multicast);
                string msg = Encoding.Default.GetString(buf);
                Console.WriteLine(msg);
            }
        }
    }
}
