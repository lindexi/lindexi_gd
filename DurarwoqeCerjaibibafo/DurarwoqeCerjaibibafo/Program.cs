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
            
            UdpClient client = new UdpClient(5656);
            client.JoinMulticastGroup(IPAddress.Parse("232.0.2.3"));
            IPEndPoint multicast = new IPEndPoint(IPAddress.Parse("232.0.2.3"), 7788);
            byte[] buf = Encoding.Default.GetBytes(Environment.UserName);
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
            client.JoinMulticastGroup(IPAddress.Parse("232.0.2.3"));
            IPEndPoint multicast = new IPEndPoint(IPAddress.Parse("232.0.2.3"), 7788);
            while (true)
            {
                byte[] buf = client.Receive(ref multicast);
                string msg = Encoding.Default.GetString(buf);
                Console.WriteLine(msg);
            }
        }
    }
}
