// See https://aka.ms/new-console-template for more information

using System.Net.NetworkInformation;
using System.Net.Sockets;

var ipV4List = new List<string>();
foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
{
    if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
    {
        continue;
    }

    if (networkInterface.Supports(NetworkInterfaceComponent.IPv4))
    {
        var ipInterfaceProperties = networkInterface.
            GetIPProperties();
        foreach (var unicastIpAddressInformation in ipInterfaceProperties.UnicastAddresses)
        {
            if (unicastIpAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork)
            {
                var address = unicastIpAddressInformation.Address.ToString();
                ipV4List.Add(address);

                Console.WriteLine($"{address} - {networkInterface.Name}");
            }
        }

        var statistics = networkInterface.GetIPv4Statistics();
    }
}

foreach (var address in ipV4List)
{
    //Console.WriteLine(address);
}

Console.WriteLine("Hello, World!");
