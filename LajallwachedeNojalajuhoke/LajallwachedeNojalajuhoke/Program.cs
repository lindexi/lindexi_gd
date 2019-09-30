using System;
using System.Text;
using SimpleWifi.Win32;
using SimpleWifi.Win32.Interop;

namespace LajallwachedeNojalajuhoke
{
    class Program
    {
        static void Main(string[] args)
        {
            var wlanClient = new WlanClient();
            foreach (var wlanClientInterface in wlanClient.Interfaces)
            {
                foreach (var wlanAvailableNetwork in wlanClientInterface.GetAvailableNetworkList(WlanGetAvailableNetworkFlags.IncludeAllAdhocProfiles))
                {
                    Console.WriteLine($"WIFI {GetStringForSSID(wlanAvailableNetwork.dot11Ssid)}.");
                }
            }
        }

        /// <summary>
        /// Converts a 802.11 SSID to a string.
        /// </summary>
        private static string GetStringForSSID(Dot11Ssid ssid)
        {
            return Encoding.UTF8.GetString(ssid.SSID, 0, (int) ssid.SSIDLength);
        }
    }
}