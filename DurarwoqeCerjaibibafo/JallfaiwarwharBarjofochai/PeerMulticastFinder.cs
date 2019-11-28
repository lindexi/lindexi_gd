using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Mssc.TransportProtocols.Utilities
{
    internal class PeerMulticastFinder : IDisposable
    {
        /// <inheritdoc />
        public PeerMulticastFinder()
        {
            MulticastSocket = new Socket(AddressFamily.InterNetwork,
                SocketType.Dgram,
                ProtocolType.Udp);
            MulticastAddress = IPAddress.Parse("230.138.100.2");
        }

        /// <summary>
        /// 寻找局域网设备
        /// </summary>
        public void FindPeer()
        {
            // 实际是反过来，让其他设备询问

            StartMulticast();

            var ipList = GetLocalIpList().ToList();
            var message = string.Join(';',ipList);
            SendBroadcastMessage(message);
            // 先发送再获取消息，这样就不会收到自己发送的消息
            ReceivedMessage += (s, e) => { Console.WriteLine($"找到 {e}"); };
        }

        /// <summary>
        /// 获取本地 IP 地址
        /// </summary>
        /// <returns></returns>
        private IEnumerable<IPAddress> GetLocalIpList()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    yield return ip;
                }
            }
        }

        /// <summary>
        /// 组播地址
        /// <para/>
        /// 224.0.0.0～224.0.0.255为预留的组播地址（永久组地址），地址224.0.0.0保留不做分配，其它地址供路由协议使用；
        /// <para/>
        /// 224.0.1.0～224.0.1.255是公用组播地址，可以用于Internet；
        /// <para/>
        /// 224.0.2.0～238.255.255.255为用户可用的组播地址（临时组地址），全网范围内有效；
        /// <para/>
        /// 239.0.0.0～239.255.255.255为本地管理组播地址，仅在特定的本地范围内有效。
        /// </summary>
        public IPAddress MulticastAddress { set; get; }

        private const int MulticastPort = 15003;

        /// <summary>
        /// 启动组播
        /// </summary>
        public void StartMulticast()
        {
            try
            {
                // 如果首次绑定失败，那么将无法接收，但是可以发送
                TryBindSocket();

                // Define a MulticastOption object specifying the multicast group 
                // address and the local IPAddress.
                // The multicast group address is the same as the address used by the server.
                var multicastOption = new MulticastOption(MulticastAddress, IPAddress.Parse("172.18.134.16"));

                MulticastSocket.SetSocketOption(SocketOptionLevel.IP,
                    SocketOptionName.AddMembership,
                    multicastOption);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Task.Run(ReceiveBroadcastMessages);
        }

        /// <summary>
        /// 收到消息
        /// </summary>
        public event EventHandler<string> ReceivedMessage;

        private void ReceiveBroadcastMessages()
        {
            // 接收需要绑定 MulticastPort 端口
            var bytes = new byte[MaxByteLength];
            EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

            try
            {
                while (!disposedValue)
                {
                    var length = MulticastSocket.ReceiveFrom(bytes, ref remoteEndPoint);

                    OnReceivedMessage(Encoding.UTF8.GetString(bytes, 0, length));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// 发送组播
        /// </summary>
        /// <param name="message"></param>
        public void SendBroadcastMessage(string message)
        {
            try
            {
                var endPoint = new IPEndPoint(MulticastAddress, MulticastPort);
                var byteList = Encoding.UTF8.GetBytes(message);

                if (byteList.Length > MaxByteLength)
                {
                    throw new ArgumentException($"传入 message 转换为 byte 数组长度太长，不能超过{MaxByteLength}字节")
                    {
                        Data =
                        {
                            { "message", message },
                            { "byteList", byteList }
                        }
                    };
                }

                MulticastSocket.SendTo(byteList, endPoint);
            }
            catch (Exception e)
            {
                Console.WriteLine("\n" + e);
            }
        }


        private Socket MulticastSocket { get; }

        private void TryBindSocket()
        {
            for (var i = MulticastPort; i < 65530; i++)
            {
                try
                {
                    EndPoint localEndPoint = new IPEndPoint(IPAddress.Any, i);

                    MulticastSocket.Bind(localEndPoint);
                    return;
                }
                catch (SocketException e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        private const int MaxByteLength = 1024;

        #region IDisposable Support

        private bool disposedValue = false; // 要检测冗余调用

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }

                MulticastSocket.Dispose();

                ReceivedMessage = null;
                MulticastAddress = null;

                disposedValue = true;
            }
        }

        // 添加此代码以正确实现可处置模式。
        public void Dispose()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        private void OnReceivedMessage(string e)
        {
            ReceivedMessage?.Invoke(this, e);
        }

        
    }
}