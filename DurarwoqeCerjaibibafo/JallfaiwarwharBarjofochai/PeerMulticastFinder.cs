using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Mssc.TransportProtocols.Utilities
{
    internal class PeerMulticastFinder:IDisposable
    {
        /// <inheritdoc />
        public PeerMulticastFinder()
        {
            MulticastSocket = new Socket(AddressFamily.InterNetwork,
                SocketType.Dgram,
                ProtocolType.Udp);
            MulticastAddress = IPAddress.Parse("224.169.52.69");
        }

        /// <summary>
        /// 组播地址
        /// </summary>
        public IPAddress MulticastAddress { set; get; }

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
                var multicastOption = new MulticastOption(MulticastAddress, IPAddress.Any);

                MulticastSocket.SetSocketOption(SocketOptionLevel.IP,
                    SocketOptionName.AddMembership,
                    multicastOption);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void ReceiveBroadcastMessages()
        {
            // 接收需要绑定 MulticastPort 端口
            var bytes = new byte[MaxByteLength];
            var groupEndPoint = new IPEndPoint(MulticastAddress, MulticastPort);
            EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

            try
            {
                while (!disposedValue)
                {
                    var length = MulticastSocket.ReceiveFrom(bytes, ref remoteEndPoint);

                    Console.WriteLine("Received broadcast from {0} :\n {1}\n",
                        groupEndPoint,
                        Encoding.UTF8.GetString(bytes, 0, length));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void BroadcastMessage(string message)
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

        private const int MulticastPort = 56095;

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
    }
}