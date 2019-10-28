using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


// This is the listener example that shows how to use the MulticastOption class. 
// In particular, it shows how to use the MulticastOption(IPAddress, IPAddress) 
// constructor, which you need to use if you have a host with more than one 
// network card.
// The first parameter specifies the multicast group address, and the second 
// specifies the local address of the network card you want to use for the data
// exchange.
// You must run this program in conjunction with the sender program as 
// follows:
// Open a console window and run the listener from the command line. 
// In another console window run the sender. In both cases you must specify 
// the local IPAddress to use. To obtain this address run the ipconfig command 
// from the command line. 
//  
namespace Mssc.TransportProtocols.Utilities
{
    class PeerMulticastFinder
    {
        private const int MulticastPort = 11002;

        /// <inheritdoc />
        public PeerMulticastFinder()
        {
            MulticastSocket = new Socket(AddressFamily.InterNetwork,
                SocketType.Dgram,
                ProtocolType.Udp);
            MulticastAddress = IPAddress.Parse("224.168.100.2");
        }

        private Socket MulticastSocket { get; }

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

        private void TryBindSocket()
        {
            for (int i = MulticastPort; i < 65530; i++)
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

        public void ReceiveBroadcastMessages()
        {
            bool done = false;
            byte[] bytes = new byte[1024];
            IPEndPoint groupEndPoint = new IPEndPoint(MulticastAddress, MulticastPort);
            EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

            try
            {
                while (!done)
                {
                    Console.WriteLine("Waiting for multicast packets.......");
                    Console.WriteLine("Enter ^C to terminate.");

                    var length = MulticastSocket.ReceiveFrom(bytes, ref remoteEndPoint);

                    Console.WriteLine("Received broadcast from {0} :\n {1}\n",
                        groupEndPoint.ToString(),
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
                MulticastSocket.SendTo(Encoding.UTF8.GetBytes(message), endPoint);
            }
            catch (Exception e)
            {
                Console.WriteLine("\n" + e.ToString());
            }
        }
    }

    class TestMulticastOption2
    {
        static IPAddress mcastAddress;
        static int mcastPort;
        static Socket mcastSocket;

        static void JoinMulticastGroup()
        {
            try
            {
                // Create a multicast socket.
                mcastSocket = new Socket(AddressFamily.InterNetwork,
                    SocketType.Dgram,
                    ProtocolType.Udp);

                // Get the local IP address used by the listener and the sender to
                // exchange multicast messages. 
                Console.Write("\nEnter local IPAddress for sending multicast packets: ");
                IPAddress localIPAddr = TestMulticastOption.LocalIpAddress;

                // Create an IPEndPoint object. 
                IPEndPoint IPlocal = new IPEndPoint(localIPAddr, 0);

                // Bind this endpoint to the multicast socket.
                mcastSocket.Bind(IPlocal);

                // Define a MulticastOption object specifying the multicast group 
                // address and the local IP address.
                // The multicast group address is the same as the address used by the listener.
                MulticastOption mcastOption;
                mcastOption = new MulticastOption(mcastAddress, localIPAddr);

                mcastSocket.SetSocketOption(SocketOptionLevel.IP,
                    SocketOptionName.AddMembership,
                    mcastOption);
            }
            catch (Exception e)
            {
                Console.WriteLine("\n" + e.ToString());
            }
        }

        public static void BroadcastMessage(string message)
        {
            IPEndPoint endPoint;

            try
            {
                //Send multicast packets to the listener.
                endPoint = new IPEndPoint(mcastAddress, mcastPort);
                mcastSocket.SendTo(ASCIIEncoding.ASCII.GetBytes(message), endPoint);
                Console.WriteLine("Multicast data sent.....");
            }
            catch (Exception e)
            {
                Console.WriteLine("\n" + e.ToString());
            }

            //mcastSocket.Close();
        }


        static TestMulticastOption2()
        {
            // Initialize the multicast address group and multicast port.
            // Both address and port are selected from the allowed sets as
            // defined in the related RFC documents. These are the same 
            // as the values used by the sender.
            mcastAddress = IPAddress.Parse("224.168.100.2");
            mcastPort = 11000;
        }

        public static void JeburdaynayuRulifalljo()
        {
            // Join the listener multicast group.
            JoinMulticastGroup();

            // Broadcast the message to the listener.
            BroadcastMessage("Hello multicast listener.");
        }
    }


    public class TestMulticastOption
    {
        private static IPAddress mcastAddress;
        private static int mcastPort;
        private static Socket mcastSocket;
        private static MulticastOption mcastOption;


        private static void MulticastOptionProperties()
        {
            Console.WriteLine("Current multicast group is: " + mcastOption.Group);
            Console.WriteLine("Current multicast local address is: " + mcastOption.LocalAddress);
        }

        public static IPAddress LocalIpAddress { set; get; }

        private static void StartMulticast()
        {
            try
            {
                mcastSocket = new Socket(AddressFamily.InterNetwork,
                    SocketType.Dgram,
                    ProtocolType.Udp);

                Console.Write("Enter the local IP address: ");

                IPAddress localIPAddr = IPAddress.Parse("0.0.0.0");
                LocalIpAddress = localIPAddr;

                //IPAddress localIP = IPAddress.Any;
                EndPoint localEP = (EndPoint) new IPEndPoint(localIPAddr, mcastPort);

                mcastSocket.Bind(localEP);


                // Define a MulticastOption object specifying the multicast group 
                // address and the local IPAddress.
                // The multicast group address is the same as the address used by the server.
                mcastOption = new MulticastOption(mcastAddress, localIPAddr);

                mcastSocket.SetSocketOption(SocketOptionLevel.IP,
                    SocketOptionName.AddMembership,
                    mcastOption);
            }

            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void ReceiveBroadcastMessages()
        {
            bool done = false;
            byte[] bytes = new Byte[100];
            IPEndPoint groupEP = new IPEndPoint(mcastAddress, mcastPort);
            EndPoint remoteEP = (EndPoint) new IPEndPoint(IPAddress.Any, 0);

            try
            {
                while (!done)
                {
                    Console.WriteLine("Waiting for multicast packets.......");
                    Console.WriteLine("Enter ^C to terminate.");

                    var length = mcastSocket.ReceiveFrom(bytes, ref remoteEP);

                    Console.WriteLine("Received broadcast from {0} :\n {1}\n",
                        groupEP.ToString(),
                        Encoding.ASCII.GetString(bytes, 0, length));
                }

                mcastSocket.Close();
            }

            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public static void Main(String[] args)
        {
            var peerMulticastFinder = new PeerMulticastFinder();

            peerMulticastFinder.StartMulticast();

            //peerMulticastFinder.JoinMulticastGroup();

            Task.Run(async () =>
            {
                var n = 0;
                while (true)
                {
                    n++;
                    peerMulticastFinder.BroadcastMessage(Environment.UserName + $" {n}");
                    await Task.Delay(1000);
                }
            });

            peerMulticastFinder.ReceiveBroadcastMessages();

            return;


            // Initialize the multicast address group and multicast port.
            // Both address and port are selected from the allowed sets as
            // defined in the related RFC documents. These are the same 
            // as the values used by the sender.
            mcastAddress = IPAddress.Parse("224.168.100.2");
            mcastPort = 11000;

            // Start a multicast group.
            StartMulticast();

            // Display MulticastOption properties.
            MulticastOptionProperties();

            Task.Run(async () =>
            {
                TestMulticastOption2.JeburdaynayuRulifalljo();
                var n = 0;
                while (true)
                {
                    n++;
                    TestMulticastOption2.BroadcastMessage(Environment.UserName + $" {n}");
                    await Task.Delay(1000);
                }
            });

            // Receive broadcast messages.
            ReceiveBroadcastMessages();
        }
    }
}