using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NativeWifi;

namespace LajallwachedeNojalajuhoke
{
    class Program
    {
        static void Main(string[] args)
        {
            WlanClient client = new WlanClient();
            foreach (WlanClient.WlanInterface wlanIface in client.Interfaces)
            {
                // Lists all networks with WEP security
                Wlan.WlanAvailableNetwork[] networks = wlanIface.GetAvailableNetworkList(0);

                if (networks.Length == 0)
                {
                    Console.WriteLine("没有找到热点");
                    Console.ReadLine();
                }
                else
                {
                    Console.WriteLine($"找到{networks.Length}热点");
                    foreach (Wlan.WlanAvailableNetwork network in networks)
                    {
                        Console.WriteLine($"WIFI {GetStringForSSID(network.dot11Ssid)}.");
                    }

                    Console.WriteLine();
                }
            }
        }

        /// <summary>
        /// Converts a 802.11 SSID to a string.
        /// </summary>
        private static string GetStringForSSID(Wlan.Dot11Ssid ssid)
        {
            return Encoding.UTF8.GetString(ssid.SSID, 0, (int) ssid.SSIDLength);
        }
    }
}