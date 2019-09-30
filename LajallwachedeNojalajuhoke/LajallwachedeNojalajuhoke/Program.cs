using System;
using System.Text;
using NativeWifi;


namespace LajallwachedeNojalajuhoke
{
    class Program
    {
        static void Main(string[] args)
        {
            var wlanClient = new WlanClient();
            foreach (var wlanClientInterface in wlanClient.Interfaces)
            {
                foreach (var wlanAvailableNetwork in wlanClientInterface.GetAvailableNetworkList(Wlan.WlanGetAvailableNetworkFlags.IncludeAllAdhocProfiles))
                {
                    Console.WriteLine($"WIFI {GetStringForSSID(wlanAvailableNetwork.dot11Ssid)}.");
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