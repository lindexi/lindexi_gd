using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace WebebecenurlalGairbuburwhalllealal
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
            {
                if
                ((
                     item.NetworkInterfaceType == NetworkInterfaceType.Ethernet // 有线网络
                     || item.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 // 无线 wifi 网络
                 )
                    && item.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            Console.WriteLine(ip.Address.ToString());
                        }
                    }
                }
            }

            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    Console.WriteLine(ip.ToString());
                }
            }
        }
    }
}
