using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace HayberenerhihaWaceafardu
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var ipFrom = IPAddress.Parse("172.16.0.0");
            var ipTo = IPAddress.Parse("172.31.255.255");
            Console.WriteLine(GetIpList(ipFrom, ipTo).Count());

            ipFrom = IPAddress.Parse("192.168.0.0");
            ipTo = IPAddress.Parse("192.168.255.255");
            Console.WriteLine(GetIpList(ipFrom, ipTo).Count());

            ipFrom = IPAddress.Parse("10.0.0.0");
            ipTo = IPAddress.Parse("10.255.255.255");
            Console.WriteLine(GetIpList(ipFrom, ipTo).Count());
        }

        private static IEnumerable<IPAddress> GetIpList(IPAddress ipFrom, IPAddress ipTo)
        {
            var ipEnd = ipTo.GetAddressBytes();
            var ipNext = ipFrom.GetAddressBytes();

            while (CompareIPs(ipNext, ipEnd) < 1)
            {
                var ip = new IPAddress(ipNext);
                IncrementIP(ipNext);
                yield return ip;
            }
        }

        private static int CompareIPs(byte[] ip1, byte[] ip2)
        {
            if (ip1 == null || ip1.Length != 4)
            {
                return -1;
            }

            if (ip2 == null || ip2.Length != 4)
            {
                return 1;
            }

            var compare = ip1[0].CompareTo(ip2[0]);
            if (compare == 0)
            {
                compare = ip1[1].CompareTo(ip2[1]);
            }

            if (compare == 0)
            {
                compare = ip1[2].CompareTo(ip2[2]);
            }

            if (compare == 0)
            {
                compare = ip1[3].CompareTo(ip2[3]);
            }

            return compare;
        }

        private static void IncrementIP(byte[] ip, int idx = 3)
        {
            if (ip == null || ip.Length != 4 || idx < 0)
            {
                return;
            }

            if (ip[idx] == 254)
            {
                ip[idx] = 1;
                IncrementIP(ip, idx - 1);
            }
            else
            {
                ip[idx] = (byte) (ip[idx] + 1);
            }
        }
    }
}