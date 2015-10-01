using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace csmtp
{
    public class viewModel
    {
        public viewModel()
        {
            str = "asdasdawsfe";
            laji = new StringBuilder();
            Random ran = new Random();
            for (int i = 0; i < 4836470; i++)
            {
                laji.Append(ran.Next().ToString());
            }
        }
        StringBuilder laji;
        public void ce()
        {        
            
            Socket socket = new Socket(AddressFamily.InterNetwork , SocketType.Dgram , ProtocolType.Udp);
            IPAddress address = IPAddress.Parse("127.0.0.1");
            IPEndPoint ip = new IPEndPoint(IPAddress.Broadcast , 8000);
            ip = new IPEndPoint(address , 100);
            byte[] buff=Encoding.UTF8.GetBytes(laji.ToString());
            SocketAsyncEventArgs e = new SocketAsyncEventArgs();
            e.SetBuffer(buff , 0 , buff.Length);
            e.AcceptSocket = new Socket(AddressFamily.InterNetwork , SocketType.Dgram , ProtocolType.Udp);
            e.RemoteEndPoint = ip;
            socket.SendAsync(e);
            str = "";
        }
        private string _str;

        public string str
        {
            set
            {
                _str = value;
            }
            get
            {
                return _str;
            }
        }
    }

    public class listener
    {
        public void listen()
        {
            string ip = "127.0.0.1";
            byte[] bip = Encoding.UTF8.GetBytes(ip);
            IPAddress address = new IPAddress(bip);
            IPEndPoint iplocalendpoint = new IPEndPoint(address , 53433);

        }
    }
}
